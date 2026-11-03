using AiCompetency.Api.Data;
using AiCompetency.Api.Models;

namespace AiCompetency.Api.Services;

public sealed record GroupScoreView(
    string GroupCode, string GroupName, int Order, int ItemCount,
    decimal? RawScore, decimal? MaxScore, decimal? Percent,
    int? Level, string? LevelCode, string? LevelMappingVersion);

public static class TestProfileView
{
    /// <summary>Returns all seven groups in framework order; unmeasured groups have null scores.</summary>
    public static List<GroupScoreView> SevenGroups(IEnumerable<TestGroupScore> scores)
    {
        var byCode = scores.ToDictionary(s => s.GroupCode);
        return CompetencyCatalog.Groups.Select(g => byCode.TryGetValue(g.Code, out var s)
                ? new GroupScoreView(g.Code, g.Name, g.Id, s.ItemCount, s.RawScore, s.MaxScore, s.Percent,
                    s.Level, GroupScoreCalculator.LevelCode(s.Level), s.LevelMappingVersion)
                : new GroupScoreView(g.Code, g.Name, g.Id, 0, null, null, null, null, null, null))
            .ToList();
    }
}
