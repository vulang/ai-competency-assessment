using System.Text.Json;
using AiCompetency.Api.Models;

namespace AiCompetency.Api.Services;

// ─── DTOs for ML service communication ───────────────────────────────────────

public record IrtItemResponse(int QuestionId, bool IsCorrect, IrtItemParams IrtParams);
public record IrtItemParams(double A, double B, double C);
public record IrtThetaEstimate(double Theta, double Se, string Method, double FisherInfo);
public record IrtNextItemResult(int SelectedItemId, double FisherInfoAtTheta);

public record BktUpdateRequest(int SkillId, double PriorMastery, bool IsCorrect, BktParams Params);
public record BktParams(double PLo = 0.3, double PT = 0.1, double PS = 0.1, double PG = 0.25);
public record BktUpdateResult(int SkillId, double PosteriorMastery, bool Mastered);

public record SkillMasteryInput(int SkillId, string SkillName, int DomainId, string DomainName, double MasteryProb, double Weight = 1.0);
public record GenerateProfileRequest(int UserId, string SessionId, double Theta, double SeTheta, List<SkillMasteryInput> SkillMasteries);
public record DomainScoreResult(int DomainId, string DomainName, double Score, int MasteryCount, int TotalSkills);
public record CompetencyProfileResult(int UserId, string SessionId, double Theta, string OverallLevel, List<DomainScoreResult> DomainScores, double OverallScore);

// ─── Service ──────────────────────────────────────────────────────────────────

/// <summary>
/// HTTP client wrapper for the Python ML microservice.
/// Handles IRT theta estimation, BKT mastery updates, and competency profile generation.
/// </summary>
public class MlService
{
    private readonly HttpClient _http;
    private readonly ILogger<MlService> _logger;

    public MlService(HttpClient http, ILogger<MlService> logger)
    {
        _http = http;
        _logger = logger;
    }

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    // ── IRT ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Estimate candidate ability (θ) from a list of item responses via IRT 3PL.
    /// Returns null on failure; caller should fall back to simple average.
    /// </summary>
    public async Task<IrtThetaEstimate?> EstimateThetaAsync(
        List<IrtItemResponse> responses,
        string method = "EAP")
    {
        var payload = new
        {
            responses,
            method,
            prior_mean = 0.0,
            prior_sd = 1.0
        };

        try
        {
            var json = JsonSerializer.Serialize(payload, _json);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync("/irt/estimate", content);
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(body, _json);
            var est = result.GetProperty("estimate");

            return new IrtThetaEstimate(
                Theta: est.GetProperty("theta").GetDouble(),
                Se: est.GetProperty("se").GetDouble(),
                Method: est.GetProperty("method").GetString() ?? method,
                FisherInfo: est.GetProperty("fisher_info").GetDouble()
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ML service IRT estimate failed. Using fallback.");
            return null;
        }
    }

    /// <summary>
    /// Select next CAT item using Maximum Fisher Information.
    /// Returns null on failure; caller falls back to closest-difficulty selection.
    /// </summary>
    public async Task<IrtNextItemResult?> SelectNextItemAsync(
        double currentTheta,
        List<(int Id, QuestionIrtParams Params)> candidates)
    {
        var payload = new
        {
            current_theta = currentTheta,
            candidate_items = candidates.Select(c => new
            {
                a = (double)c.Params.AParam,
                b = (double)c.Params.BParam,
                c_param = (double)c.Params.CParam
            }).ToList(),
            candidate_item_ids = candidates.Select(c => c.Id).ToList()
        };

        // Remap c_param → c for the Python service model field name
        var payloadFixed = new
        {
            current_theta = currentTheta,
            candidate_items = candidates.Select(c => new
            {
                a = (double)c.Params.AParam,
                b = (double)c.Params.BParam,
                c = (double)c.Params.CParam
            }).ToList(),
            candidate_item_ids = candidates.Select(c => c.Id).ToList()
        };

        try
        {
            var json = JsonSerializer.Serialize(payloadFixed, _json);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync("/irt/next-item", content);
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(body, _json);

            return new IrtNextItemResult(
                SelectedItemId: result.GetProperty("selected_item_id").GetInt32(),
                FisherInfoAtTheta: result.GetProperty("fisher_info_at_theta").GetDouble()
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ML service next-item selection failed. Using fallback.");
            return null;
        }
    }

    // ── BKT ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Update BKT mastery probability for a skill after one observation.
    /// Returns null on failure; caller keeps existing mastery unchanged.
    /// </summary>
    public async Task<BktUpdateResult?> UpdateMasteryAsync(
        int skillId, double priorMastery, bool isCorrect)
    {
        var payload = new BktUpdateRequest(
            SkillId: skillId,
            PriorMastery: priorMastery,
            IsCorrect: isCorrect,
            Params: new BktParams()
        );

        try
        {
            var json = JsonSerializer.Serialize(payload, _json);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync("/kt/update", content);
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<BktUpdateResult>(body, _json);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ML service BKT update failed for skill {SkillId}.", skillId);
            return null;
        }
    }

    // ── Profile ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Generate a competency profile from IRT theta and skill masteries.
    /// </summary>
    public async Task<CompetencyProfileResult?> GenerateProfileAsync(
        GenerateProfileRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _json);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync("/profile/generate", content);
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CompetencyProfileResult>(body, _json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ML service profile generation failed.");
            return null;
        }
    }
}
