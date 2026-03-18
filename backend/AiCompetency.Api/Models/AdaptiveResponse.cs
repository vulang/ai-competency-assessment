using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiCompetency.Api.Models;

[Table("adaptive_responses")]
public class AdaptiveResponse
{
    [Key]
    [Column("response_id")]
    public Guid Id { get; set; }

    [Column("session_id")]
    public Guid SessionId { get; set; }

    [Column("question_id")]
    public int QuestionId { get; set; }

    [Column("question_difficulty_level")]
    public decimal QuestionDifficultyLevel { get; set; }

    [Column("user_answer")]
    [Required]
    public string UserAnswer { get; set; } = string.Empty;

    [Column("is_correct")]
    public bool IsCorrect { get; set; }

    [Column("ability_after_response")]
    public decimal AbilityAfterResponse { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AdaptiveTestSession? Session { get; set; }
    public Question? Question { get; set; }
}
