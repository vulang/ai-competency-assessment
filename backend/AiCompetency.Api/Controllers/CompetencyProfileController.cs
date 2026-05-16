using System.Security.Claims;
using System.Text.Json;
using AiCompetency.Api.Data;
using AiCompetency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace AiCompetency.Api.Controllers;

[ApiController]
[Route("api/competency")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class CompetencyProfileController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly CompetencyProfileService _profileService;

    public CompetencyProfileController(
        ApplicationDbContext context,
        CompetencyProfileService profileService)
    {
        _context = context;
        _profileService = profileService;
    }

    private int GetCurrentUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdString, out var id) ? id : 1;
    }

    // ── Candidate endpoints ───────────────────────────────────────────────────

    /// <summary>
    /// GET /api/competency/profile
    /// Returns the latest competency profile for the authenticated candidate.
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        var profile = await _profileService.GetLatestProfileAsync(userId);

        if (profile == null)
            return NotFound(new { Message = "No competency profile found. Complete a test first." });

        return Ok(MapProfile(profile));
    }

    /// <summary>
    /// GET /api/competency/profile/history
    /// Returns all competency profiles for the authenticated candidate.
    /// </summary>
    [HttpGet("profile/history")]
    public async Task<IActionResult> GetMyProfileHistory()
    {
        var userId = GetCurrentUserId();
        var profiles = await _context.CompetencyProfiles
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.GeneratedAt)
            .ToListAsync();

        return Ok(profiles.Select(MapProfile));
    }

    /// <summary>
    /// GET /api/competency/sessions/{sessionId}/irt
    /// Returns IRT theta estimate for a specific session.
    /// </summary>
    [HttpGet("sessions/{sessionId}/irt")]
    public async Task<IActionResult> GetSessionIrt(Guid sessionId)
    {
        var estimate = await _context.SessionIrtEstimates.FindAsync(sessionId);
        if (estimate == null)
            return NotFound(new { Message = "No IRT estimate for this session." });

        return Ok(new
        {
            SessionId = estimate.SessionId,
            Theta = estimate.Theta,
            SeTheta = estimate.SeTheta,
            Method = estimate.Method,
            ComputedAt = estimate.ComputedAt,
            OverallLevel = ClassifyLevel((double)estimate.Theta)
        });
    }

    /// <summary>
    /// GET /api/competency/skills
    /// Returns BKT mastery per skill for the authenticated candidate.
    /// </summary>
    [HttpGet("skills")]
    public async Task<IActionResult> GetMySkillMastery()
    {
        var userId = GetCurrentUserId();
        var masteries = await _context.UserSkillMasteries
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.SkillId)
            .ToListAsync();

        return Ok(masteries.Select(m => new
        {
            m.SkillId,
            m.MasteryProb,
            Mastered = m.MasteryProb >= 0.95m,
            m.ResponseCount,
            m.LastUpdated
        }));
    }

    // ── Admin endpoints ───────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/competency/admin/candidates/{userId}/profile
    /// Admin: view any candidate's latest competency profile.
    /// </summary>
    [HttpGet("admin/candidates/{userId}/profile")]
    public async Task<IActionResult> GetCandidateProfile(int userId)
    {
        var profile = await _profileService.GetLatestProfileAsync(userId);
        if (profile == null)
            return NotFound(new { Message = $"No competency profile found for user {userId}." });

        return Ok(MapProfile(profile));
    }

    /// <summary>
    /// GET /api/competency/admin/irt-stats
    /// Admin: IRT parameter statistics across all questions.
    /// </summary>
    [HttpGet("admin/irt-stats")]
    public async Task<IActionResult> GetIrtStats()
    {
        var stats = await _context.QuestionIrtParams
            .Select(p => new
            {
                p.QuestionId,
                p.AParam,
                p.BParam,
                p.CParam,
                p.SeB,
                p.IsCalibrated,
                p.ResponseCount,
                p.CalibratedAt
            })
            .ToListAsync();

        var summary = new
        {
            TotalItems = stats.Count,
            CalibratedItems = stats.Count(s => s.IsCalibrated),
            AvgDifficulty = stats.Count > 0 ? stats.Average(s => (double)s.BParam) : 0,
            AvgDiscrimination = stats.Count > 0 ? stats.Average(s => (double)s.AParam) : 0,
            Items = stats
        };

        return Ok(summary);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static object MapProfile(Models.CompetencyProfile p)
    {
        var scores = p.Scores.RootElement.EnumerateObject()
            .ToDictionary(e => e.Name, e => e.Value.GetDouble());

        return new
        {
            ProfileId = p.ProfileId,
            UserId = p.UserId,
            SessionId = p.SessionId,
            Theta = p.Theta,
            OverallLevel = p.OverallLevel,
            OverallScore = p.OverallScore,
            DomainScores = scores,
            GeneratedAt = p.GeneratedAt
        };
    }

    private static string ClassifyLevel(double theta) => theta switch
    {
        < -1.0 => "Foundation",
        < 1.0 => "Apply",
        _ => "Create"
    };
}
