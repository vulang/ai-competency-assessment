using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiCompetency.Api.Models;

[Table("frameworks")]
public sealed class CompetencyFramework
{
    [Key, Column("framework_id")] public int Id { get; set; }
    [Required, MaxLength(255), Column("name")] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(64), Column("version")] public string Version { get; set; } = string.Empty;
    [Column("is_active")] public bool IsActive { get; set; }
    public ICollection<CompetencyDomain> Domains { get; set; } = [];
}

[Table("domains")]
public sealed class CompetencyDomain
{
    [Key, Column("domain_id")] public int Id { get; set; }
    [Column("framework_id")] public int FrameworkId { get; set; }
    [Required, MaxLength(64), Column("code")] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(255), Column("name")] public string Name { get; set; } = string.Empty;
    [Column("weight")] public decimal Weight { get; set; } = 1m;
    public CompetencyFramework Framework { get; set; } = null!;
    public ICollection<CompetencySkill> Skills { get; set; } = [];
}

[Table("skills")]
public sealed class CompetencySkill
{
    [Key, Column("skill_id")] public int Id { get; set; }
    [Column("domain_id")] public int DomainId { get; set; }
    [Required, MaxLength(255), Column("name")] public string Name { get; set; } = string.Empty;
    [Column("description")] public string Description { get; set; } = string.Empty;
    [Column("weight")] public decimal Weight { get; set; } = 1m;
    [Column("is_default")] public bool IsDefault { get; set; }
    public CompetencyDomain Domain { get; set; } = null!;
    public ICollection<CompetencyCriterion> Criteria { get; set; } = [];
}

[Table("criteria")]
public sealed class CompetencyCriterion
{
    [Key, Column("criteria_id")] public int Id { get; set; }
    [Column("skill_id")] public int SkillId { get; set; }
    [Required, MaxLength(255), Column("name")] public string Name { get; set; } = string.Empty;
    [Column("level")] public int Level { get; set; }
    [Column("description")] public string Description { get; set; } = string.Empty;
    public CompetencySkill Skill { get; set; } = null!;
}
