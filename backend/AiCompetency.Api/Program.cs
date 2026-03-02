using AiCompetency.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddHttpClient<AiCompetency.Api.Services.QuestionGeneratorService>();

// Register the DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
    options.UseOpenIddict();
});

// Register Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("connect/token")
               .SetAuthorizationEndpointUris("connect/authorize");
               //.SetLogoutEndpointUris("connect/logout");

        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange();
        
        options.RegisterScopes(
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Roles);

        options.AllowPasswordFlow();

        options.AcceptAnonymousClients();

        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
            .EnableTokenEndpointPassthrough()
            .EnableAuthorizationEndpointPassthrough()
            .DisableTransportSecurityRequirement();
            //.EnableLogoutEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

var app = builder.Build();

// Create a scope to ensure the DB is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.EnsureCreatedAsync();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    if (await userManager.FindByNameAsync("admin@example.com") == null)
    {
        var user = new IdentityUser { UserName = "admin@example.com", Email = "admin@example.com" };
        var result = await userManager.CreateAsync(user, "Password123!");
        if (!result.Succeeded)
        {
             throw new Exception("Failed to create default user");
        }
    }

    // Seed OpenIddict Client
    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    var existingClient = await manager.FindByClientIdAsync("ai-competency-ui");
    if (existingClient != null)
    {
        await manager.DeleteAsync(existingClient);
    }

    await manager.CreateAsync(new OpenIddictApplicationDescriptor
    {
        ClientId = "ai-competency-ui",
        ClientType = OpenIddictConstants.ClientTypes.Public,
        DisplayName = "AI Competency UI",
        RedirectUris = { new Uri("http://localhost:4200/callback") },
        PostLogoutRedirectUris = { new Uri("http://localhost:4200/") },
        Permissions =
        {
            OpenIddictConstants.Permissions.Endpoints.Authorization,
            OpenIddictConstants.Permissions.Endpoints.Token,
            //OpenIddictConstants.Permissions.Endpoints.Logout,
            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
            OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
            OpenIddictConstants.Permissions.ResponseTypes.Code,
            OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles
        }
    });
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

var frameworks = new List<FrameworkDto>
{
    new(1, "AI Competency 2024", "1.0", true)
};

var exams = new List<ExamDto>
{
    new(1, "Prompt Engineering Baseline", 45, 70, true)
};

app.MapGet("/api/frameworks", () => frameworks);

app.MapPost("/api/frameworks", (FrameworkDto framework) =>
{
    var nextId = frameworks.Count == 0 ? 1 : frameworks.Max(item => item.FrameworkId) + 1;
    var created = framework with { FrameworkId = nextId };
    frameworks.Add(created);
    return Results.Created($"/api/frameworks/{created.FrameworkId}", created);
});

app.MapGet("/api/exams", () => exams);

app.MapPost("/api/exams", (ExamDto exam) =>
{
    var nextId = exams.Count == 0 ? 1 : exams.Max(item => item.ExamId) + 1;
    var created = exam with { ExamId = nextId };
    exams.Add(created);
    return Results.Created($"/api/exams/{created.ExamId}", created);
});

app.MapPost("/api/sessions", (CreateSessionRequest request) =>
{
    var session = new TestSessionDto(Guid.NewGuid(), request.UserId, request.ExamId, DateTimeOffset.UtcNow, null, 0, "In-Progress");
    return Results.Ok(session);
});

app.MapPost("/api/responses", (CreateResponseRequest request) =>
{
    var response = new ResponseDto(Guid.NewGuid(), request.SessionId, request.QuestionId, request.FinalAnswer, 0, null, DateTimeOffset.UtcNow);
    return Results.Ok(response);
});

app.Run();

record FrameworkDto(int FrameworkId, string Name, string? Version, bool IsActive);
record ExamDto(int ExamId, string Title, int DurationMinutes, int PassScore, bool IsPublished);
record TestSessionDto(Guid SessionId, int UserId, int ExamId, DateTimeOffset StartTime, DateTimeOffset? EndTime, decimal TotalScore, string Status);
record ResponseDto(Guid ResponseId, Guid SessionId, int QuestionId, string FinalAnswer, decimal ScoreEarned, string? AiFeedback, DateTimeOffset SubmittedAt);
record CreateSessionRequest(int UserId, int ExamId);
record CreateResponseRequest(Guid SessionId, int QuestionId, string FinalAnswer);
