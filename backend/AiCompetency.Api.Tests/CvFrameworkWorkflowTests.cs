using AiCompetency.Api.Controllers;
using AiCompetency.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AiCompetency.Api.Tests;

public sealed class CvFrameworkWorkflowTests
{
    private static async Task<TestDbContext> CreateDb()
    {
        var db = new TestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    [Fact]
    public async Task ReviewedPublicationKeepsOlderVersionAndRejectsPublishedEdits()
    {
        await using var db = await CreateDb();
        var admin = new CvFrameworkAdminController(db);
        Assert.IsType<BadRequestObjectResult>(await admin.Publish("cv-v1-draft", new("", "")));
        Assert.IsType<BadRequestObjectResult>(await admin.Publish("cv-v1-draft", new("Reviewer", "Approved")));
        Assert.IsType<CreatedAtActionResult>(await admin.Clone("cv-v1-draft", new("cv-v2")));
        var copy = await db.CvFrameworkVersions.Include(v => v.Groups).ThenInclude(g => g.Indicators)
            .Include(v => v.Groups).ThenInclude(g => g.RubricRules).SingleAsync(v => v.Version == "cv-v2");
        var input = new FrameworkContentRequest(copy.Groups.Select(g => new GroupInput(g.Code, g.NameVi, g.NameEn, g.Order,
            g.Indicators.Select(i => new IndicatorInput(i.Code, i.Level, i.TextVi, i.TextEn)).ToList(),
            g.RubricRules.Select(r => new RubricInput(r.Level, r.AcceptedEvidence, r.InsufficientEvidence, r.Example)).ToList())).ToList());
        input.Groups[0].Indicators[0] = input.Groups[0].Indicators[0] with { TextEn = "Reviewed evidence action" };
        Assert.IsType<NoContentResult>(await admin.UpdateContent("cv-v2", input));
        Assert.IsType<BadRequestObjectResult>(await admin.Publish("cv-v2", new("", "Approved")));
        Assert.IsType<OkObjectResult>(await admin.Publish("cv-v2", new("Expert A", "Pilot reviewed")));
        Assert.IsType<ConflictObjectResult>(await admin.UpdateContent("cv-v2", input));
        Assert.False((await db.CvFrameworkVersions.SingleAsync(v => v.Version == "cv-v1-draft")).IsActive);
        Assert.False((await db.CvFrameworkVersions.SingleAsync(v => v.Version == "cv-v1")).IsActive);
        Assert.True((await db.CvFrameworkVersions.SingleAsync(v => v.Version == "cv-v2")).IsActive);
        Assert.NotEqual("Reviewed evidence action", (await db.BehaviorIndicators.SingleAsync(i => i.FrameworkVersionId == 1 && i.Code == input.Groups[0].Indicators[0].Code)).TextEn);
        Assert.IsType<CreatedAtActionResult>(await admin.Clone("cv-v2", new("cv-v3")));
        Assert.IsType<OkObjectResult>(await admin.Publish("cv-v3", new("Expert B", "Revised and reviewed")));
        Assert.False((await db.CvFrameworkVersions.SingleAsync(v => v.Version == "cv-v2")).IsActive);
        Assert.True((await db.CvFrameworkVersions.SingleAsync(v => v.Version == "cv-v3")).IsActive);
    }

    [Fact]
    public async Task InvalidCoverageCannotReplaceDraft()
    {
        await using var db = await CreateDb();
        var admin = new CvFrameworkAdminController(db);
        var invalid = new FrameworkContentRequest([]);
        Assert.IsType<BadRequestObjectResult>(await admin.UpdateContent("cv-v1-draft", invalid));
        Assert.Equal(126, await db.BehaviorIndicators.CountAsync());
        var seed = await db.CvFrameworkVersions.Include(v => v.Groups).ThenInclude(g => g.Indicators)
            .Include(v => v.Groups).ThenInclude(g => g.RubricRules).SingleAsync(v => v.Version == "cv-v1-draft");
        var groups = seed.Groups.Select(g => new GroupInput(g.Code, g.NameVi, g.NameEn, g.Order,
            g.Indicators.Select(i => new IndicatorInput(i.Code, i.Level, i.TextVi, i.TextEn)).ToList(),
            g.RubricRules.Select(r => new RubricInput(r.Level, r.AcceptedEvidence, r.InsufficientEvidence, r.Example)).ToList())).ToList();
        var item = groups[0].Indicators[0];
        groups[0].Indicators[0] = item with { Code = $"{groups[0].Code}.3.99", Level = 1 };
        Assert.IsType<BadRequestObjectResult>(await admin.UpdateContent("cv-v1-draft", new(groups)));
        Assert.IsType<ConflictObjectResult>(await admin.Clone("cv-v1-draft", new("cv-v1-draft")));
    }
}
