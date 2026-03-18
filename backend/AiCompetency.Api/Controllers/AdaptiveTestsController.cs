using System.Security.Claims;
using AiCompetency.Api.Data;
using AiCompetency.Api.Models;
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

    // CAT Configuration
    private const int MaxQuestions = 10;
    private const decimal InitialAbility = 5.0m;
    private const decimal AbilityAdjustment = 1.0m;

    public AdaptiveTestsController(ApplicationDbContext context)
    {
        _context = context;
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

        // Get questions that haven't been asked, are published, and have a difficulty level.
        // Orders by the absolute difference between the question's difficulty and the candidate's estimated ability.
        var nextQuestion = await _context.Questions
            .Where(q => q.Status == QuestionStatus.Published && q.DifficultyLevel.HasValue && !askedQuestionIds.Contains(q.Id))
            .OrderBy(q => Math.Abs((decimal)q.DifficultyLevel.Value - session.CurrentAbilityEstimate))
            .FirstOrDefaultAsync();

        if (nextQuestion == null)
        {
            // If no more questions available, complete test
            session.Status = "Completed";
            session.EndTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return BadRequest(new { Message = "No more questions available. Test completed.", Status = "Completed" });
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

        // Simple mock scoring based on whether they typed anything
        // In a real CAT, "isCorrect" relies heavily on precise answer matching or AI grading
        bool isCorrect = !string.IsNullOrWhiteSpace(request.Answer);

        // Adjust ability estimate
        decimal oldAbility = session.CurrentAbilityEstimate;
        decimal newAbility = oldAbility;

        if (isCorrect)
        {
            newAbility += AbilityAdjustment;
        }
        else
        {
            newAbility -= AbilityAdjustment;
        }

        // Clamp ability between bounds if needed (e.g. 1 to 10)
        newAbility = Math.Clamp(newAbility, 1.0m, 10.0m);

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
                finalAnswer = r.UserAnswer, // Aliased for GUI component
                ScoreEarned = r.IsCorrect ? 1 : 0,
                AiFeedback = $"Difficulty: {r.QuestionDifficultyLevel}. Ability changed to {r.AbilityAfterResponse}. Answer evaluated as {(r.IsCorrect ? "Correct" : "Incorrect")}."
            })
        };

        return Ok(result);
    }
}
