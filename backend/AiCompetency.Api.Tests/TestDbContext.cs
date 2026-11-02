using System.Text.Json;
using AiCompetency.Api.Data;
using AiCompetency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AiCompetency.Api.Tests;

// The in-memory provider lacks PostgreSQL's native JsonDocument mapping.
internal sealed class TestDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Question>().Property(q => q.Metadata)
            .HasConversion(v => v.RootElement.GetRawText(), v => JsonDocument.Parse(v, default(JsonDocumentOptions)));
        builder.Entity<QuestionGenerationPlan>().Property(q => q.PlanJson)
            .HasConversion(v => v.RootElement.GetRawText(), v => JsonDocument.Parse(v, default(JsonDocumentOptions)));
        builder.Entity<CompetencyProfile>().Property(q => q.Scores)
            .HasConversion(v => v.RootElement.GetRawText(), v => JsonDocument.Parse(v, default(JsonDocumentOptions)));
    }
}
