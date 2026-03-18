namespace AiCompetency.Api.DTOs;

public class TestBlueprintDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int PassingScore { get; set; }
    public int QuestionCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateTestBlueprintDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int PassingScore { get; set; }
    public int QuestionCount { get; set; }
    public string Status { get; set; } = "draft";
}
