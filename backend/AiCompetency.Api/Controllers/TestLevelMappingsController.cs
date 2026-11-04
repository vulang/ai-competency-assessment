using System.Text.RegularExpressions;
using AiCompetency.Api.Data;
using AiCompetency.Api.Models;
using AiCompetency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace AiCompetency.Api.Controllers;

public sealed record SaveLevelMappingRequest(string Version, decimal IntermediateMinPercent, decimal AdvancedMinPercent,
    int MinItemsPerGroup, string? Source);
public sealed record PublishLevelMappingRequest(string ReviewedBy, string? ReviewNotes);

/// <summary>
/// Versioned rules that convert group percent correct to Basic/Intermediate/Advanced (thesis §2.12).
/// The rule must be fixed before data collection: published versions are immutable.
/// </summary>
[ApiController]
[Route("api/test-level-mappings")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Admin")]
public sealed class TestLevelMappingsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await context.TestLevelMappings.OrderByDescending(m => m.IsActive).ThenByDescending(m => m.CreatedAt).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(SaveLevelMappingRequest request)
    {
        var error = Validate(request);
        if (error != null) return BadRequest(error);
        var version = request.Version.Trim();
        if (await context.TestLevelMappings.AnyAsync(m => m.Version == version)) return Conflict("Version already exists.");
        var mapping = new TestLevelMapping { Version = version, Status = "Draft" };
        Apply(mapping, request);
        context.TestLevelMappings.Add(mapping);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(List), mapping);
    }

    [HttpPut("{version}")]
    public async Task<IActionResult> Update(string version, SaveLevelMappingRequest request)
    {
        var mapping = await context.TestLevelMappings.SingleOrDefaultAsync(m => m.Version == version);
        if (mapping == null) return NotFound();
        if (mapping.Status != "Draft") return Conflict("Published mappings are immutable. Create a new version instead.");
        var error = Validate(request with { Version = version });
        if (error != null) return BadRequest(error);
        Apply(mapping, request);
        await context.SaveChangesAsync();
        return Ok(mapping);
    }

    /// <summary>
    /// Activates an approved mapping. Group scores recorded without any mapping receive levels from it;
    /// scores already mapped by an earlier version keep that version.
    /// </summary>
    [HttpPost("{version}/publish")]
    public async Task<IActionResult> Publish(string version, PublishLevelMappingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReviewedBy) || request.ReviewedBy.Trim().Length > 256)
            return BadRequest("Record who approved the mapping (at most 256 characters).");
        var mapping = await context.TestLevelMappings.SingleOrDefaultAsync(m => m.Version == version);
        if (mapping == null) return NotFound();
        if (mapping.Status != "Draft") return Conflict("This mapping is already published.");

        await using var transaction = await context.Database.BeginTransactionAsync();
        foreach (var active in await context.TestLevelMappings.Where(m => m.IsActive).ToListAsync())
            active.IsActive = false;
        await context.SaveChangesAsync();
        mapping.Status = "Published";
        mapping.IsActive = true;
        mapping.PublishedAt = DateTimeOffset.UtcNow;
        mapping.ReviewedBy = request.ReviewedBy.Trim();
        mapping.ReviewNotes = string.IsNullOrWhiteSpace(request.ReviewNotes) ? null : request.ReviewNotes.Trim();
        var unmapped = await context.TestGroupScores.Where(s => s.LevelMappingVersion == null).ToListAsync();
        foreach (var score in unmapped)
        {
            score.Level = GroupScoreCalculator.MapLevel(score.Percent, score.ItemCount, mapping);
            score.LevelMappingVersion = mapping.Version;
        }
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { Mapping = mapping, BackfilledGroupScores = unmapped.Count });
    }

    private static string? Validate(SaveLevelMappingRequest request)
    {
        if (!Regex.IsMatch(request.Version?.Trim() ?? "", "^[a-z0-9][a-z0-9._-]{2,63}$"))
            return "Use a version of 3–64 lowercase letters, digits, dots, dashes, or underscores.";
        if (request.IntermediateMinPercent is <= 0 or >= 100 || request.AdvancedMinPercent is <= 0 or > 100 ||
            request.IntermediateMinPercent >= request.AdvancedMinPercent)
            return "Thresholds must satisfy 0 < Intermediate < Advanced ≤ 100.";
        if (request.MinItemsPerGroup is < 1 or > TestBlueprints.MaxPerGroup)
            return $"Minimum items per group must be between 1 and {TestBlueprints.MaxPerGroup}.";
        return null;
    }

    private static void Apply(TestLevelMapping mapping, SaveLevelMappingRequest request)
    {
        mapping.IntermediateMinPercent = request.IntermediateMinPercent;
        mapping.AdvancedMinPercent = request.AdvancedMinPercent;
        mapping.MinItemsPerGroup = request.MinItemsPerGroup;
        mapping.Source = request.Source?.Trim() ?? mapping.Source;
    }
}
