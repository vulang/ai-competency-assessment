using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AiCompetency.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace AiCompetency.Api.Tests;

[Trait("Category", "Integration")]
public class PostgresApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder().WithImage("postgres:16")
        .WithTmpfsMount("/var/lib/postgresql/data")
        .WithLabel("ai-cas.phase0-validation", "true")
        .WithDatabase("ai_cas_integration").WithUsername("postgres").WithPassword("integration-validation").Build();
    private ApiFactory? _factory;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        _factory = new ApiFactory(_database.GetConnectionString());
    }
    public async Task DisposeAsync()
    {
        if (_factory != null) await _factory.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task RealTokensEnforceAdminAuthorizationAndIsolateCandidateAttempts()
    {
        using var admin = _factory!.CreateClient();
        var discovery = await admin.GetFromJsonAsync<JsonElement>("/.well-known/openid-configuration");
        Assert.Equal(new Uri(admin.BaseAddress!, "connect/logout").ToString(), discovery.GetProperty("end_session_endpoint").GetString());
        Assert.Equal("http://localhost:5000/", discovery.GetProperty("issuer").GetString());
        await Login(admin, "admin@example.com");
        var created = await admin.PostAsJsonAsync("/api/questions", new {
            title = "Integration Q", prompt = "Choose A", type = "mcq_single", competencyGroupId = "Data",
            competencyLevelId = "Basic", difficulty = 2, options = new[] { "A", "B" }, answer = "A", status = "published"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var alice = _factory.CreateClient();
        using var bob = _factory.CreateClient();
        foreach (var (client, email) in new[] { (alice, "alice@example.com"), (bob, "bob@example.com") })
        {
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/account/register", new { email, password = "Password123!" })).StatusCode);
            await Login(client, email);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/questions")).StatusCode);
        }
        var aliceSession = await Start(alice);
        var bobSession = await Start(bob);
        Assert.NotEqual(aliceSession, bobSession);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/candidate/tests/sessions/{aliceSession}")).StatusCode);
        var session = await alice.GetFromJsonAsync<JsonElement>($"/api/candidate/tests/sessions/{aliceSession}");
        var questions = session.GetProperty("questions").EnumerateArray().ToList();
        Assert.All(questions, q => Assert.False(q.GetProperty("metadata").TryGetProperty("answer", out _)));
        Assert.Equal(HttpStatusCode.Conflict, (await alice.GetAsync($"/api/candidate/tests/sessions/{aliceSession}/result")).StatusCode);
        var answers = questions.Select(q => new { questionId = q.GetProperty("id").GetInt32(), answer = "A" }).ToList();
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PostAsJsonAsync($"/api/candidate/tests/sessions/{aliceSession}/submit", new { responses = answers })).StatusCode);
        var competing = await Task.WhenAll(
            alice.PostAsJsonAsync($"/api/candidate/tests/sessions/{aliceSession}/submit", new { responses = answers }),
            alice.PostAsJsonAsync($"/api/candidate/tests/sessions/{aliceSession}/submit", new { responses = answers }));
        Assert.Single(competing, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(competing, r => r.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict);
        Assert.Equal(1, (await alice.GetFromJsonAsync<JsonElement>("/api/candidate/tests/history")).GetArrayLength());
        Assert.Equal(0, (await bob.GetFromJsonAsync<JsonElement>("/api/candidate/tests/history")).GetArrayLength());
        var result = await alice.GetFromJsonAsync<JsonElement>($"/api/candidate/tests/sessions/{aliceSession}/result");
        Assert.Equal(1, result.GetProperty("totalScore").GetInt32());
        Assert.Equal(1, result.GetProperty("responses").GetArrayLength());
        Assert.Equal(1, result.GetProperty("passScore").GetInt32());
        var bobQuestions = (await bob.GetFromJsonAsync<JsonElement>($"/api/candidate/tests/sessions/{bobSession}"))
            .GetProperty("questions").EnumerateArray().Select(q => new { questionId = q.GetProperty("id").GetInt32(), answer = "B" }).ToList();
        (await bob.PostAsJsonAsync($"/api/candidate/tests/sessions/{bobSession}/submit", new { responses = bobQuestions })).EnsureSuccessStatusCode();
        var wrongResult = await bob.GetFromJsonAsync<JsonElement>($"/api/candidate/tests/sessions/{bobSession}/result");
        Assert.False(wrongResult.GetProperty("passed").GetBoolean());
    }

    private static async Task Login(HttpClient client, string email)
    {
        var response = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["grant_type"] = "password", ["client_id"] = "ai-competency-ui", ["username"] = email,
            ["password"] = "Password123!", ["scope"] = "openid profile email roles"
        }));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("access_token").GetString());
    }
    private static async Task<string> Start(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/candidate/tests/start-standard", new { });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionId").GetString()!;
    }

    private sealed class ApiFactory(string connection) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:Postgres"] = connection, ["Authentication:Issuer"] = "http://localhost:5000/"
            }));
            builder.ConfigureServices(services => services.AddHttpClient<MlService>()
                .ConfigurePrimaryHttpMessageHandler(() => new UnavailableMlHandler()));
        }
    }
    private sealed class UnavailableMlHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    }
}
