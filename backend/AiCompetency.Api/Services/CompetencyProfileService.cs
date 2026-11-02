using System.Text.Json;
using AiCompetency.Api.Data;
using AiCompetency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AiCompetency.Api.Services;

/// <summary>
/// Orchestrates IRT theta estimation, BKT mastery updates, and competency profile
/// generation after a test session is submitted.
/// </summary>
public class CompetencyProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly MlService _ml;
    private readonly ILogger<CompetencyProfileService> _logger;

    // Default IRT parameters when a question has no calibrated data yet.
    // b is mapped from difficulty_level: 1→-2, 2→-1, 3→0, 4→1, 5→2
    private static readonly double[] DifficultyToBParam = [0.0, -2.0, -1.0, 0.0, 1.0, 2.0];

    public CompetencyProfileService(
        ApplicationDbContext context,
        MlService ml,
        ILogger<CompetencyProfileService> logger)
    {
        _context = context;
        _ml = ml;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Run the full ML pipeline after a standard test session is submitted:
    ///   1. Resolve IRT params for each answered question
    ///   2. Estimate theta via ML service (EAP)
    ///   3. Persist theta to session_irt_estimates
    ///   4. Update BKT mastery per skill
    ///   5. Generate and persist competency profile
    /// </summary>
    public async Task<CompetencyProfile?> ProcessSessionAsync(
        Guid sessionId,
        string userId,
        List<(int QuestionId, bool IsCorrect, int? DifficultyLevel, int? SkillId)> responses)
    {
        if (responses.Count == 0) return null;

        // 1. Resolve IRT params (calibrated if available, else defaults)
        var irtResponses = await BuildIrtResponsesAsync(responses);

        // 2. Estimate theta
        IrtThetaEstimate? thetaEst = await _ml.EstimateThetaAsync(irtResponses);
        double theta = thetaEst?.Theta ?? FallbackTheta(responses);
        double seTheta = thetaEst?.Se ?? 1.0;

        // 3. Persist theta
        await UpsertSessionIrtEstimateAsync(sessionId, theta, seTheta, thetaEst?.Method ?? "Fallback");

        // 4. Update BKT mastery per skill
        var skillMasteries = await UpdateSkillMasteriesAsync(userId, responses);

        // 5. Generate profile
        return await GenerateAndPersistProfileAsync(userId, sessionId, theta, seTheta, skillMasteries);
    }

    /// <summary>
    /// Get the latest competency profile for a user.
    /// </summary>
    public async Task<CompetencyProfile?> GetLatestProfileAsync(string userId)
    {
        return await _context.CompetencyProfiles
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.GeneratedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get default IRT params for a question (used in CAT before calibration).
    /// </summary>
    public async Task<QuestionIrtParams> GetOrCreateIrtParamsAsync(int questionId, int? difficultyLevel)
    {
        var existing = await _context.QuestionIrtParams.FindAsync(questionId);
        if (existing != null) return existing;

        var defaultB = (difficultyLevel is >= 1 and <= 5)
            ? (decimal)DifficultyToBParam[difficultyLevel.Value]
            : 0.0m;

        return new QuestionIrtParams
        {
            QuestionId = questionId,
            AParam = 1.0m,
            BParam = defaultB,
            CParam = 0.25m,
            IsCalibrated = false
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<List<IrtItemResponse>> BuildIrtResponsesAsync(
        List<(int QuestionId, bool IsCorrect, int? DifficultyLevel, int? SkillId)> responses)
    {
        var questionIds = responses.Select(r => r.QuestionId).ToList();
        var irtParamsMap = await _context.QuestionIrtParams
            .Where(p => questionIds.Contains(p.QuestionId))
            .ToDictionaryAsync(p => p.QuestionId);

        return responses.Select(r =>
        {
            double a, b, c;
            if (irtParamsMap.TryGetValue(r.QuestionId, out var p))
            {
                a = (double)p.AParam;
                b = (double)p.BParam;
                c = (double)p.CParam;
            }
            else
            {
                a = 1.0;
                b = (r.DifficultyLevel is >= 1 and <= 5)
                    ? DifficultyToBParam[r.DifficultyLevel.Value]
                    : 0.0;
                c = 0.25;
            }
            return new IrtItemResponse(r.QuestionId, r.IsCorrect, new IrtItemParams(a, b, c));
        }).ToList();
    }

    private static double FallbackTheta(
        List<(int QuestionId, bool IsCorrect, int? DifficultyLevel, int? SkillId)> responses)
    {
        // Simple fallback: proportion correct → logit scale
        double pct = responses.Count > 0
            ? (double)responses.Count(r => r.IsCorrect) / responses.Count
            : 0.5;
        pct = Math.Clamp(pct, 0.01, 0.99);
        return Math.Log(pct / (1.0 - pct)); // logit
    }

    private async Task UpsertSessionIrtEstimateAsync(
        Guid sessionId, double theta, double seTheta, string method)
    {
        var existing = await _context.SessionIrtEstimates.FindAsync(sessionId);
        if (existing != null)
        {
            existing.Theta = (decimal)theta;
            existing.SeTheta = (decimal)seTheta;
            existing.Method = method;
            existing.ComputedAt = DateTime.UtcNow;
        }
        else
        {
            _context.SessionIrtEstimates.Add(new SessionIrtEstimate
            {
                SessionId = sessionId,
                Theta = (decimal)theta,
                SeTheta = (decimal)seTheta,
                Method = method,
                ComputedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();
    }

    private async Task<List<SkillMasteryInput>> UpdateSkillMasteriesAsync(
        string userId,
        List<(int QuestionId, bool IsCorrect, int? DifficultyLevel, int? SkillId)> responses)
    {
        // Group responses by skill
        var bySkill = responses
            .Where(r => r.SkillId.HasValue)
            .GroupBy(r => r.SkillId!.Value)
            .ToList();

        if (bySkill.Count == 0) return [];

        // Load existing mastery states and skill/domain metadata
        var skillIds = bySkill.Select(g => g.Key).ToList();
        var existingMasteries = await _context.UserSkillMasteries
            .Where(m => m.UserId == userId && skillIds.Contains(m.SkillId))
            .ToDictionaryAsync(m => m.SkillId);

        var skills = await _context.Skills.Include(s => s.Domain)
            .Where(s => skillIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id);
        if (skills.Count != skillIds.Count)
            throw new InvalidOperationException("Cannot update mastery for a skill outside the competency catalogue.");

        var masteryInputs = new List<SkillMasteryInput>();

        foreach (var group in bySkill)
        {
            int skillId = group.Key;
            double priorMastery = existingMasteries.TryGetValue(skillId, out var m)
                ? (double)m.MasteryProb
                : 0.3; // BKT P(L0) default

            double posteriorMastery = priorMastery;
            int count = 0;

            foreach (var response in group)
            {
                var result = await _ml.UpdateMasteryAsync(skillId, posteriorMastery, response.IsCorrect);
                posteriorMastery = result?.PosteriorMastery ?? SimpleBktUpdate(posteriorMastery, response.IsCorrect);
                count++;
            }

            // Upsert mastery
            if (existingMasteries.TryGetValue(skillId, out var existing))
            {
                existing.MasteryProb = (decimal)posteriorMastery;
                existing.ResponseCount += count;
                existing.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                _context.UserSkillMasteries.Add(new UserSkillMastery
                {
                    UserId = userId,
                    SkillId = skillId,
                    MasteryProb = (decimal)posteriorMastery,
                    ResponseCount = count,
                    LastUpdated = DateTime.UtcNow
                });
            }

            var skill = skills[skillId];
            masteryInputs.Add(new SkillMasteryInput(
                SkillId: skillId,
                SkillName: skill.Name,
                DomainId: skill.DomainId,
                DomainName: skill.Domain.Name,
                MasteryProb: posteriorMastery,
                Weight: (double)skill.Weight
            ));
        }

        await _context.SaveChangesAsync();
        return masteryInputs;
    }

    private static double SimpleBktUpdate(double prior, bool isCorrect)
    {
        const double pS = 0.1, pG = 0.25, pT = 0.1;
        double num = isCorrect
            ? prior * (1 - pS)
            : prior * pS;
        double den = isCorrect
            ? prior * (1 - pS) + (1 - prior) * pG
            : prior * pS + (1 - prior) * (1 - pG);
        double posterior = den < 1e-12 ? prior : num / den;
        return Math.Clamp(posterior + (1 - posterior) * pT, 0.0, 1.0);
    }

    private async Task<CompetencyProfile?> GenerateAndPersistProfileAsync(
        string userId, Guid sessionId, double theta, double seTheta,
        List<SkillMasteryInput> skillMasteries)
    {
        if (skillMasteries.Count == 0)
        {
            _logger.LogInformation("No skill masteries found for session {SessionId}; skipping profile.", sessionId);
            return null;
        }

        var profileRequest = new GenerateProfileRequest(
            UserId: userId,
            SessionId: sessionId.ToString(),
            Theta: theta,
            SeTheta: seTheta,
            SkillMasteries: skillMasteries
        );

        var profileResult = await _ml.GenerateProfileAsync(profileRequest);
        if (profileResult == null) return null;

        // Build JSONB scores document
        var scoresDict = profileResult.DomainScores.ToDictionary(
            d => d.DomainName.ToLowerInvariant().Replace(" ", "_"),
            d => d.Score);
        var scoresJson = JsonDocument.Parse(JsonSerializer.Serialize(scoresDict));

        var profile = new CompetencyProfile
        {
            ProfileId = Guid.NewGuid(),
            UserId = userId,
            SessionId = sessionId,
            Scores = scoresJson,
            Theta = (decimal)theta,
            OverallLevel = profileResult.OverallLevel,
            OverallScore = (decimal)profileResult.OverallScore,
            GeneratedAt = DateTime.UtcNow
        };

        _context.CompetencyProfiles.Add(profile);
        await _context.SaveChangesAsync();
        return profile;
    }
}
