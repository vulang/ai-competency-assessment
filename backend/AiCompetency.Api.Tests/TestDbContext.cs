using System.Text.Json;
using AiCompetency.Api.Data;
using AiCompetency.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AiCompetency.Api.Tests;

// The in-memory provider lacks PostgreSQL's native JsonDocument mapping.
internal sealed class TestDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    // Publishing a level mapping uses a transaction, which the in-memory provider does not support.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // SQLite stores decimals as text, which breaks numeric check constraints.
        if (Database.IsSqlite())
            foreach (var property in builder.Model.GetEntityTypes().SelectMany(e => e.GetProperties())
                         .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
                property.SetProviderClrType(typeof(double));
        builder.Entity<Question>().Property(q => q.Metadata)
            .HasConversion(v => v.RootElement.GetRawText(), v => JsonDocument.Parse(v, default(JsonDocumentOptions)));
        builder.Entity<QuestionGenerationPlan>().Property(q => q.PlanJson)
            .HasConversion(v => v.RootElement.GetRawText(), v => JsonDocument.Parse(v, default(JsonDocumentOptions)));
        builder.Entity<CompetencyProfile>().Property(q => q.Scores)
            .HasConversion(v => v.RootElement.GetRawText(), v => JsonDocument.Parse(v, default(JsonDocumentOptions)));
    }
}
