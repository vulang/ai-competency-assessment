using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using System.Net.Http.Json;
using AiCompetency.Api.Controllers;
using AiCompetency.Api.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AiCompetency.Api.Tests;

public class RegistrationTests
{
    [Fact]
    public async Task RegistrationCreatesCandidateOnlyAndPasswordCanBeUsedToSignIn()
    {
        await using var fixture = await RegistrationServer.CreateAsync();
        using var response = await fixture.Client.PostAsJsonAsync("/api/account/register", new
        {
            email = "candidate@example.com", password = "Strong123!", role = "Admin"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = fixture.Server.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await users.FindByEmailAsync("candidate@example.com");
        Assert.NotNull(user);
        Assert.True(await users.CheckPasswordAsync(user, "Strong123!"));
        Assert.Equal(["Candidate"], await users.GetRolesAsync(user));
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<IdentityUser>>();
        var principal = await signIn.CreateUserPrincipalAsync(user);
        Assert.True(principal.IsInRole("Candidate"));
        Assert.False(principal.IsInRole("Admin"));
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Strong123!", body);
    }

    [Theory]
    [InlineData("invalid-email", "Strong123!")]
    [InlineData("candidate@example.com", "weak")]
    [InlineData("candidate@example.com", "alllowercase")]
    [InlineData("", "Strong123!")]
    public async Task InvalidRegistrationsReturnValidationErrorsAndCreateNoUser(string email, string password)
    {
        await using var fixture = await RegistrationServer.CreateAsync();
        using var response = await fixture.Client.PostAsJsonAsync("/api/account/register", new { email, password });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("errors", await response.Content.ReadAsStringAsync());
        using var scope = fixture.Server.Services.CreateScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.CountAsync());
    }

    [Fact]
    public async Task DuplicateEmailWithDifferentCaseCannotCreateAnotherAccount()
    {
        await using var fixture = await RegistrationServer.CreateAsync();
        Assert.Equal(HttpStatusCode.Created, (await fixture.Client.PostAsJsonAsync("/api/account/register",
            new { email = "candidate@example.com", password = "Strong123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Client.PostAsJsonAsync("/api/account/register",
            new { email = "CANDIDATE@example.com", password = "Strong123!" })).StatusCode);
        using var scope = fixture.Server.Services.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.CountAsync());
    }

    [Fact]
    public async Task MissingCandidateRoleCreatesNoPartialAccount()
    {
        await using var fixture = await RegistrationServer.CreateAsync(seedRole: false);
        var response = await fixture.Client.PostAsJsonAsync("/api/account/register", new
            { email = "candidate@example.com", password = "Strong123!" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var scope = fixture.Server.Services.CreateScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.CountAsync());
    }

    [Fact]
    public async Task FailedRoleAssignmentRollsBackUserCreation()
    {
        await using var fixture = await RegistrationServer.CreateAsync(failRoleAssignment: true);
        var response = await fixture.Client.PostAsJsonAsync("/api/account/register", new
            { email = "candidate@example.com", password = "Strong123!" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var scope = fixture.Server.Services.CreateScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.CountAsync());
    }

    [Theory]
    [InlineData("authorization_code")]
    [InlineData("password")]
    public async Task TokenExchangePreservesRolesAndScopesForRegisteredCandidate(string grantType)
    {
        await using var fixture = await RegistrationServer.CreateAsync();
        await fixture.Client.PostAsJsonAsync("/api/account/register", new { email = "candidate@example.com", password = "Strong123!" });
        using var scope = fixture.Server.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await users.FindByEmailAsync("candidate@example.com");
        Assert.NotNull(user);
        var codePrincipal = new ClaimsPrincipal(new ClaimsIdentity("test"));
        codePrincipal.SetClaim(OpenIddictConstants.Claims.Subject, user.Id);
        codePrincipal.SetScopes("openid", "profile", "email", "roles");
        using var requestServices = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(new CodeAuthenticationService(codePrincipal)).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = requestServices };
        http.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction
            {
                Request = new OpenIddictRequest
                {
                    GrantType = grantType, Username = "candidate@example.com", Password = "Strong123!",
                    Scope = grantType == "password" ? "openid profile email roles" : null
                }
            }
        });
        var controller = new AuthorizationController(scope.ServiceProvider.GetRequiredService<SignInManager<IdentityUser>>(), users)
            { ControllerContext = new ControllerContext { HttpContext = http } };
        var result = Assert.IsType<Microsoft.AspNetCore.Mvc.SignInResult>(await controller.Exchange());
        Assert.Equal(user.Id, result.Principal.GetClaim(OpenIddictConstants.Claims.Subject));
        Assert.Contains("profile", result.Principal.GetScopes());
        Assert.True(result.Principal.IsInRole("Candidate"));
        var role = result.Principal.FindFirst("role");
        Assert.NotNull(role);
        Assert.Contains(OpenIddictConstants.Destinations.IdentityToken, role.GetDestinations());
        Assert.Contains(OpenIddictConstants.Destinations.AccessToken, role.GetDestinations());
    }

    private sealed class CodeAuthenticationService(ClaimsPrincipal principal) : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, scheme!)));
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => throw new NotSupportedException();
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => throw new NotSupportedException();
        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) => throw new NotSupportedException();
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => throw new NotSupportedException();
    }

    private sealed class FailingUserManager : UserManager<IdentityUser>
    {
        public FailingUserManager(IServiceProvider services) : base(
            services.GetRequiredService<IUserStore<IdentityUser>>(),
            services.GetRequiredService<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
            services.GetRequiredService<IPasswordHasher<IdentityUser>>(),
            services.GetServices<IUserValidator<IdentityUser>>(),
            services.GetServices<IPasswordValidator<IdentityUser>>(),
            services.GetRequiredService<ILookupNormalizer>(), new IdentityErrorDescriber(), services,
            services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UserManager<IdentityUser>>>()) { }

        public override Task<IdentityResult> AddToRoleAsync(IdentityUser user, string role) =>
            Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "RoleAssignmentFailed" }));
    }

    private sealed class RegistrationServer(TestServer server, SqliteConnection connection) : IAsyncDisposable
    {
        public TestServer Server { get; } = server;
        public HttpClient Client { get; } = server.CreateClient();

        public static async Task<RegistrationServer> CreateAsync(bool seedRole = true, bool failRoleAssignment = false)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var server = new TestServer(new WebHostBuilder().ConfigureServices(services =>
            {
                services.AddScoped<ApplicationDbContext>(_ => new TestDbContext(
                    new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options));
                services.AddIdentity<IdentityUser, IdentityRole>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = 8;
                    options.ClaimsIdentity.RoleClaimType = "role";
                }).AddEntityFrameworkStores<ApplicationDbContext>();
                if (failRoleAssignment) services.AddScoped<UserManager<IdentityUser>, FailingUserManager>();
                services.AddControllers().AddApplicationPart(typeof(AccountController).Assembly);
            }).Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(e => e.MapControllers());
            }));
            using var scope = server.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync();
            if (seedRole)
            {
                var result = await scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>()
                    .CreateAsync(new IdentityRole("Candidate"));
                Assert.True(result.Succeeded);
            }
            return new RegistrationServer(server, connection);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            Server.Dispose();
            await connection.DisposeAsync();
        }
    }
}
