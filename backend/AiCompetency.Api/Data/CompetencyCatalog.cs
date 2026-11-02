using System.Text.Json;
using AiCompetency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AiCompetency.Api.Data;

// Compatibility catalogue sourced from question-generator/data/framework_mapping.yaml.
// Phase 1 will add reviewed, versioned behavior indicators and CV rubrics.
public static class CompetencyCatalog
{
    public sealed record Group(int Id, string Code, string Name, string Description, Dictionary<string, string> LevelFocus);
    public static IReadOnlyList<Group> Groups { get; } = LoadGroups();

    private static IReadOnlyList<Group> LoadGroups()
    {
        using var stream = typeof(CompetencyCatalog).Assembly.GetManifestResourceStream("AiCompetency.Api.Data.competency-framework.json")!;
        return JsonSerializer.Deserialize<List<Group>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    public static string Normalize(string value) => string.Concat(value.Where(char.IsLetterOrDigit)).ToUpperInvariant();
    public static string? GroupCode(string? group)
    {
        var normalized = Normalize(group ?? "");
        if (normalized is "AIFUNDAMENTALS" or "CG1") return "FOUNDATION";
        return Groups.FirstOrDefault(g => Normalize(g.Code) == normalized || Normalize(g.Name) == normalized)?.Code;
    }

    public static string? LevelName(string? level) => Normalize(level ?? "") switch
    {
        "BASIC" or "FOUNDATION" or "NENTANG" or "CL1" => "Basic",
        "INTERMEDIATE" or "APPLY" or "APDUNG" or "CL2" => "Intermediate",
        "ADVANCED" or "CREATE" or "KIENTAO" or "CL3" => "Advanced",
        _ => null
    };

    public static void Seed(ModelBuilder builder)
    {
        builder.Entity<CompetencyFramework>().HasData(new CompetencyFramework { Id = 1, Name = "AI competency compatibility framework", Version = "p0-v1", IsActive = true });
        foreach (var group in Groups)
        {
            builder.Entity<CompetencyDomain>().HasData(new CompetencyDomain { Id = group.Id, FrameworkId = 1, Code = group.Code, Name = group.Name, Weight = 1m });
            builder.Entity<CompetencySkill>().HasData(new CompetencySkill { Id = group.Id, DomainId = group.Id, Name = group.Name, Description = group.Description, Weight = 1m, IsDefault = true });
            var level = 0;
            foreach (var name in new[] { "Basic", "Intermediate", "Advanced" })
            {
                level++;
                builder.Entity<CompetencyCriterion>().HasData(new CompetencyCriterion { Id = (group.Id - 1) * 3 + level, SkillId = group.Id, Name = name, Level = level, Description = group.LevelFocus[name] });
            }
        }
    }
}
