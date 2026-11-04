using System.Text.Json;
using AiCompetency.Api.Data;
using AiCompetency.Api.Models;

namespace AiCompetency.Api.Services;

/// <summary>Stored in exams.blueprint. LevelMix holds target percentages per level (1–3); empty means any level.</summary>
public sealed record TestBlueprintSpec
{
    public Dictionary<string, int> GroupQuotas { get; init; } = new();
    public Dictionary<int, int> LevelMix { get; init; } = new();
    public long Seed { get; init; }
    public int PassPercent { get; init; } = 70;
}

public sealed record AssemblyResult(List<Question> Questions, List<string> Warnings);

public static class TestBlueprints
{
    // Thesis §2.12: a short form of 35–42 items, about 5–6 per competency group.
    public const int StandardMinPerGroup = 5;
    public const int StandardMaxPerGroup = 6;
    public const int StandardMinTotal = 35;
    public const int StandardMaxTotal = 42;
    public const int MaxPerGroup = 20;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<string> GroupCodes { get; } = CompetencyCatalog.Groups.Select(g => g.Code).ToList();

    public static string Serialize(TestBlueprintSpec spec) => JsonSerializer.Serialize(spec, Json);
    public static TestBlueprintSpec? Deserialize(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<TestBlueprintSpec>(json, Json);

    public static bool IsEligible(Question q) =>
        q.Status == QuestionStatus.Published && q.ReviewedAt != null && ScoringService.IsObjective(q.Type) &&
        q.GroupCode != null && q.TargetLevel != null;

    // Mirrors IsEligible in a form EF can translate.
    public static IQueryable<Question> Eligible(IQueryable<Question> questions) => questions.Where(q =>
        q.Status == QuestionStatus.Published && q.ReviewedAt != null &&
        (q.Type == "mcq_single" || q.Type == "mcq_multi" || q.Type == "true_false") &&
        q.GroupCode != null && q.TargetLevel != null);

    public static string? Validate(TestBlueprintSpec spec)
    {
        if (spec.GroupQuotas.Keys.Any(code => !GroupCodes.Contains(code)))
            return "Group quotas must use the seven framework group codes.";
        if (spec.GroupQuotas.Values.Any(v => v is < 0 or > MaxPerGroup))
            return $"Each group quota must be between 0 and {MaxPerGroup}.";
        if (spec.GroupQuotas.Values.Sum() == 0)
            return "Assign at least one question to a group.";
        if (spec.LevelMix.Keys.Any(level => level is < 1 or > 3) || spec.LevelMix.Values.Any(v => v is < 0 or > 100))
            return "Level mix percentages apply to levels 1–3 and must be between 0 and 100.";
        if (spec.LevelMix.Count > 0 && spec.LevelMix.Values.Sum() != 100)
            return "Level mix percentages must add up to 100.";
        if (spec.PassPercent is < 1 or > 100)
            return "Passing score must be between 1 and 100 percent.";
        return null;
    }

    /// <summary>Lists where a form departs from the thesis short-form specification.</summary>
    public static List<string> StandardIssues(IReadOnlyDictionary<string, int> countsByGroup)
    {
        var issues = new List<string>();
        foreach (var code in GroupCodes)
        {
            var count = countsByGroup.GetValueOrDefault(code);
            if (count is < StandardMinPerGroup or > StandardMaxPerGroup)
                issues.Add($"{code} has {count} questions; a standard form needs {StandardMinPerGroup}–{StandardMaxPerGroup}.");
        }
        var total = countsByGroup.Values.Sum();
        if (total is < StandardMinTotal or > StandardMaxTotal)
            issues.Add($"The form has {total} questions; a standard form needs {StandardMinTotal}–{StandardMaxTotal}.");
        return issues;
    }

    /// <summary>
    /// Selects questions per group quota and level mix. The same seed and bank always give the same form.
    /// When a level runs short, the group is filled from its other levels and a warning is recorded.
    /// </summary>
    public static AssemblyResult Assemble(TestBlueprintSpec spec, IEnumerable<Question> pool)
    {
        var eligible = pool.Where(IsEligible).ToList();
        var selected = new List<Question>();
        var warnings = new List<string>();
        foreach (var code in GroupCodes)
        {
            var quota = spec.GroupQuotas.GetValueOrDefault(code);
            if (quota == 0) continue;
            var candidates = eligible.Where(q => q.GroupCode == code).OrderBy(q => Rank(spec.Seed, q.Id)).ToList();
            var picked = new List<Question>();
            foreach (var (level, target) in LevelTargets(quota, spec.LevelMix))
            {
                var atLevel = candidates.Where(q => q.TargetLevel == level).Take(target).ToList();
                if (atLevel.Count < target)
                    warnings.Add($"{code} level {level}: {atLevel.Count} of {target} questions available.");
                picked.AddRange(atLevel);
            }
            var fill = candidates.Except(picked).Take(quota - picked.Count).ToList();
            if (fill.Count > 0 && spec.LevelMix.Count > 0)
                warnings.Add($"{code}: filled {fill.Count} question(s) from other levels.");
            picked.AddRange(fill);
            if (picked.Count < quota)
                warnings.Add($"{code}: only {picked.Count} of {quota} reviewed, published objective questions are available.");
            selected.AddRange(picked.OrderBy(q => q.TargetLevel).ThenBy(q => Rank(spec.Seed, q.Id)));
        }
        return new AssemblyResult(selected, warnings);
    }

    /// <summary>Splits a group quota across levels by largest remainder.</summary>
    public static List<(int Level, int Count)> LevelTargets(int quota, IReadOnlyDictionary<int, int> mix)
    {
        if (mix.Count == 0) return [];
        var exact = Enumerable.Range(1, 3).Select(level => (Level: level, Value: quota * mix.GetValueOrDefault(level) / 100m)).ToList();
        var counts = exact.ToDictionary(e => e.Level, e => (int)Math.Floor(e.Value));
        foreach (var extra in exact.OrderByDescending(e => e.Value - Math.Floor(e.Value)).ThenBy(e => e.Level)
                     .Take(quota - counts.Values.Sum()))
            counts[extra.Level]++;
        return counts.Where(c => c.Value > 0).Select(c => (c.Key, c.Value)).OrderBy(c => c.Key).ToList();
    }

    // SplitMix64 gives a stable order across runtimes, unlike System.Random.
    public static ulong Rank(long seed, int questionId)
    {
        var z = unchecked((ulong)seed * 0x9E3779B97F4A7C15UL + (ulong)questionId);
        z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
        z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
        return z ^ (z >> 31);
    }
}
