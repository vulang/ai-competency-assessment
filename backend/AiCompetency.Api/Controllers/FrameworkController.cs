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

    [HttpGet("cv/versions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetCvVersions() => Ok(await context.CvFrameworkVersions.AsNoTracking()
        .OrderBy(v => v.Id).Select(v => new { v.Version, v.Status, v.IsActive, v.PublishedAt, v.Source }).ToListAsync());

    [HttpGet("cv/active")]
    public async Task<IActionResult> GetActiveCv()
    {
        var version = await context.CvFrameworkVersions.AsNoTracking()
            .SingleOrDefaultAsync(v => v.IsActive && v.Status == "Published");
        return version == null ? NotFound("No reviewed CV framework has been published.") : await GetIndicators(version.Version);
    }

    [HttpGet("{version}/indicators")]
    public async Task<IActionResult> GetIndicators(string version)
    {
        var framework = await context.CvFrameworkVersions.AsNoTracking()
            .Include(v => v.Groups).ThenInclude(g => g.Indicators)
            .Include(v => v.Groups).ThenInclude(g => g.RubricRules)
            .SingleOrDefaultAsync(v => v.Version == version);
        if (framework == null || (framework.Status != "Published" && !User.IsInRole("Admin")))
            return NotFound("CV framework version not found.");
        var levels = await context.ProficiencyLevels.AsNoTracking().OrderBy(l => l.Ordinal)
            .Select(l => new { l.Code, l.Ordinal, l.NameVi, l.NameEn }).ToListAsync();
        return Ok(new
        {
            framework.Version, framework.Status, framework.IsActive, framework.PublishedAt, framework.Source,
            Levels = levels,
            Groups = framework.Groups.OrderBy(g => g.Order).Select(g => new
            {
                g.Code, g.NameVi, g.NameEn, g.Order,
                Indicators = g.Indicators.OrderBy(i => i.Level).ThenBy(i => i.Code)
                    .Select(i => new { i.Code, i.Level, i.TextVi, i.TextEn }),
                RubricRules = g.RubricRules.OrderBy(r => r.Level)
                    .Select(r => new { r.Level, r.AcceptedEvidence, r.InsufficientEvidence, r.Example })
            })
        });
    }

}
