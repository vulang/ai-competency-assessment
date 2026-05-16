using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiCompetency.Api.Models;

/// <summary>
/// Persists BKT mastery probability per user per skill.
/// Updated after each test session that includes questions for the skill.
/// </summary>
[Table("user_skill_mastery")]
public class UserSkillMastery
{
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("skill_id")]
    public int SkillId { get; set; }

    /// <summary>Mastery probability from BKT (0.0–1.0).</summary>
    [Column("mastery_prob")]
    public decimal MasteryProb { get; set; } = 0.3m;

    [Column("response_count")]
    public int ResponseCount { get; set; } = 0;

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
