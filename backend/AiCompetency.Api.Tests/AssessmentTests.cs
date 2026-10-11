using System.Net;
using System.Security.Claims;
using System.Text.Json;
using AiCompetency.Api.Controllers;
using AiCompetency.Api.Data;
using AiCompetency.Api.Models;
using AiCompetency.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AiCompetency.Api.Tests;

public class AssessmentTests
{
    private static ApplicationDbContext CreateDb() => new TestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static T AsUser<T>(T controller, string userId) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", userId)], "test"))
            }
        };
        return controller;
    }

    [Fact]
    public void SubjectUsesGuidAndMissingIdentityFails()
    {
        var id = Guid.NewGuid().ToString();
        Assert.Equal(id, CurrentUser.GetRequiredId(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", id)]))));
        Assert.Throws<UnauthorizedAccessException>(() => CurrentUser.GetRequiredId(new ClaimsPrincipal()));
    }

    [Theory]
    [InlineData("mcq_single", "{\"answer\":\"A\"}", "B", false)]
    [InlineData("mcq_single", "{\"answer\":[\"A\"]}", " a ", true)]
    [InlineData("mcq_multi", "{\"answer\":[\"A\",\"B\"]}", "[\"B\",\"A\"]", true)]
    [InlineData("mcq_multi", "{\"answer\":[\"A\",\"B\"]}", "[\"A\",\"A\"]", false)]
    [InlineData("mcq_multi", "{\"answer\":[\"A\",\"B\"]}", "not json", false)]
    [InlineData("mcq_single", "{\"answer\":12}", "12", false)]
    [InlineData("scenario", "{\"answer\":\"A\"}", "A", false)]
    [InlineData("true_false", "{\"answer\":true}", "true", true)]
    [InlineData("true_false", "{\"answer\":false}", "true", false)]
    [InlineData("mcq_single", "{\"answer\":\"A\",\"options\":[\"A. Alpha\",\"B. Beta\"]}", "A. Alpha", true)]
    [InlineData("mcq_single", "{\"answer\":\"B\",\"options\":[\"Alpha\",\"Beta\"]}", "Beta", true)]
    [InlineData("mcq_single", "{\"answer\":\"A\",\"options\":[\"A. Alpha\",\"B. Beta\"]}", "A. unrelated", false)]
    [InlineData("mcq_multi", "{\"answer\":[\"A\",\"B\"],\"options\":[\"A. Alpha\",\"B. Beta\"]}", "[\"B. Beta\",\"A. Alpha\"]", true)]
    [InlineData("mcq_single", "{}", "A", false)]
    public void ScoringRejectsWrongAndMalformedAnswers(string type, string metadata, string answer, bool expected)
        => Assert.Equal(expected, AnswerEvaluator.IsCorrect(type, JsonDocument.Parse(metadata), answer));

    [Fact]
    public async Task CandidatesHaveSeparateHistoryAndCannotReadOrSubmitEachOthersSession()
    {
        await using var db = CreateDb();
        var exam = new Exam { Title = "Test", Status = "active" };
        db.Exams.Add(exam);
        await db.SaveChangesAsync();
        var alice = AsUser(new CandidateTestsController(db, new ProfileJobQueue()), Guid.NewGuid().ToString());
        var bob = AsUser(new CandidateTestsController(db, new ProfileJobQueue()), Guid.NewGuid().ToString());
        Assert.IsType<OkObjectResult>(await alice.StartTest(exam.Id));
        Assert.IsType<OkObjectResult>(await bob.StartTest(exam.Id));
        var sessions = await db.ExamResults.ToListAsync();
        Assert.NotEqual(sessions[0].UserId, sessions[1].UserId);
        var aliceSession = sessions.Single(s => s.UserId == CurrentUser.GetRequiredId(alice.User));
        Assert.IsType<NotFoundObjectResult>(await bob.GetTestSession(aliceSession.Id));
        Assert.IsType<NotFoundObjectResult>(await bob.SubmitTest(aliceSession.Id, new()));
        Assert.IsType<OkObjectResult>(await alice.SubmitTest(aliceSession.Id, new()));
        var aliceHistory = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await alice.GetTestHistory()).Value);
        var bobHistory = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await bob.GetTestHistory()).Value);
        Assert.Contains(aliceSession.Id.ToString(), aliceHistory);
        Assert.Equal("[]", bobHistory);
    }

    [Fact]
    public async Task DuplicateStandardAnswersCannotInflateScore()
    {
        await using var db = CreateDb();
        var q = new Question { Content = "Q", Type = "mcq_single", Metadata = JsonDocument.Parse("{\"answer\":\"A\"}") };
        var exam = new Exam { Title = "Test", Status = "active", ExamQuestions = [new ExamQuestion { Question = q, PointValue = 1 }] };
        db.Exams.Add(exam);
        await db.SaveChangesAsync();
        var controller = AsUser(new CandidateTestsController(db, new ProfileJobQueue()), "alice");
        await controller.StartTest(exam.Id);
        var id = (await db.ExamResults.SingleAsync()).Id;
        Assert.IsType<BadRequestObjectResult>(await controller.SubmitTest(id, new()
        {
            Responses = [new() { QuestionId = q.Id, Answer = "A" }, new() { QuestionId = q.Id, Answer = "A" }]
        }));
        Assert.Empty(await db.Responses.ToListAsync());
    }

    [Fact]
    public async Task IrtEstimateIsVisibleOnlyToSessionOwner()
    {
        await using var db = CreateDb();
        var session = new ExamResult { UserId = "alice", Exam = new Exam { Title = "Test" } };
        db.SessionIrtEstimates.Add(new SessionIrtEstimate { Session = session });
        await db.SaveChangesAsync();
        var ml = new MlService(new HttpClient(new CaptureHandler()), NullLogger<MlService>.Instance);
        var profile = new CompetencyProfileService(db, ml, NullLogger<CompetencyProfileService>.Instance);
        var controller = AsUser(new CompetencyProfileController(db, profile), "bob");
        Assert.IsType<NotFoundObjectResult>(await controller.GetSessionIrt(session.Id));
        AsUser(controller, "alice");
        Assert.IsType<OkObjectResult>(await controller.GetSessionIrt(session.Id));
    }

    [Fact]
    public async Task CatScoresActualAnswerAndSendsCompleteHistoryToEap()
    {
        await using var db = CreateDb();
        db.Questions.AddRange(new Question { Content = "Q1", Type = "mcq_single", Status = QuestionStatus.Published, DifficultyLevel = 3, Metadata = JsonDocument.Parse("{\"answer\":\"A\"}") },
            new Question { Content = "Q2", Type = "mcq_single", Status = QuestionStatus.Published, DifficultyLevel = 3, Metadata = JsonDocument.Parse("{\"answer\":\"B\"}") });
        await db.SaveChangesAsync();
        var handler = new CaptureHandler();
        var ml = new MlService(new HttpClient(handler) { BaseAddress = new Uri("http://ml.test") }, NullLogger<MlService>.Instance);
        var controller = AsUser(new AdaptiveTestsController(db, ml), "alice");
        await controller.StartAdaptiveTest();
        var id = (await db.AdaptiveTestSessions.SingleAsync()).Id;
        var questions = await db.Questions.OrderBy(q => q.Id).ToListAsync();
        await controller.SubmitAnswer(id, new() { QuestionId = questions[0].Id, Answer = "wrong but nonempty" });
        db.ChangeTracker.Clear(); // A fresh request must load the previous responses from storage.
        await controller.SubmitAnswer(id, new() { QuestionId = questions[1].Id, Answer = "B" });
        using var payload = JsonDocument.Parse(handler.LastBody!);
        var responses = payload.RootElement.GetProperty("responses");
        Assert.Equal(2, responses.GetArrayLength());
        Assert.False(responses[0].GetProperty("is_correct").GetBoolean());
        Assert.True(responses[1].GetProperty("is_correct").GetBoolean());
        Assert.IsType<BadRequestObjectResult>(await controller.SubmitAnswer(id, new() { QuestionId = questions[1].Id, Answer = "B" }));
        Assert.Equal(2, await db.AdaptiveResponses.CountAsync());
    }

    [Fact]
    public async Task ProfileWorkerProcessesPersistedSessionAfterRequestScopeIsDisposed()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ApplicationDbContext>(_ => new TestDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName).Options));
        services.AddScoped(_ => new MlService(new HttpClient(new CaptureHandler())
            { BaseAddress = new Uri("http://ml.test") }, NullLogger<MlService>.Instance));
        services.AddScoped<CompetencyProfileService>();
        await using var provider = services.BuildServiceProvider();
        Guid sessionId;
        await using (var requestScope = provider.CreateAsyncScope())
        {
            var db = requestScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var q = new Question { Content = "Q", Type = "mcq_single" };
            var session = new ExamResult { UserId = "alice", Status = "Completed", Exam = new Exam { Title = "Test" },
                Responses = [new Response { Question = q, ScoreEarned = 1 }] };
            db.ExamResults.Add(session);
            await db.SaveChangesAsync();
            sessionId = session.Id;
        }
        var queue = new ProfileJobQueue();
        using var worker = new ProfileJobWorker(queue, provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ProfileJobWorker>.Instance);
        queue.Enqueue(sessionId);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (true)
            {
                await using var check = provider.CreateAsyncScope();
                var db = check.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (await db.SessionIrtEstimates.AnyAsync(e => e.SessionId == sessionId)) break;
                await Task.Delay(20, timeout.Token);
            }
        }
        finally { await worker.StopAsync(CancellationToken.None); }
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? LastBody { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"estimate\":{\"theta\":0.2,\"se\":0.5,\"method\":\"EAP\",\"fisher_info\":1}}")
            };
        }
    }
}
