using System.Security.Claims;
using AiCompetency.Api.Data;
using AiCompetency.Api.Models;
using AiCompetency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace AiCompetency.Api.Controllers;

[ApiController]
[Route("api/adaptivetests")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class AdaptiveTestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly MlService _ml;
    private readonly CompetencyProfileService _profileService;

    // CAT Configuration
    private const int MaxQuestions = 10;
    private const decimal InitialAbility = 0.0m;   // IRT logit scale, N(0,1)
    private const decimal ThetaMin = -4.0m;
    private const decimal ThetaMax = 4.0m;

    public AdaptiveTestsController(
        ApplicationDbContext context,
        MlService ml,
        CompetencyProfileService profileService)
    {
        _context = context;
        _ml = ml;
        _profileService = profileService;
    }

    private int GetCurrentUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        return 1; // Default fallback
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartAdaptiveTest()
    {
        var session = new AdaptiveTestSession
        {
            Id = Guid.NewGuid(),
            UserId = GetCurrentUserId(),
            StartTime = DateTime.UtcNow,
            Status = "InProgress",
            CurrentAbilityEstimate = InitialAbility,
            QuestionsAskedCount = 0
        };

        _context.AdaptiveTestSessions.Add(session);
        await _context.SaveChangesAsync();

        return Ok(new { SessionId = session.Id });
    }

    [HttpGet("{sessionId}/next")]
    public async Task<IActionResult> GetNextQuestion(Guid sessionId)
    {
        var session = await _context.AdaptiveTestSessions
            .Include(s => s.Responses)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null || session.UserId != GetCurrentUserId())
        {
            return NotFound("Session not found.");
        }

        if (session.Status == "Completed" || session.QuestionsAskedCount >= MaxQuestions)
        {
            return BadRequest(new { Message = "Test is already completed", Status = "Completed" });
        }

        var askedQuestionIds = session.Responses.Select(r => r.QuestionId).ToList();
        var currentTheta = (double)session.CurrentAbilityEstimate;

        // Fetch candidate questions (published, not yet asked, have difficulty)
        var candidateQuestions = await _context.Questions
            .Where(q => q.Status == QuestionStatus.Published
                     && q.DifficultyLevel.HasValue
                     && !askedQuestionIds.Contains(q.Id))
            .ToListAsync();

        if (candidateQuestions.Count == 0)
        {
            session.Status = "Completed";
            session.EndTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return BadRequest(new { Message = "No more questions available. Test completed.", Status = "Completed" });
        }

        // Load IRT params for each candidate (or use defaults)
        var candidateIds = candidateQuestions.Select(q => q.Id).ToList();
        var irtParamsMap = await _context.QuestionIrtParams
            .Where(p => candidateIds.Contains(p.QuestionId))
            .ToDictionaryAsync(p => p.QuestionId);

        var candidatesWithParams = candidateQuestions.Select(q =>
        {
            if (!irtParamsMap.TryGetValue(q.Id, out var p))
                p = new QuestionIrtParams
                {
                    QuestionId = q.Id,
                    AParam = 1.0m,
                    BParam = q.DifficultyLevel switch { 1 => -2m, 2 => -1m, 4 => 1m, 5 => 2m, _ => 0m },
                    CParam = 0.25m
                };
            return (q, p);
        }).ToList();

        // Select next item: try ML service (Fisher Information) then fall back to closest-b
        Question nextQuestion;
        var mlResult = await _ml.SelectNextItemAsync(
            currentTheta,
            candidatesWithParams.Select(c => (c.q.Id, c.p)).ToList());

        if (mlResult != null)
        {
            nextQuestion = candidatesWithParams
                .First(c => c.q.Id == mlResult.SelectedItemId).q;
        }
        else
        {
            // Fallback: item with b closest to current theta
            nextQuestion = candidatesWithParams
                .OrderBy(c => Math.Abs((double)c.p.BParam - currentTheta))
                .First().q;
        }

        return Ok(new
        {
            SessionId = session.Id,
            QuestionIndex = session.QuestionsAskedCount + 1,
            TotalQuestions = MaxQuestions,
            Question = new
            {
                nextQuestion.Id,
                nextQuestion.Type,
                nextQuestion.Content,
                nextQuestion.Metadata,
                Difficulty = nextQuestion.DifficultyLevel
            }
        });
    }

    public class SubmitAdaptiveAnswerRequest
    {
        public int QuestionId { get; set; }
        public string Answer { get; set; } = string.Empty;
    }

    [HttpPost("{sessionId}/submit")]
    public async Task<IActionResult> SubmitAnswer(Guid sessionId, [FromBody] SubmitAdaptiveAnswerRequest request)
    {
        var session = await _context.AdaptiveTestSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null || session.UserId != GetCurrentUserId())
            return NotFound("Session not found.");

        if (session.Status == "Completed")
            return BadRequest("Session already completed.");

        var question = await _context.Questions.FindAsync(request.QuestionId);
        if (question == null) return NotFound("Question not found.");

        bool isCorrect = !string.IsNullOrWhiteSpace(request.Answer);

        // ── IRT-based theta update (EAP) ────────────────────────────────────
        // Build all responses so far (including current) for EAP
        var allResponsesSoFar = session.Responses
            .Select(r => new IrtItemResponse(
                r.QuestionId, r.IsCorrect,
                new IrtItemParams(1.0, MapDifficultyToB(r.QuestionDifficultyLevel), 0.25)))
            .Append(new IrtItemResponse(
                request.QuestionId, isCorrect,
                new IrtItemParams(1.0, MapDifficultyToB((decimal)(question.DifficultyLevel ?? 3)), 0.25)))
            .ToList();

        // Try ML EAP; fall back to clamped linear adjustment
        decimal newAbility;
        var thetaEst = await _ml.EstimateThetaAsync(allResponsesSoFar);
        if (thetaEst != null)
        {
            newAbility = Math.Clamp((decimal)thetaEst.Theta, ThetaMin, ThetaMax);
        }
        else
        {
            // Fallback: simple ±0.4 step in logit scale
            decimal step = 0.4m;
            newAbility = Math.Clamp(
                session.CurrentAbilityEstimate + (isCorrect ? step : -step),
                ThetaMin, ThetaMax);
        }

        var response = new AdaptiveResponse
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            QuestionId = request.QuestionId,
            QuestionDifficultyLevel = question.DifficultyLevel ?? 0,
            UserAnswer = request.Answer,
            IsCorrect = isCorrect,
            AbilityAfterResponse = newAbility,
            CreatedAt = DateTime.UtcNow
        };

        _context.AdaptiveResponses.Add(response);

        session.CurrentAbilityEstimate = newAbility;
        session.QuestionsAskedCount++;

        bool isTestComplete = session.QuestionsAskedCount >= MaxQuestions;
        if (isTestComplete)
        {
            session.Status = "Completed";
            session.EndTime = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            IsCorrect = isCorrect,
            NewAbilityEstimate = newAbility,
            IsTestComplete = isTestComplete
        });
    }

    [HttpGet("{sessionId}/result")]
    public async Task<IActionResult> GetAdaptiveTestResult(Guid sessionId)
    {
        var session = await _context.AdaptiveTestSessions
            .Include(s => s.Responses)
                .ThenInclude(r => r.Question)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null || session.UserId != GetCurrentUserId())
            return NotFound("Session not found.");

        // Mimic standard test results structure so we can reuse the TestResultsComponent
        var result = new
        {
            SessionId = session.Id,
            ExamTitle = "Adaptive Competency Assessment",
            TotalScore = session.CurrentAbilityEstimate, // Use ability as score
            PassScore = 6, // E.g., ability > 6 is pass
            Passed = session.CurrentAbilityEstimate >= 6,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            Responses = session.Responses.OrderBy(r => r.CreatedAt).Select(r => new
            {
                r.QuestionId,
                r.Question.Content,
                r.UserAnswer,
                finalAnswer = r.UserAnswer,
                ScoreEarned = r.IsCorrect ? 1 : 0,
                AiFeedback = $"Difficulty b≈{r.QuestionDifficultyLevel}. θ={r.AbilityAfterResponse:F2}. {(r.IsCorrect ? "Correct" : "Incorrect")}.",
                r.Question.Metadata
            })
        };

        return Ok(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Map integer difficulty level to IRT b parameter (logit scale).</summary>
    private static double MapDifficultyToB(decimal difficultyLevel) => (int)difficultyLevel switch
    {
        1 => -2.0,
        2 => -1.0,
        4 => 1.0,
        5 => 2.0,
        _ => 0.0
    };

    private static string ClassifyLevel(decimal theta) => theta switch
    {
        < -1.0m => "Foundation",
        < 1.0m  => "Apply",
        _       => "Create"
    };
}
