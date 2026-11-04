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

    public CompetencySkill? Skill { get; set; }

    [Column("group_code"), MaxLength(64)]
    public string? GroupCode { get; set; }

    [Column("target_level")]
    public int? TargetLevel { get; set; }


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

    // Expert review is required before a question can enter an experimental test.
    [Column("reviewed_by"), MaxLength(256)]
    public string? ReviewedBy { get; set; }

    [Column("reviewed_at")]
    public DateTimeOffset? ReviewedAt { get; set; }

    [Column("review_note")]
    public string? ReviewNote { get; set; }
}

public enum QuestionStatus
{
    Draft,
    Reviewed,
    Published,
    Archived
}
