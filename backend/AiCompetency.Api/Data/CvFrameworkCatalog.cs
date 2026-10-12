using System.Text.Json;
using AiCompetency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AiCompetency.Api.Data;

public static class CvFrameworkCatalog
{
    private sealed record Seed(string Version, string Status, string Source, List<ProficiencyLevel> Levels, List<GroupSeed> Groups);
    private sealed record GroupSeed(string Code, string NameVi, string NameEn, int Order,
        List<BehaviorIndicator> Indicators, List<CvRubricRule> RubricRules);

    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<CvFrameworkVersion>().HasIndex(v => v.Version).IsUnique();
        builder.Entity<CvFrameworkVersion>().HasIndex(v => v.IsActive).IsUnique().HasFilter("\"IsActive\" = true");
        builder.Entity<CvFrameworkVersion>().ToTable("competency_framework_versions", t =>
            t.HasCheckConstraint("ck_cv_framework_publication", "(\"Status\" = 'Draft' AND \"PublishedAt\" IS NULL AND NOT \"IsActive\") OR (\"Status\" = 'Published' AND \"PublishedAt\" IS NOT NULL)"));
        builder.Entity<ProficiencyLevel>().Property(l => l.Ordinal).ValueGeneratedNever();
        builder.Entity<ProficiencyLevel>().HasIndex(l => l.Code).IsUnique();
        builder.Entity<ProficiencyLevel>().ToTable("proficiency_levels", t =>
            t.HasCheckConstraint("ck_proficiency_level_code", "(\"Ordinal\" = 0 AND \"Code\" = 'NO_EVIDENCE') OR (\"Ordinal\" = 1 AND \"Code\" = 'BASIC') OR (\"Ordinal\" = 2 AND \"Code\" = 'INTERMEDIATE') OR (\"Ordinal\" = 3 AND \"Code\" = 'ADVANCED')"));
        builder.Entity<CvCompetencyGroup>().HasKey(g => new { g.FrameworkVersionId, g.Code });
        builder.Entity<CvCompetencyGroup>().HasIndex(g => new { g.FrameworkVersionId, g.Order }).IsUnique();
        builder.Entity<CvCompetencyGroup>().HasOne<CvFrameworkVersion>().WithMany(v => v.Groups)
            .HasForeignKey(g => g.FrameworkVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CvCompetencyGroup>().ToTable("competency_groups", t =>
        {
            t.HasCheckConstraint("ck_cv_group_code", "\"Code\" IN ('FOUNDATION','DATA','CRITICAL_THINKING','APPLICATION','ETHICS','TOOLS','FUTURE_OF_WORK')");
            t.HasCheckConstraint("ck_cv_group_order", "\"Order\" BETWEEN 1 AND 7");
        });
        builder.Entity<BehaviorIndicator>().HasIndex(i => new { i.FrameworkVersionId, i.Code }).IsUnique();
        builder.Entity<BehaviorIndicator>().HasOne<CvCompetencyGroup>().WithMany(g => g.Indicators)
            .HasForeignKey(i => new { i.FrameworkVersionId, i.GroupCode }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<BehaviorIndicator>().HasOne<ProficiencyLevel>().WithMany().HasForeignKey(i => i.Level).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<BehaviorIndicator>().ToTable("behavior_indicators", t =>
            t.HasCheckConstraint("ck_indicator_level", "\"Level\" BETWEEN 1 AND 3"));
        builder.Entity<CvRubricRule>().HasIndex(r => new { r.FrameworkVersionId, r.GroupCode, r.Level }).IsUnique();
        builder.Entity<CvRubricRule>().HasOne<CvCompetencyGroup>().WithMany(g => g.RubricRules)
            .HasForeignKey(r => new { r.FrameworkVersionId, r.GroupCode }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CvRubricRule>().HasOne<ProficiencyLevel>().WithMany().HasForeignKey(r => r.Level).OnDelete(DeleteBehavior.Restrict);

        using var stream = typeof(CvFrameworkCatalog).Assembly.GetManifestResourceStream("AiCompetency.Api.Data.cv-v1-draft.yaml")!;
        var seed = JsonSerializer.Deserialize<Seed>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        builder.Entity<CvFrameworkVersion>().HasData(new CvFrameworkVersion { Id = 1, Version = seed.Version, Status = seed.Status, Source = seed.Source });
        builder.Entity<ProficiencyLevel>().HasData(seed.Levels);
        var indicatorId = 0;
        var ruleId = 0;
        foreach (var group in seed.Groups)
        {
            builder.Entity<CvCompetencyGroup>().HasData(new CvCompetencyGroup {
                FrameworkVersionId = 1, Code = group.Code, NameVi = group.NameVi, NameEn = group.NameEn, Order = group.Order
            });
            foreach (var indicator in group.Indicators)
            {
                indicator.Id = ++indicatorId;
                indicator.FrameworkVersionId = 1;
                indicator.GroupCode = group.Code;
                builder.Entity<BehaviorIndicator>().HasData(indicator);
            }
            foreach (var rule in group.RubricRules)
            {
                rule.Id = ++ruleId;
                rule.FrameworkVersionId = 1;
                rule.GroupCode = group.Code;
                builder.Entity<CvRubricRule>().HasData(rule);
            }
        }
    }
}
