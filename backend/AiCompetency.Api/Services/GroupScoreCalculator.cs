using AiCompetency.Api.Models;

namespace AiCompetency.Api.Services;

public sealed record ScoredItem(Question Question, decimal Points, decimal Earned);

public static class GroupScoreCalculator
{
    /// <summary>
    /// Sums objective items by competency group. Unanswered items count as zero.
    /// </summary>
    public static List<TestGroupScore> Compute(Guid sessionId, IEnumerable<ScoredItem> items, TestLevelMapping? mapping)
    {
        var now = DateTime.UtcNow;
        return items
            .Where(i => i.Question.GroupCode != null && ScoringService.IsObjective(i.Question.Type) && i.Points > 0)
            .GroupBy(i => i.Question.GroupCode!)
            .Select(g =>
            {
                var max = g.Sum(i => i.Points);
                var raw = g.Sum(i => Math.Clamp(i.Earned, 0, i.Points));
                var percent = Math.Round(raw / max * 100m, 2);
                var count = g.Count();
                return new TestGroupScore
                {
                    SessionId = sessionId,
                    GroupCode = g.Key,
                    RawScore = raw,
                    MaxScore = max,
                    ItemCount = count,
                    Percent = percent,
                    Level = MapLevel(percent, count, mapping),
                    LevelMappingVersion = mapping?.Version,
                    ComputedAt = now
                };
            })
            .ToList();
    }

    public static int? MapLevel(decimal percent, int itemCount, TestLevelMapping? mapping)
    {
        if (mapping == null || itemCount < mapping.MinItemsPerGroup) return null;
        if (percent >= mapping.AdvancedMinPercent) return 3;
        return percent >= mapping.IntermediateMinPercent ? 2 : 1;
    }

    public static string? LevelCode(int? level) => level switch
    {
        1 => "BASIC",
        2 => "INTERMEDIATE",
        3 => "ADVANCED",
        _ => null
    };
}
