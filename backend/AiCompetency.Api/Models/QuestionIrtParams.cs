using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiCompetency.Api.Models;

/// <summary>
/// Stores calibrated IRT 3PL parameters for each question.
/// Populated by the ML service calibration job once enough responses exist.
/// Falls back to defaults (a=1, b mapped from difficulty_level, c=0.25) until calibrated.
/// </summary>
[Table("question_irt_params")]
public class QuestionIrtParams
{
    [Key]
    [Column("question_id")]
    public int QuestionId { get; set; }

    /// <summary>Discrimination parameter a (default 1.0, range 0.3–3.0)</summary>
    [Column("a_param")]
    public decimal AParam { get; set; } = 1.0m;

    /// <summary>Difficulty parameter b in logit scale (default 0.0, range -4 to +4)</summary>
    [Column("b_param")]
    public decimal BParam { get; set; } = 0.0m;

    /// <summary>Pseudo-guessing parameter c (default 0.25, range 0–0.4)</summary>
    [Column("c_param")]
    public decimal CParam { get; set; } = 0.25m;

    /// <summary>Standard error of b estimate</summary>
    [Column("se_b")]
    public decimal? SeB { get; set; }

    [Column("calibrated_at")]
    public DateTime? CalibratedAt { get; set; }

    [Column("response_count")]
    public int ResponseCount { get; set; } = 0;

    [Column("is_calibrated")]
    public bool IsCalibrated { get; set; } = false;

    public Question? Question { get; set; }
}
