using AiCompetency.Api.Data;
using AiCompetency.Api.Models;
using AiCompetency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace AiCompetency.Api.Controllers;

/// <summary>Admin dashboard and attempt reports for standard (non-adaptive) tests.</summary>
[ApiController]
[Route("api/reports")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Admin")]
public sealed class ReportsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] int? examId)
    {
        var sessions = await CompletedSessions(examId);
        var scored = sessions.Select(s => (Session: s, Percent: Percent(s))).Where(s => s.Percent != null).ToList();
        var exams = await context.ExamResults.Where(s => s.Status == "Completed")
            .GroupBy(s => new { s.ExamId, s.Exam!.Title, s.Exam.IsStandard })
            .Select(g => new { g.Key.ExamId, g.Key.Title, g.Key.IsStandard, Attempts = g.Count() })
            .OrderByDescending(e => e.Attempts).ToListAsync();
        return Ok(new
        {
            ExamId = examId,
            Exams = exams,
            Attempts = sessions.Count,
            Candidates = sessions.Select(s => s.UserId).Distinct().Count(),
            PassRate = sessions.Count == 0 ? (decimal?)null : Math.Round(100m * sessions.Count(Passed) / sessions.Count, 1),
            AveragePercent = scored.Count == 0 ? (decimal?)null : Math.Round(scored.Average(s => s.Percent!.Value), 1),
            ScoreDistribution = Enumerable.Range(0, 10).Select(bin => new
            {
                From = bin * 10,
                To = bin == 9 ? 100 : bin * 10 + 9,
                Count = scored.Count(s => Math.Min(9, (int)(s.Percent!.Value / 10)) == bin)
            }),
            Groups = CompetencyCatalog.Groups.Select(g =>
            {
                var scores = sessions.SelectMany(s => s.GroupScores).Where(gs => gs.GroupCode == g.Code).ToList();
                return new
                {
                    GroupCode = g.Code,
                    GroupName = g.Name,
                    Sessions = scores.Count,
                    AveragePercent = scores.Count == 0 ? (decimal?)null : Math.Round(scores.Average(s => s.Percent), 1),
                    Basic = scores.Count(s => s.Level == 1),
                    Intermediate = scores.Count(s => s.Level == 2),
                    Advanced = scores.Count(s => s.Level == 3),
                    Unmapped = scores.Count(s => s.Level == null)
                };
            })
        });
    }

    [HttpGet("attempts")]
    public async Task<IActionResult> Attempts([FromQuery] int? examId)
    {
        var sessions = await context.ExamResults.Include(s => s.Exam).ThenInclude(e => e!.ExamQuestions)
            .Where(s => examId == null || s.ExamId == examId)
            .OrderByDescending(s => s.EndTime ?? s.StartTime).ToListAsync();
        var emails = await Emails(sessions.Select(s => s.UserId));
        return Ok(sessions.Select(s => new
        {
            SessionId = s.Id,
            Candidate = emails.GetValueOrDefault(s.UserId, s.UserId),
            ExamId = s.ExamId,
            ExamTitle = s.Exam!.Title,
            s.StartTime,
            s.EndTime,
            s.Status,
            Score = s.TotalScore,
            MaxScore = s.Exam.ExamQuestions.Sum(eq => eq.PointValue),
            Percent = Percent(s),
            PassScore = s.Exam.PassScore,
            Passed = s.Status == "Completed" ? Passed(s) : (bool?)null
        }));
    }

    [HttpGet("attempts/{sessionId:guid}")]
    public async Task<IActionResult> Attempt(Guid sessionId)
    {
        var session = await context.ExamResults
            .Include(s => s.Exam).ThenInclude(e => e!.ExamQuestions).ThenInclude(eq => eq.Question)
            .Include(s => s.Responses)
            .Include(s => s.GroupScores)
            .SingleOrDefaultAsync(s => s.Id == sessionId);
        if (session == null) return NotFound();
        var emails = await Emails([session.UserId]);
        var responses = session.Responses.ToDictionary(r => r.QuestionId);
        return Ok(new
        {
            SessionId = session.Id,
            Candidate = emails.GetValueOrDefault(session.UserId, session.UserId),
            ExamTitle = session.Exam!.Title,
            session.StartTime,
            session.EndTime,
            session.Status,
            Score = session.TotalScore,
            MaxScore = session.Exam.ExamQuestions.Sum(eq => eq.PointValue),
            Percent = Percent(session),
            Passed = session.Status == "Completed" ? Passed(session) : (bool?)null,
            GroupScores = TestProfileView.SevenGroups(session.GroupScores),
            Responses = session.Exam.ExamQuestions.OrderBy(eq => eq.OrderIndex).Select(eq => new
            {
                eq.QuestionId,
                eq.OrderIndex,
                Title = QuestionPresentation.Title(eq.Question!),
                eq.Question!.Type,
                eq.Question.GroupCode,
                eq.Question.TargetLevel,
                Answer = responses.GetValueOrDefault(eq.QuestionId)?.FinalAnswer,
                ScoreEarned = responses.GetValueOrDefault(eq.QuestionId)?.ScoreEarned ?? 0,
                eq.PointValue,
                Feedback = responses.GetValueOrDefault(eq.QuestionId)?.AiFeedback
            })
        });
    }

    private Task<List<ExamResult>> CompletedSessions(int? examId) => context.ExamResults
        .Include(s => s.Exam).ThenInclude(e => e!.ExamQuestions)
        .Include(s => s.GroupScores)
        .Where(s => s.Status == "Completed" && (examId == null || s.ExamId == examId))
        .ToListAsync();

    private async Task<Dictionary<string, string>> Emails(IEnumerable<string> userIds)
    {
        var ids = userIds.Distinct().ToList();
        return await context.Users.Where(u => ids.Contains(u.Id) && u.Email != null)
            .ToDictionaryAsync(u => u.Id, u => u.Email!);
    }

    private static decimal? Percent(ExamResult session)
    {
        var max = session.Exam!.ExamQuestions.Sum(eq => eq.PointValue);
        return max > 0 ? Math.Round(session.TotalScore / max * 100m, 1) : null;
    }

    private static bool Passed(ExamResult session) => session.TotalScore >= session.Exam!.PassScore;
}
