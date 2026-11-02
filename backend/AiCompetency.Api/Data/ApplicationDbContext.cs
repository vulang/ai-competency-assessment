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
    public DbSet<Question> Questions { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<ExamQuestion> ExamQuestions { get; set; }
    public DbSet<ExamResult> ExamResults { get; set; }
    public DbSet<Response> Responses { get; set; }
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
        builder.Entity<Question>().ToTable("questions", t => t.HasCheckConstraint("ck_questions_difficulty", "difficulty_level IS NULL OR difficulty_level BETWEEN 1 AND 5"));
        CompetencyCatalog.Seed(builder);

        builder.Entity<Question>()
            .Property(q => q.Status)
            .HasConversion<string>();

        builder.Entity<ExamQuestion>()
            .HasKey(eq => eq.Id);

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
