using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiCompetency.Api.Models;

/// <summary>
/// Stores the IRT theta estimate computed after each test session completes.
/// </summary>
[Table("session_irt_estimates")]
public class SessionIrtEstimate
{
    [Key]
    [Column("session_id")]
    public Guid SessionId { get; set; }

    /// <summary>Ability estimate in logit scale (typically -4 to +4).</summary>
    [Column("theta")]
    public decimal Theta { get; set; }

    /// <summary>Standard error of the theta estimate.</summary>
    [Column("se_theta")]
    public decimal SeTheta { get; set; } = 1.0m;

    /// <summary>Estimation method: MLE or EAP.</summary>
    [Column("method")]
    [MaxLength(16)]
    public string Method { get; set; } = "EAP";

    [Column("computed_at")]
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;

    public ExamResult? Session { get; set; }
}
