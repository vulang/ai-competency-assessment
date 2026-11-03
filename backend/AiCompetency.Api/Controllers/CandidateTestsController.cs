using System.Security.Claims;
using System.Text.Json;
using AiCompetency.Api.Data;
using AiCompetency.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

using AiCompetency.Api.Services;

namespace AiCompetency.Api.Controllers;

[ApiController]
[Route("api/candidate/tests")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class CandidateTestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ProfileJobQueue _profileJobs;
    private readonly IScoringService _scoring;

    public CandidateTestsController(
        ApplicationDbContext context,
        ProfileJobQueue profileJobs,
        IScoringService scoring)
    {
        _context = context;
        _profileJobs = profileJobs;
        _scoring = scoring;
    }

    private string GetCurrentUserId() => CurrentUser.GetRequiredId(User);


    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableTests()
    {
        var exams = await _context.Exams
            .Where(e => e.Status == "active")
            .Where(e => e.ExamQuestions.Any())
            .OrderByDescending(e => e.IsStandard).ThenBy(e => e.Title)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Description,
                e.DurationMinutes,
                e.PassScore,
                e.IsStandard,
                QuestionCount = e.ExamQuestions.Count
            })
            .ToListAsync();

        return Ok(exams);
    }

    [HttpPost("{examId}/start")]
    public async Task<IActionResult> StartTest(int examId)
    {
        var exam = await _context.Exams.Include(e => e.ExamQuestions).SingleOrDefaultAsync(e => e.Id == examId);
        if (exam == null || exam.Status != "active" || exam.ExamQuestions.Count == 0)
        {
            return NotFound("Exam not found or not published.");
        }

        return Ok(new { SessionId = await CreateSession(exam.Id) });
    }

    private async Task<Guid> CreateSession(int examId)
    {
        var session = new ExamResult
        {
            Id = Guid.NewGuid(),
            UserId = GetCurrentUserId(),
            ExamId = examId,
            StartTime = DateTime.UtcNow,
            Status = "InProgress",
            TotalScore = 0
        };

        _context.ExamResults.Add(session);
        await _context.SaveChangesAsync();
        return session.Id;
    }

    [HttpGet("sessions/{sessionId}")]
    public async Task<IActionResult> GetTestSession(Guid sessionId)
    {
        var session = await _context.ExamResults
            .Include(er => er.Exam)
                .ThenInclude(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
            .FirstOrDefaultAsync(er => er.Id == sessionId);

        if (session == null || session.UserId != GetCurrentUserId())
        {
            return NotFound("Session not found.");
        }

        var response = new
        {
            SessionId = session.Id,
            Exam = new
            {
                session.Exam.Id,
                session.Exam.Title,
                session.Exam.DurationMinutes
            },
            StartTime = session.StartTime,
            Questions = session.Exam.ExamQuestions
                .OrderBy(eq => eq.OrderIndex)
                .Select(eq => new
                {
                    eq.Question.Id,
                    eq.Question.Type,
                    eq.Question.Content,
                    Metadata = QuestionPresentation.CandidateMetadata(eq.Question.Metadata)
                })
        };

        return Ok(response);
    }

    /// <summary>Starts the published standard form defined by an experimental blueprint (thesis §2.12).</summary>
    [HttpPost("start-standard")]
    public async Task<IActionResult> StartStandardTest()
    {
        var exam = await _context.Exams
            .Where(e => e.IsStandard && e.Status == "active" && e.ExamQuestions.Any())
            .SingleOrDefaultAsync();
        if (exam == null)
            return NotFound("No standard test has been published yet.");

        return Ok(new { SessionId = await CreateSession(exam.Id) });
    }

    public class SubmitTestRequest
    {
        public List<QuestionResponse> Responses { get; set; } = new();
    }

    public class QuestionResponse
    {
        public int QuestionId { get; set; }
        public string Answer { get; set; } = string.Empty;
    }

    [HttpPost("sessions/{sessionId}/submit")]
    public async Task<IActionResult> SubmitTest(Guid sessionId, [FromBody] SubmitTestRequest request)
    {
        var session = await _context.ExamResults
            .Include(er => er.Exam)
                .ThenInclude(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
            .FirstOrDefaultAsync(er => er.Id == sessionId);

        if (session == null || session.UserId != GetCurrentUserId())
        {
            return NotFound("Session not found.");
        }

        if (session.Status == "Completed")
        {
            return BadRequest("Session already completed.");
        }

        if (request.Responses.Select(r => r.QuestionId).Distinct().Count() != request.Responses.Count)
            return BadRequest("Each question may only be answered once.");
        if (request.Responses.Any(r => !session.Exam.ExamQuestions.Any(q => q.QuestionId == r.QuestionId)))
            return BadRequest("A response refers to a question outside this exam.");

        decimal totalScore = 0;
        var responsesToSave = new List<Response>();

        foreach (var answer in request.Responses)
        {
            var examQuestion = session.Exam.ExamQuestions.First(eq => eq.QuestionId == answer.QuestionId);
            var scored = _scoring.Score(examQuestion.Question!, answer.Answer, examQuestion.PointValue);
            totalScore += scored.Score;

            responsesToSave.Add(new Response
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                QuestionId = answer.QuestionId,
                FinalAnswer = answer.Answer,
                ScoreEarned = scored.Score,
                SubmittedAt = DateTime.UtcNow,
                AiFeedback = scored.Feedback
            });
        }

        // Every objective item on the form counts toward its group; unanswered items score zero.
        var mapping = await _context.TestLevelMappings.SingleOrDefaultAsync(m => m.IsActive);
        var earned = responsesToSave.ToDictionary(r => r.QuestionId, r => r.ScoreEarned);
        _context.TestGroupScores.AddRange(GroupScoreCalculator.Compute(sessionId,
            session.Exam.ExamQuestions.Select(eq => new ScoredItem(eq.Question!, eq.PointValue, earned.GetValueOrDefault(eq.QuestionId))),
            mapping));

        _context.Responses.AddRange(responsesToSave);

        session.Status = "Completed";
        session.EndTime = DateTime.UtcNow;
        session.TotalScore = totalScore;

        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateException error) when (error is DbUpdateConcurrencyException ||
            error.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        { return Conflict("This session was submitted by another request."); }

        // Queue only the persisted session ID; the worker loads it in its own DI scope.
        _profileJobs.Enqueue(session.Id);

        return Ok(new { SessionId = session.Id, TotalScore = totalScore });
    }

    [HttpGet("sessions/{sessionId}/result")]
    public async Task<IActionResult> GetTestResult(Guid sessionId)
    {
        var session = await _context.ExamResults
            .Include(er => er.Exam)
            .Include(er => er.GroupScores)
            .Include(er => er.Responses)
                .ThenInclude(r => r.Question)
            .FirstOrDefaultAsync(er => er.Id == sessionId);

        if (session == null || session.UserId != GetCurrentUserId())
        {
            return NotFound("Session not found.");
        }

        if (session.Status != "Completed") return Conflict("Complete the test before viewing results.");

        var result = new
        {
            SessionId = session.Id,
            ExamTitle = session.Exam.Title,
            TotalScore = session.TotalScore,
            PassScore = session.Exam.PassScore,
            Passed = session.TotalScore >= session.Exam.PassScore,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            GroupScores = TestProfileView.SevenGroups(session.GroupScores),
            Responses = session.Responses.Select(r => new
            {
                r.QuestionId,
                r.Question.Content,
                r.FinalAnswer,
                r.ScoreEarned,
                r.AiFeedback,
                r.Question.Metadata
            })
        };

        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetTestHistory()
    {
        var sessions = await _context.ExamResults
            .Include(er => er.Exam)
            .Where(er => er.UserId == GetCurrentUserId() && er.Status == "Completed")
            .OrderByDescending(er => er.EndTime)
            .Select(er => new
            {
                SessionId = er.Id,
                ExamId = er.ExamId,
                ExamTitle = er.Exam.Title,
                TotalScore = er.TotalScore,
                PassScore = er.Exam.PassScore,
                StartTime = er.StartTime,
                EndTime = er.EndTime
            })
            .ToListAsync();

        return Ok(sessions);
    }

}
