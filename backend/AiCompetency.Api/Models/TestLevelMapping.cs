using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiCompetency.Api.Models;

/// <summary>
/// Predetermined rule that maps a group's percent correct to Basic/Intermediate/Advanced (thesis §2.12).
/// Drafts are editable; a published version is immutable and at most one is active.
/// </summary>
[Table("test_level_mappings")]
public class TestLevelMapping
{
    [Key, Column("id")]
    public int Id { get; set; }

    [Column("version"), MaxLength(64)]
    public string Version { get; set; } = string.Empty;

    [Column("status"), MaxLength(16)]
    public string Status { get; set; } = "Draft";

    [Column("is_active")]
    public bool IsActive { get; set; }

    /// <summary>Percent at or above which a group is Intermediate; below it is Basic.</summary>
    [Column("intermediate_min_percent")]
    public decimal IntermediateMinPercent { get; set; }

    [Column("advanced_min_percent")]
    public decimal AdvancedMinPercent { get; set; }

    /// <summary>Groups measured by fewer objective items receive no level.</summary>
    [Column("min_items_per_group")]
    public int MinItemsPerGroup { get; set; }

    [Column("source")]
    public string Source { get; set; } = string.Empty;

    [Column("reviewed_by"), MaxLength(256)]
    public string? ReviewedBy { get; set; }

    [Column("review_notes")]
    public string? ReviewNotes { get; set; }

    [Column("published_at")]
    public DateTimeOffset? PublishedAt { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
