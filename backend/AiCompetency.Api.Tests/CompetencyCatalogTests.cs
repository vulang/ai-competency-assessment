using System.Net;
using System.Text.Json;
using AiCompetency.Api.Controllers;
using AiCompetency.Api.Data;
using AiCompetency.Api.Models;
using AiCompetency.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AiCompetency.Api.Tests;

public class CompetencyCatalogTests
{
    private static async Task<TestDbContext> CreateDbAsync()
    {
        var context = new TestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static QuestionsController Controller(ApplicationDbContext context) => new(context, new QuestionClassificationService(context))
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };

    [Fact]
    public async Task CatalogueSeedsSevenGroupsAndThreeExistingLevelDescriptionsPerSkill()
    {
        await using var db = await CreateDbAsync();
        Assert.Equal(7, await db.Domains.CountAsync());
        Assert.Equal(7, await db.Skills.CountAsync(s => s.IsDefault));
        Assert.Equal(21, await db.Criteria.CountAsync());
        var result = Assert.IsType<OkObjectResult>(await new FrameworkController(db).GetActive());
        var framework = JsonSerializer.SerializeToElement(result.Value);
        Assert.Equal("p0-v1", framework.GetProperty("Version").GetString());
        Assert.Equal(7, framework.GetProperty("Domains").GetArrayLength());
    }

    [Theory]
    [InlineData("AITools", "KienTao", 6, "AI Tools", "Advanced")]
    [InlineData("TOOLS", "Basic", 6, "AI Tools", "Basic")]
    [InlineData("Critical Thinking", "Apply", 3, "Critical Thinking", "Intermediate")]
    public async Task ManualQuestionLinksToRealSkillAndKeepsDifficultyIndependent(string group, string level, int skillId, string name, string canonicalLevel)
    {
        await using var db = await CreateDbAsync();
        var result = Assert.IsType<CreatedAtActionResult>(await Controller(db).CreateQuestion(new QuestionDto
        {
            Prompt = "Q", Type = "single_choice", CompetencyGroupId = group, CompetencyLevelId = level,
            Difficulty = 5, Answer = "[\"A\"]"
        }));
        var question = await db.Questions.SingleAsync();
        Assert.Equal(skillId, question.SkillId);
        Assert.Equal(5, question.DifficultyLevel);
        Assert.Equal("mcq_single", question.Type);
        Assert.Equal(name, question.Metadata.RootElement.GetProperty("group").GetString());
        Assert.Equal(canonicalLevel, question.Metadata.RootElement.GetProperty("level").GetString());
        Assert.True(AnswerEvaluator.IsCorrect(question.Type, question.Metadata, "A"));
        Assert.Equal(skillId, Assert.IsType<QuestionDto>(result.Value).SkillId);
    }

    [Fact]
    public async Task ChangingGroupMovesSkillAndBatchPreservesGeneratorDifficulty()
    {
        await using var db = await CreateDbAsync();
        var controller = Controller(db);
        await controller.CreateQuestion(new QuestionDto { Prompt = "Q", Type = "mcq_single", CompetencyGroupId = "Data", CompetencyLevelId = "Basic", Difficulty = 1 });
        var question = await db.Questions.SingleAsync();
        var update = Assert.IsType<OkObjectResult>(await controller.UpdateQuestion(question.Id,
            new QuestionDto { Prompt = "Q", Type = "mcq_single", CompetencyGroupId = "AI Tools", CompetencyLevelId = "Advanced", Difficulty = 2 }));
        Assert.Equal(6, question.SkillId);
        Assert.Equal("AI Tools", Assert.IsType<QuestionDto>(update.Value).CompetencyGroupId);
        await controller.SaveBatch([new() { Stem = "Generated Q", Group = "Data", Level = "Advanced", Difficulty = 1 },
            new() { Stem = "Unknown difficulty", Group = "Data", Level = "Basic" }]);
        Assert.Equal(1, (await db.Questions.SingleAsync(q => q.Content == "Generated Q")).DifficultyLevel);
        Assert.Null((await db.Questions.SingleAsync(q => q.Content == "Unknown difficulty")).DifficultyLevel);
    }

