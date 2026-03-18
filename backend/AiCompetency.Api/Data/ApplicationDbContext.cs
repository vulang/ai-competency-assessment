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

    public DbSet<Question> Questions { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<ExamQuestion> ExamQuestions { get; set; }
    public DbSet<ExamResult> ExamResults { get; set; }
    public DbSet<Response> Responses { get; set; }
    public DbSet<QuestionGenerationPlan> QuestionGenerationPlans { get; set; }
    public DbSet<AdaptiveTestSession> AdaptiveTestSessions { get; set; }
    public DbSet<AdaptiveResponse> AdaptiveResponses { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename Identity tables if needed, or leave them as default.
        // For now, we only configure our new entities.

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
    }
}
