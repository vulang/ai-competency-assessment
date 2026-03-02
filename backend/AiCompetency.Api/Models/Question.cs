using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AiCompetency.Api.Models;

[Table("questions")]
public class Question
{
    [Key]
    [Column("question_id")]
    public int Id { get; set; }

    [Column("skill_id")]
    public int? SkillId { get; set; }

    [Column("type")]
    [Required]
    [MaxLength(32)]
    public string Type { get; set; } = string.Empty;

    [Column("content")]
    [Required]
    public string Content { get; set; } = string.Empty;

    [Column("metadata", TypeName = "jsonb")]
    public JsonDocument Metadata { get; set; } = JsonDocument.Parse("{}");

    [Column("difficulty_level")]
    public int? DifficultyLevel { get; set; }

    [Column("rubric_scoring")]
    public string? RubricScoring { get; set; }
    [Column("status")]
    public QuestionStatus Status { get; set; } = QuestionStatus.Draft;
}

public enum QuestionStatus
{
    Draft,
    Published,
    Archived
}
