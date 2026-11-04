using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiCompetency.Api.Models;

[Table("exams")]
public class Exam
{
    [Key]
    [Column("exam_id")]
    public int Id { get; set; }

    [Column("title")]
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Column("duration_minutes")]
    public int DurationMinutes { get; set; }

    [Column("pass_score")]
    public int PassScore { get; set; }

    [Column("question_count")]
    public int QuestionCount { get; set; }

    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "draft";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Serialized TestBlueprintSpec; null for exams created before Phase 2.
    [Column("blueprint", TypeName = "jsonb")]
    public string? Blueprint { get; set; }

    // The single active standard form delivered by start-standard.
    [Column("is_standard")]
    public bool IsStandard { get; set; }

    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }

    public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
}
