using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiCompetency.Api.Models;

[Table("adaptive_test_sessions")]
public class AdaptiveTestSession
{
    [Key]
    [Column("session_id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("start_time")]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    [Column("end_time")]
    public DateTime? EndTime { get; set; }

    [Column("status")]
    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = string.Empty;

    [Column("current_ability_estimate")]
    public decimal CurrentAbilityEstimate { get; set; }

    [Column("questions_asked_count")]
    public int QuestionsAskedCount { get; set; }

    public ICollection<AdaptiveResponse> Responses { get; set; } = new List<AdaptiveResponse>();
}
