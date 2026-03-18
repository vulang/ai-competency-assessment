using System.Security.Claims;
using AiCompetency.Api.Data;
using AiCompetency.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace AiCompetency.Api.Controllers;

[ApiController]
[Route("api/candidate/tests")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class CandidateTestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CandidateTestsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        // For simplicity, returning a mock ID if we can't parse it.
        // In a real scenario, the sub claim should be the integer ID.
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        return 1; // Default fallback for dev
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableTests()
    {
        var exams = await _context.Exams
            .Where(e => e.Status == "active")
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.DurationMinutes,
                e.PassScore,
                QuestionCount = e.ExamQuestions.Count
            })
            .ToListAsync();

        return Ok(exams);
    }

    [HttpPost("{examId}/start")]
    public async Task<IActionResult> StartTest(int examId)
    {
        var exam = await _context.Exams.FindAsync(examId);
        if (exam == null || exam.Status != "active")
        {
            return NotFound("Exam not found or not published.");
        }

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

        return Ok(new { SessionId = session.Id });
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
                    eq.Question.Metadata
                })
        };

        return Ok(response);
    }

    [HttpPost("start-standard")]
    public async Task<IActionResult> StartStandardTest()
    {
        // 1. Get up to 50 active questions ordered randomly
        var randomQuestions = await _context.Questions
            .Where(q => q.Status == QuestionStatus.Published)
            .OrderBy(r => Guid.NewGuid())
            .Take(50)
            .ToListAsync();

        if (randomQuestions.Count == 0)
        {
            return BadRequest("No published questions available to create a test.");
        }

        // 2. Create a dynamic Exam entity
        var newExam = new Exam
        {
            Title = "Standard Test",
            Description = "Dynamically generated standard assessment.",
            DurationMinutes = 60, // Fixed default for now
            PassScore = randomQuestions.Count * 70 / 100, // 70% passing
            QuestionCount = randomQuestions.Count,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Exams.Add(newExam);
        await _context.SaveChangesAsync(); // get ID

        // 3. Link Questions
        var examQuestions = randomQuestions.Select((q, index) => new ExamQuestion
        {
            ExamId = newExam.Id,
            QuestionId = q.Id,
            OrderIndex = index + 1,
            PointValue = 1 // 1 point per question
        }).ToList();

        _context.ExamQuestions.AddRange(examQuestions);
        
        // 4. Create the session
        var session = new ExamResult
        {
            Id = Guid.NewGuid(),
            UserId = GetCurrentUserId(),
            ExamId = newExam.Id,
            StartTime = DateTime.UtcNow,
            Status = "InProgress",
            TotalScore = 0
        };

        _context.ExamResults.Add(session);
        await _context.SaveChangesAsync();

        return Ok(new { SessionId = session.Id });
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
            .FirstOrDefaultAsync(er => er.Id == sessionId);

        if (session == null || session.UserId != GetCurrentUserId())
        {
            return NotFound("Session not found.");
        }

        if (session.Status == "Completed")
        {
            return BadRequest("Session already completed.");
        }

        decimal totalScore = 0;
        var responsesToSave = new List<Response>();

        foreach (var answer in request.Responses)
        {
            var examQuestion = session.Exam.ExamQuestions.FirstOrDefault(eq => eq.QuestionId == answer.QuestionId);
            if (examQuestion == null) continue;

            // Simple auto-scoring for prototype. In reality, we'd check against correct answer in JSON metadata.
            // For now, give full points if they answered anything, just to mock the process.
            decimal scoreEarned = string.IsNullOrWhiteSpace(answer.Answer) ? 0 : examQuestion.PointValue;
            totalScore += scoreEarned;

            responsesToSave.Add(new Response
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                QuestionId = answer.QuestionId,
                FinalAnswer = answer.Answer,
                ScoreEarned = scoreEarned,
                SubmittedAt = DateTime.UtcNow,
                AiFeedback = scoreEarned > 0 ? "Good answer!" : "Please provide an answer next time."
            });
        }

        _context.Responses.AddRange(responsesToSave);

        session.Status = "Completed";
        session.EndTime = DateTime.UtcNow;
        session.TotalScore = totalScore;

        await _context.SaveChangesAsync();

        return Ok(new { SessionId = session.Id, TotalScore = totalScore });
    }

    [HttpGet("sessions/{sessionId}/result")]
    public async Task<IActionResult> GetTestResult(Guid sessionId)
    {
        var session = await _context.ExamResults
            .Include(er => er.Exam)
            .Include(er => er.Responses)
                .ThenInclude(r => r.Question)
            .FirstOrDefaultAsync(er => er.Id == sessionId);

        if (session == null || session.UserId != GetCurrentUserId())
        {
            return NotFound("Session not found.");
        }

        var result = new
        {
            SessionId = session.Id,
            ExamTitle = session.Exam.Title,
            TotalScore = session.TotalScore,
            PassScore = session.Exam.PassScore,
            Passed = session.TotalScore >= session.Exam.PassScore,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            Responses = session.Responses.Select(r => new
            {
                r.QuestionId,
                r.Question.Content,
                r.FinalAnswer,
                r.ScoreEarned,
                r.AiFeedback
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
