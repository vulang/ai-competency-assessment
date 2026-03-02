using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiCompetency.Api.Models;

[Table("test_sessions")]
public class ExamResult
{
    [Key]
    [Column("session_id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("exam_id")]
    public int ExamId { get; set; }

    [Column("start_time")]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    [Column("end_time")]
    public DateTime? EndTime { get; set; }

    [Column("total_score")]
    public decimal TotalScore { get; set; }

    [Column("status")]
    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = string.Empty;

    public Exam? Exam { get; set; }
    
    // Using string for User temporarily as User entity is not strictly defined in this task but exists in schema
    // If we need the User object, we'd add it here. For now, just the ID is enough for the foreign key.
    
    public ICollection<Response> Responses { get; set; } = new List<Response>();
}
