using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AiCompetency.Api.Models;

/// <summary>
/// Aggregated competency profile generated after a test session.
/// Combines IRT theta + BKT mastery into per-domain scores (0–100).
/// </summary>
[Table("competency_profiles")]
public class CompetencyProfile
{
    [Key]
    [Column("profile_id")]
    public Guid ProfileId { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("session_id")]
    public Guid? SessionId { get; set; }

    /// <summary>
    /// JSONB column storing domain scores.
    /// Example: {"ai_fundamentals": 72.5, "data": 85.0, "critical_thinking": 68.3}
    /// </summary>
    [Column("scores", TypeName = "jsonb")]
    public JsonDocument Scores { get; set; } = JsonDocument.Parse("{}");

    /// <summary>IRT theta from the associated session.</summary>
    [Column("theta")]
    public decimal? Theta { get; set; }

    /// <summary>Foundation | Apply | Create</summary>
    [Column("overall_level")]
    [MaxLength(32)]
    public string OverallLevel { get; set; } = "Foundation";

    /// <summary>Weighted overall score 0–100.</summary>
    [Column("overall_score")]
    public decimal OverallScore { get; set; } = 0m;

    [Column("generated_at")]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
