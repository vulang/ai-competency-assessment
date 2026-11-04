using AiCompetency.Api.Services;

namespace AiCompetency.Api.DTOs;

public class TestBlueprintDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    /// <summary>Raw points needed to pass, fixed when the test is published.</summary>
    public int PassingScore { get; set; }
    public int QuestionCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsStandard { get; set; }
    public TestBlueprintSpec? Blueprint { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public sealed class TestDetailDto : TestBlueprintDto
{
    public List<TestQuestionDto> Questions { get; set; } = [];
    public List<GroupCoverageDto> Coverage { get; set; } = [];
    public List<string> StandardIssues { get; set; } = [];
    public bool HasSessions { get; set; }
}

public sealed record TestQuestionDto(
    int QuestionId, int OrderIndex, decimal PointValue, string Title, string Type,
    string? GroupCode, int? TargetLevel, int? Difficulty, string Status, bool Eligible);

/// <summary>Questions per group and level against the blueprint quota.</summary>
public sealed record GroupCoverageDto(string GroupCode, string GroupName, int Quota, int Total, int Basic, int Intermediate, int Advanced);

public sealed class SaveTestRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 60;
    public int PassPercent { get; set; } = 70;
    public bool IsStandard { get; set; }
    public Dictionary<string, int> GroupQuotas { get; set; } = new();
    public Dictionary<int, int> LevelMix { get; set; } = new();
    /// <summary>Omit to draw a new random seed; the seed is stored so assembly can be reproduced.</summary>
    public long? Seed { get; set; }
}

public sealed record TestPreviewDto(long Seed, List<TestQuestionDto> Questions, List<GroupCoverageDto> Coverage,
    List<string> Warnings, List<string> StandardIssues);

public sealed record TestQuestionsRequest(List<int> QuestionIds);

public sealed record AssembleTestRequest(long? Seed);
