using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiCompetency.Api.Data;

// EF tooling must not execute application startup (database seeding/certificates).
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=ai_competency;Username=postgres")
            .UseOpenIddict().Options;
        return new ApplicationDbContext(options);
    }
}
