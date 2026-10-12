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

    public CandidateTestsController(
        ApplicationDbContext context,
        ProfileJobQueue profileJobs)
    {
        _context = context;
        _profileJobs = profileJobs;
    }

    private string GetCurrentUserId() => CurrentUser.GetRequiredId(User);


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
                    Metadata = QuestionPresentation.CandidateMetadata(eq.Question.Metadata)
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
            PassScore = (int)Math.Ceiling(randomQuestions.Count * 0.70m), // 70% passing
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
            var examQuestion = session.Exam.ExamQuestions.FirstOrDefault(eq => eq.QuestionId == answer.QuestionId);
            if (examQuestion == null) continue;

            decimal scoreEarned = 0;
            string feedback = "Incorrect.";

            if (!string.IsNullOrWhiteSpace(answer.Answer) && examQuestion.Question != null)
            {
                if (examQuestion.Question.Type == "scenario")
                {
                    scoreEarned = 0;
                    feedback = "Answer submitted for manual review.";
                }
                else
                {
                    bool isCorrect = AnswerEvaluator.IsCorrect(examQuestion.Question?.Type ?? "", examQuestion.Question?.Metadata, answer.Answer);
                    if (isCorrect) 
                    {
                        scoreEarned = examQuestion.PointValue;
                        feedback = "Correct!";
                    }
                }
            }

            totalScore += scoreEarned;

            responsesToSave.Add(new Response
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                QuestionId = answer.QuestionId,
                FinalAnswer = answer.Answer,
                ScoreEarned = scoreEarned,
                SubmittedAt = DateTime.UtcNow,
                AiFeedback = string.IsNullOrWhiteSpace(answer.Answer) ? "Please provide an answer next time." : feedback
            });
        }

        _context.Responses.AddRange(responsesToSave);

        session.Status = "Completed";
        session.EndTime = DateTime.UtcNow;
        session.TotalScore = totalScore;

        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("This session was submitted by another request."); }

        // Queue only the persisted session ID; the worker loads it in its own DI scope.
        _profileJobs.Enqueue(session.Id);

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
