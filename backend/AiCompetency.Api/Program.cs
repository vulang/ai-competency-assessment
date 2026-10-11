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

// ── ML Microservice (Python FastAPI on port 8001) ─────────────────────────
var mlServiceUrl = builder.Configuration.GetValue<string>("MlService:BaseUrl") ?? "http://localhost:8001";
builder.Services.AddHttpClient<AiCompetency.Api.Services.MlService>(client =>
{
    client.BaseAddress = new Uri(mlServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(10); // Fast timeout; ML calls are non-critical
});
builder.Services.AddScoped<AiCompetency.Api.Services.CompetencyProfileService>();
builder.Services.AddScoped<AiCompetency.Api.Services.QuestionClassificationService>();
builder.Services.AddSingleton<AiCompetency.Api.Services.ProfileJobQueue>();
builder.Services.AddHostedService<AiCompetency.Api.Services.ProfileJobWorker>();


// Register the DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
    options.UseOpenIddict();
});

// Register Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Match the role claim emitted into OpenIddict tokens.
builder.Services.Configure<IdentityOptions>(options =>
    options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role);

// Configure OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetIssuer(new Uri(builder.Configuration["Authentication:Issuer"] ?? "http://localhost:5000/"));
        options.SetTokenEndpointUris("connect/token")
               .SetAuthorizationEndpointUris("connect/authorize")
               .SetEndSessionEndpointUris("connect/logout");
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
            .EnableEndSessionEndpointPassthrough()
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
    await context.Database.MigrateAsync();

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

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "Annotator", "Candidate" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var created = await roleManager.CreateAsync(new IdentityRole(role));
            if (!created.Succeeded) throw new InvalidOperationException($"Failed to create role {role}.");
        }
    }
    var admin = await userManager.FindByNameAsync("admin@example.com");
    if (admin != null && !await userManager.IsInRoleAsync(admin, "Admin"))
    {
        var assigned = await userManager.AddToRoleAsync(admin, "Admin");
        if (!assigned.Succeeded) throw new InvalidOperationException("Failed to assign the admin role.");
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
            OpenIddictConstants.Permissions.Endpoints.EndSession,
            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
            OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
            OpenIddictConstants.Permissions.GrantTypes.Password,
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

app.Run();

public partial class Program { }
