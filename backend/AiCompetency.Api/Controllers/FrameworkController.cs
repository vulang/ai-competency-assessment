using AiCompetency.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace AiCompetency.Api.Controllers;

[ApiController]
[Route("api/framework")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public sealed class FrameworkController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var framework = await context.Frameworks.Include(f => f.Domains).ThenInclude(d => d.Skills)
            .ThenInclude(s => s.Criteria).SingleOrDefaultAsync(f => f.IsActive);
        if (framework == null) return NotFound("No active competency framework.");
        return Ok(new
        {
            framework.Id, framework.Name, framework.Version,
            Domains = framework.Domains.OrderBy(d => d.Id).Select(d => new
            {
                d.Id, d.Code, d.Name, d.Weight,
                Skills = d.Skills.OrderBy(s => s.Id).Select(s => new
                {
                    s.Id, s.Name, s.Description, s.Weight, s.IsDefault,
                    Criteria = s.Criteria.OrderBy(c => c.Level).Select(c => new { c.Id, c.Name, c.Level, c.Description })
                })
            })
        });
    }
}