    [Theory]
    [InlineData("unknown group", "Basic", null, 1)]
    [InlineData("Data", "unknown level", null, 1)]
    [InlineData("Data", "Basic", 6, 1)]
    [InlineData("Data", "Basic", null, 10)]
    public async Task InvalidMappingRejectsEntireBatchWithoutPartialSave(string group, string level, int? skillId, int difficulty)
    {
        await using var db = await CreateDbAsync();
        var result = await Controller(db).SaveBatch([
            new() { Stem = "Valid", Group = "Data", Level = "Basic", Difficulty = 1 },
            new() { Stem = "Invalid", Group = group, Level = level, SkillId = skillId, Difficulty = difficulty }
        ]);
        var response = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, Assert.IsType<ValidationProblemDetails>(response.Value).Status);
        Assert.Empty(await db.Questions.ToListAsync());
    }

    [Fact]
    public async Task ProfilePipelinePersistsRealDomainScoresAndKeepsCandidateMasteriesSeparate()
    {
        await using var db = await CreateDbAsync();
        var question = new Question { Content = "Q", Type = "mcq_single", SkillId = 2, DifficultyLevel = 3 };
        var session = new ExamResult { UserId = "alice", Status = "Completed", Exam = new Exam { Title = "Test" } };
        db.Questions.Add(question);
        db.ExamResults.Add(session);
        db.UserSkillMasteries.Add(new UserSkillMastery { UserId = "bob", SkillId = 2, MasteryProb = 0.2m, ResponseCount = 4 });
        await db.SaveChangesAsync();
        var handler = new ProfileHandler();
        var ml = new MlService(new HttpClient(handler) { BaseAddress = new Uri("http://ml.test") }, NullLogger<MlService>.Instance);
        var profiles = new CompetencyProfileService(db, ml, NullLogger<CompetencyProfileService>.Instance);
        var result = await profiles.ProcessSessionAsync(session.Id, "alice", [(question.Id, true, 3, 2)]);
        Assert.NotNull(result);
        Assert.Equal(75.0, result.Scores.RootElement.GetProperty("data").GetDouble());
        Assert.Equal("Data", handler.ProfilePayload!.RootElement.GetProperty("skill_masteries")[0].GetProperty("domain_name").GetString());
        Assert.Equal(1, (await db.UserSkillMasteries.SingleAsync(m => m.UserId == "alice")).ResponseCount);
        Assert.Equal(4, (await db.UserSkillMasteries.SingleAsync(m => m.UserId == "bob")).ResponseCount);
        Assert.NotNull(await profiles.GetLatestProfileAsync("alice"));
        Assert.Null(await profiles.GetLatestProfileAsync("bob"));
    }

    private sealed class ProfileHandler : HttpMessageHandler
    {
        public JsonDocument? ProfilePayload { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            string response;
            if (request.RequestUri!.AbsolutePath == "/profile/generate")
            {
                ProfilePayload = JsonDocument.Parse(body);
                response = JsonSerializer.Serialize(new {
                    user_id = ProfilePayload.RootElement.GetProperty("user_id").GetString(),
                    session_id = ProfilePayload.RootElement.GetProperty("session_id").GetString(),
                    theta = 0, overall_level = "Apply", overall_score = 75,
                    domain_scores = new[] { new { domain_id = 2, domain_name = "Data", score = 75, mastery_count = 0, total_skills = 1 } }
                });
            }
            else if (request.RequestUri.AbsolutePath == "/kt/update")
                response = "{\"skill_id\":2,\"posterior_mastery\":0.75,\"mastered\":false}";
            else response = "{\"estimate\":{\"theta\":0,\"se\":0.5,\"method\":\"EAP\",\"fisher_info\":1}}";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response) };
        }
    }
}
