using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AiCompetency.Api.Controllers;
using AiCompetency.Api.Data;
using AiCompetency.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;
using Xunit;

namespace AiCompetency.Api.Tests;

public class AuthorizationTests
{
    [Fact]
    public async Task AdminRoutesReturn401ForAnonymous403ForCandidateAnd200ForAdmin()
    {
        using var server = new TestServer(new WebHostBuilder().ConfigureServices(services =>
        {
            services.AddScoped<ApplicationDbContext>(_ => new TestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options));
            services.AddHttpClient<MlService>(c => c.BaseAddress = new Uri("http://ml.test"));
            services.AddScoped<CompetencyProfileService>();
            services.AddScoped<QuestionClassificationService>();
            services.AddSingleton<ProfileJobQueue>();
            services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, _ => { });
            services.AddAuthorization();
            services.AddControllers().AddApplicationPart(typeof(QuestionsController).Assembly);
        }).Configure(app =>
        {
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(e => e.MapControllers());
        }));
        using var client = server.CreateClient();
        foreach (var route in new[] { "/api/questions", "/api/tests", "/api/competency/admin/irt-stats" })
        {
            client.DefaultRequestHeaders.Remove("Test-Role");
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);
            client.DefaultRequestHeaders.Add("Test-Role", "Candidate");
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(route)).StatusCode);
            client.DefaultRequestHeaders.Remove("Test-Role");
            client.DefaultRequestHeaders.Add("Test-Role", "Admin");
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(route)).StatusCode);
        }
    }

    private sealed class HeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Request.Headers["Test-Role"].ToString();
            if (role.Length == 0) return Task.FromResult(AuthenticateResult.NoResult());
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "test-user"), new Claim("role", role)], Scheme.Name, "sub", "role"));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
