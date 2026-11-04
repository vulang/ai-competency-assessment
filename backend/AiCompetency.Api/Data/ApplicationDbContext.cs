using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AiCompetency.Api.Data;

using AiCompetency.Api.Models;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<CompetencyFramework> Frameworks { get; set; }
    public DbSet<CompetencyDomain> Domains { get; set; }
    public DbSet<CompetencySkill> Skills { get; set; }
    public DbSet<CompetencyCriterion> Criteria { get; set; }
    public DbSet<CvFrameworkVersion> CvFrameworkVersions { get; set; }
    public DbSet<ProficiencyLevel> ProficiencyLevels { get; set; }
    public DbSet<CvCompetencyGroup> CvCompetencyGroups { get; set; }
    public DbSet<BehaviorIndicator> BehaviorIndicators { get; set; }
    public DbSet<CvRubricRule> CvRubricRules { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<ExamQuestion> ExamQuestions { get; set; }
    public DbSet<ExamResult> ExamResults { get; set; }
    public DbSet<Response> Responses { get; set; }
    public DbSet<TestGroupScore> TestGroupScores { get; set; }
    public DbSet<TestLevelMapping> TestLevelMappings { get; set; }
    public DbSet<QuestionGenerationPlan> QuestionGenerationPlans { get; set; }
    public DbSet<AdaptiveTestSession> AdaptiveTestSessions { get; set; }
    public DbSet<AdaptiveResponse> AdaptiveResponses { get; set; }

    // ML / IRT / BKT tables
    public DbSet<QuestionIrtParams> QuestionIrtParams { get; set; }
    public DbSet<SessionIrtEstimate> SessionIrtEstimates { get; set; }
    public DbSet<UserSkillMastery> UserSkillMasteries { get; set; }
    public DbSet<CompetencyProfile> CompetencyProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CompetencyFramework>().HasIndex(f => f.Version).IsUnique();
        builder.Entity<CompetencyFramework>().HasIndex(f => f.IsActive).IsUnique().HasFilter("is_active = true");
        builder.Entity<CompetencyDomain>().HasIndex(d => new { d.FrameworkId, d.Code }).IsUnique();
        builder.Entity<CompetencyDomain>().HasIndex(d => new { d.FrameworkId, d.Name }).IsUnique();
        builder.Entity<CompetencyDomain>().HasOne(d => d.Framework).WithMany(f => f.Domains)
            .HasForeignKey(d => d.FrameworkId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CompetencySkill>().HasIndex(s => new { s.DomainId, s.Name }).IsUnique();
        builder.Entity<CompetencySkill>().HasIndex(s => s.DomainId).IsUnique().HasFilter("is_default = true");
        builder.Entity<CompetencySkill>().HasOne(s => s.Domain).WithMany(d => d.Skills)
            .HasForeignKey(s => s.DomainId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CompetencyCriterion>().HasIndex(c => new { c.SkillId, c.Name, c.Level }).IsUnique();
        builder.Entity<CompetencyCriterion>().HasOne(c => c.Skill).WithMany(s => s.Criteria)
            .HasForeignKey(c => c.SkillId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<CompetencyCriterion>().ToTable("criteria", t => t.HasCheckConstraint("ck_criteria_level", "level BETWEEN 1 AND 3"));
        builder.Entity<CompetencyDomain>().ToTable("domains", t => t.HasCheckConstraint("ck_domains_weight", "weight > 0"));
        builder.Entity<CompetencySkill>().ToTable("skills", t => t.HasCheckConstraint("ck_skills_weight", "weight > 0"));
        builder.Entity<Question>().HasOne(q => q.Skill).WithMany().HasForeignKey(q => q.SkillId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Question>().ToTable("questions", t =>
        {
            t.HasCheckConstraint("ck_questions_difficulty", "difficulty_level IS NULL OR difficulty_level BETWEEN 1 AND 5");
            t.HasCheckConstraint("ck_questions_group_code", "group_code IS NULL OR group_code IN ('FOUNDATION','DATA','CRITICAL_THINKING','APPLICATION','ETHICS','TOOLS','FUTURE_OF_WORK')");
            t.HasCheckConstraint("ck_questions_target_level", "target_level IS NULL OR target_level BETWEEN 1 AND 3");
            t.HasCheckConstraint("ck_questions_published_classification", "status <> 'Published' OR (group_code IS NOT NULL AND target_level IS NOT NULL)");
            t.HasCheckConstraint("ck_questions_review_record", "(reviewed_at IS NULL) = (reviewed_by IS NULL)");
            t.HasCheckConstraint("ck_questions_reviewed_status", "status <> 'Reviewed' OR (reviewed_at IS NOT NULL AND group_code IS NOT NULL AND target_level IS NOT NULL)");
        });
        CompetencyCatalog.Seed(builder);
        CvFrameworkCatalog.Configure(builder);

        builder.Entity<Question>()
            .Property(q => q.Status)
            .HasConversion<string>();

        builder.Entity<ExamQuestion>()
            .HasKey(eq => eq.Id);
        builder.Entity<ExamQuestion>().HasIndex(eq => new { eq.ExamId, eq.QuestionId }).IsUnique();

        builder.Entity<Exam>().HasIndex(e => e.IsStandard).IsUnique()
            .HasFilter("is_standard AND status = 'active'");
        builder.Entity<Exam>().ToTable("exams", t =>
            t.HasCheckConstraint("ck_exams_standard_blueprint", "NOT is_standard OR blueprint IS NOT NULL"));

        builder.Entity<TestGroupScore>().HasKey(s => new { s.SessionId, s.GroupCode });
        builder.Entity<TestGroupScore>().HasOne(s => s.Session).WithMany(s => s.GroupScores)
            .HasForeignKey(s => s.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<TestGroupScore>().HasOne<ProficiencyLevel>().WithMany()
            .HasForeignKey(s => s.Level).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TestGroupScore>().ToTable("test_group_scores", t =>
        {
            t.HasCheckConstraint("ck_test_group_scores_group", "group_code IN ('FOUNDATION','DATA','CRITICAL_THINKING','APPLICATION','ETHICS','TOOLS','FUTURE_OF_WORK')");
            t.HasCheckConstraint("ck_test_group_scores_values", "item_count > 0 AND max_score > 0 AND raw_score BETWEEN 0 AND max_score AND percent BETWEEN 0 AND 100");
            t.HasCheckConstraint("ck_test_group_scores_level", "level IS NULL OR (level BETWEEN 1 AND 3 AND level_mapping_version IS NOT NULL)");
        });

        builder.Entity<TestLevelMapping>().HasIndex(m => m.Version).IsUnique();
        builder.Entity<TestLevelMapping>().HasIndex(m => m.IsActive).IsUnique().HasFilter("is_active = true");
        builder.Entity<TestLevelMapping>().ToTable("test_level_mappings", t =>
        {
            t.HasCheckConstraint("ck_test_level_mappings_publication",
                "(status = 'Draft' AND published_at IS NULL AND NOT is_active) OR (status = 'Published' AND published_at IS NOT NULL AND reviewed_by IS NOT NULL)");
            t.HasCheckConstraint("ck_test_level_mappings_thresholds",
                "intermediate_min_percent > 0 AND intermediate_min_percent < advanced_min_percent AND advanced_min_percent <= 100 AND min_items_per_group >= 1");
        });
        builder.Entity<TestLevelMapping>().HasData(TestLevelMappingCatalog.Draft, TestLevelMappingCatalog.V1);

        builder.Entity<ExamQuestion>()
            .HasOne(eq => eq.Exam)
            .WithMany(e => e.ExamQuestions)
            .HasForeignKey(eq => eq.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ExamQuestion>()
            .HasOne(eq => eq.Question)
            .WithMany()
            .HasForeignKey(eq => eq.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ExamResult>().Property(s => s.Status).IsConcurrencyToken();

        builder.Entity<ExamResult>()
            .HasOne(er => er.Exam)
            .WithMany()
            .HasForeignKey(er => er.ExamId);

        builder.Entity<Response>()
            .HasOne(r => r.ExamResult)
            .WithMany(er => er.Responses)
            .HasForeignKey(r => r.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Response>()
            .HasOne(r => r.Question)
            .WithMany()
            .HasForeignKey(r => r.QuestionId);

        builder.Entity<AdaptiveResponse>()
            .HasOne(ar => ar.Session)
            .WithMany(s => s.Responses)
            .HasForeignKey(ar => ar.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AdaptiveResponse>()
            .HasOne(ar => ar.Question)
            .WithMany()
            .HasForeignKey(ar => ar.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── ML / IRT / BKT ──────────────────────────────────────────────────

        builder.Entity<QuestionIrtParams>()
            .HasOne(p => p.Question)
            .WithMany()
            .HasForeignKey(p => p.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SessionIrtEstimate>()
            .HasOne(s => s.Session)
            .WithMany()
            .HasForeignKey(s => s.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserSkillMastery>()
            .HasKey(m => new { m.UserId, m.SkillId });

        builder.Entity<CompetencyProfile>()
            .Property(p => p.Scores)
            .HasColumnType("jsonb");
    }
}
