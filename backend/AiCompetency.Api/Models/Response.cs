using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiCompetency.Api.Models;

[Table("responses")]
public class Response
{
    [Key]
    [Column("response_id")]
    public Guid Id { get; set; }

    [Column("session_id")]
    public Guid SessionId { get; set; }

    [Column("question_id")]
    public int QuestionId { get; set; }

    [Column("final_answer")]
    [Required]
    public string FinalAnswer { get; set; } = string.Empty;

    [Column("score_earned")]
    public decimal ScoreEarned { get; set; }

    [Column("ai_feedback")]
    public string? AiFeedback { get; set; }

    [Column("submitted_at")]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public ExamResult? ExamResult { get; set; }
    public Question? Question { get; set; }
}
