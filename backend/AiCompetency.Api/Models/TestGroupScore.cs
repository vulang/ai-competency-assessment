using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiCompetency.Api.Models;

/// <summary>
/// Per-group score for one completed standard test session (thesis §2.12).
/// Only objective items contribute; the level uses the mapping recorded in LevelMappingVersion.
/// </summary>
[Table("test_group_scores")]
public class TestGroupScore
{
    [Column("session_id")]
    public Guid SessionId { get; set; }

    [Column("group_code"), MaxLength(64)]
    public string GroupCode { get; set; } = string.Empty;

    [Column("raw_score")]
    public decimal RawScore { get; set; }

    [Column("max_score")]
    public decimal MaxScore { get; set; }

    [Column("item_count")]
    public int ItemCount { get; set; }

    [Column("percent")]
    public decimal Percent { get; set; }

    /// <summary>1 = Basic, 2 = Intermediate, 3 = Advanced; null until an approved mapping applies.</summary>
    [Column("level")]
    public int? Level { get; set; }

    [Column("level_mapping_version"), MaxLength(64)]
    public string? LevelMappingVersion { get; set; }

    [Column("computed_at")]
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;

    public ExamResult? Session { get; set; }
}
