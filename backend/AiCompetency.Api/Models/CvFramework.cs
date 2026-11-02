using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiCompetency.Api.Models;

[Table("competency_framework_versions")]
public sealed class CvFrameworkVersion
{
    [Key] public int Id { get; set; }
    [MaxLength(64)] public string Version { get; set; } = "";
    [MaxLength(16)] public string Status { get; set; } = "Draft";
    public string Source { get; set; } = "";
    public DateTimeOffset? PublishedAt { get; set; }
    public bool IsActive { get; set; }
    public ICollection<CvCompetencyGroup> Groups { get; set; } = [];
}

[Table("proficiency_levels")]
public sealed class ProficiencyLevel
{
    [Key] public int Ordinal { get; set; }
    [MaxLength(32)] public string Code { get; set; } = "";
    public string NameVi { get; set; } = "";
    public string NameEn { get; set; } = "";
}

[Table("competency_groups")]
public sealed class CvCompetencyGroup
{
    public int FrameworkVersionId { get; set; }
    [MaxLength(64)] public string Code { get; set; } = "";
    public string NameVi { get; set; } = "";
    public string NameEn { get; set; } = "";
    public int Order { get; set; }
    public ICollection<BehaviorIndicator> Indicators { get; set; } = [];
    public ICollection<CvRubricRule> RubricRules { get; set; } = [];
}

[Table("behavior_indicators")]
public sealed class BehaviorIndicator
{
    [Key] public int Id { get; set; }
    public int FrameworkVersionId { get; set; }
    [MaxLength(64)] public string GroupCode { get; set; } = "";
    [MaxLength(96)] public string Code { get; set; } = "";
    public int Level { get; set; }
    public string TextVi { get; set; } = "";
    public string TextEn { get; set; } = "";
}

[Table("cv_rubric_rules")]
public sealed class CvRubricRule
{
    [Key] public int Id { get; set; }
    public int FrameworkVersionId { get; set; }
    [MaxLength(64)] public string GroupCode { get; set; } = "";
    public int Level { get; set; }
    public string AcceptedEvidence { get; set; } = "";
    public string InsufficientEvidence { get; set; } = "";
    public string Example { get; set; } = "";
}
