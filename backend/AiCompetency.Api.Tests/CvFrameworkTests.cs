using System.Security.Claims;
using System.Text.Json;
using AiCompetency.Api.Controllers;
using AiCompetency.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AiCompetency.Api.Tests;

public sealed class CvFrameworkTests
{
    private static async Task<TestDbContext> CreateDb()
    {
        var db = new TestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static FrameworkController Controller(ApplicationDbContext db, string role) => new(db)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"))
        } }
    };

    [Fact]
    public async Task DraftContainsSevenGroupsFourLevelsAndThreeIndicatorsPerPositiveLevel()
    {
        await using var db = await CreateDb();
        var result = Assert.IsType<OkObjectResult>(await Controller(db, "Admin").GetIndicators("cv-v1-draft"));
        var payload = JsonSerializer.SerializeToElement(result.Value);
        Assert.Equal("Draft", payload.GetProperty("Status").GetString());
        Assert.False(payload.GetProperty("IsActive").GetBoolean());
        Assert.Equal(4, payload.GetProperty("Levels").GetArrayLength());
        Assert.Equal("NO_EVIDENCE", payload.GetProperty("Levels")[0].GetProperty("Code").GetString());
        var groups = payload.GetProperty("Groups").EnumerateArray().ToList();
        Assert.Equal(CompetencyCatalog.Groups.Select(g => g.Code), groups.Select(g => g.GetProperty("Code").GetString()));
        foreach (var group in groups)
        {
            var indicators = group.GetProperty("Indicators").EnumerateArray().ToList();
            foreach (var level in new[] { 1, 2, 3 })
                Assert.Equal(3, indicators.Count(i => i.GetProperty("Level").GetInt32() == level));
            Assert.All(indicators, i => {
                Assert.False(string.IsNullOrWhiteSpace(i.GetProperty("TextVi").GetString()));
                Assert.False(string.IsNullOrWhiteSpace(i.GetProperty("TextEn").GetString()));
            });
            Assert.Equal(4, group.GetProperty("RubricRules").GetArrayLength());
        }
        Assert.Equal(63, await db.BehaviorIndicators.CountAsync());
        Assert.Equal(28, await db.CvRubricRules.CountAsync());
    }

    [Fact]
    public async Task CandidatesCannotReadDraftAndCompatibilityCatalogueStaysActive()
    {
        await using var db = await CreateDb();
        var controller = Controller(db, "Candidate");
        Assert.IsType<NotFoundObjectResult>(await controller.GetIndicators("cv-v1-draft"));
        Assert.IsType<NotFoundObjectResult>(await controller.GetIndicators("missing"));
        Assert.IsType<NotFoundObjectResult>(await controller.GetActiveCv());
        var compatibility = Assert.IsType<OkObjectResult>(await controller.GetActive());
        Assert.Equal("p0-v1", JsonSerializer.SerializeToElement(compatibility.Value).GetProperty("Version").GetString());
    }

    [Fact]
    public async Task PublishedVersionIsReadableByCandidatesWithRubricAndIndicators()
    {
        await using var db = await CreateDb();
        var version = await db.CvFrameworkVersions.SingleAsync();
        version.Status = "Published";
        version.PublishedAt = DateTimeOffset.UtcNow;
        version.IsActive = true;
        await db.SaveChangesAsync();
        var result = Assert.IsType<OkObjectResult>(await Controller(db, "Candidate").GetActiveCv());
        var payload = JsonSerializer.SerializeToElement(result.Value);
        Assert.Equal("Published", payload.GetProperty("Status").GetString());
        Assert.Equal(7, payload.GetProperty("Groups").GetArrayLength());
    }
}
