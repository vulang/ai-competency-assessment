using AiCompetency.Api.Data;
using AiCompetency.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace AiCompetency.Api.Controllers;

public sealed record CloneFrameworkRequest(string Version);
public sealed record IndicatorInput(string Code, int Level, string TextVi, string TextEn);
public sealed record RubricInput(int Level, string AcceptedEvidence, string InsufficientEvidence, string Example);
public sealed record GroupInput(string Code, string NameVi, string NameEn, int Order,
    List<IndicatorInput> Indicators, List<RubricInput> RubricRules);
public sealed record FrameworkContentRequest(List<GroupInput> Groups);
public sealed record PublishFrameworkRequest(string ReviewedBy, string ReviewNotes);

[ApiController]
[Route("api/framework/cv")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Admin")]
public sealed class CvFrameworkAdminController(ApplicationDbContext context) : ControllerBase
{
    private static readonly string[] Codes = ["FOUNDATION", "DATA", "CRITICAL_THINKING", "APPLICATION", "ETHICS", "TOOLS", "FUTURE_OF_WORK"];

    [HttpPost("{sourceVersion}/drafts")]
    public async Task<IActionResult> Clone(string sourceVersion, CloneFrameworkRequest request)
    {
        var target = request.Version?.Trim() ?? "";
        if (target.Length is < 3 or > 64 || !System.Text.RegularExpressions.Regex.IsMatch(target, "^[a-z0-9][a-z0-9._-]*$"))
            return BadRequest("Use a unique version of 3–64 lowercase letters, digits, dots, dashes, or underscores.");
        if (await context.CvFrameworkVersions.AnyAsync(v => v.Version == target)) return Conflict("Version already exists.");
        var source = await Load(sourceVersion);
        if (source == null) return NotFound();
        var clone = new CvFrameworkVersion { Version = target, Status = "Draft", Source = $"Copy of {source.Version}; expert review pending." };
        foreach (var group in source.Groups)
        {
            var copy = new CvCompetencyGroup { Code = group.Code, NameVi = group.NameVi, NameEn = group.NameEn, Order = group.Order };
            foreach (var indicator in group.Indicators)
                copy.Indicators.Add(new BehaviorIndicator { Code = indicator.Code, Level = indicator.Level, TextVi = indicator.TextVi, TextEn = indicator.TextEn });
            foreach (var rule in group.RubricRules)
                copy.RubricRules.Add(new CvRubricRule { Level = rule.Level, AcceptedEvidence = rule.AcceptedEvidence,
                    InsufficientEvidence = rule.InsufficientEvidence, Example = rule.Example });
            clone.Groups.Add(copy);
        }
        context.CvFrameworkVersions.Add(clone);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(FrameworkController.GetIndicators), "Framework", new { version = target }, new { clone.Version, clone.Status });
    }

    [HttpPut("{version}/content")]
    public async Task<IActionResult> UpdateContent(string version, FrameworkContentRequest request)
    {
        var framework = await Load(version);
        if (framework == null) return NotFound();
        if (framework.Status != "Draft") return Conflict("Published versions are immutable. Copy this version to make changes.");
        var error = Validate(request);
        if (error != null) return BadRequest(error);

        // Synchronize stable indicator codes and group keys within this draft.
        var inputs = request.Groups.ToDictionary(g => g.Code);
        foreach (var group in framework.Groups)
        {
            var input = inputs[group.Code];
            group.NameVi = input.NameVi.Trim();
            group.NameEn = input.NameEn.Trim();
            group.Order = input.Order;
            var newIndicators = input.Indicators.ToDictionary(i => i.Code);
            foreach (var existing in group.Indicators.ToList())
            {
                if (!newIndicators.TryGetValue(existing.Code, out var item))
                    context.BehaviorIndicators.Remove(existing);
                else { existing.Level = item.Level; existing.TextVi = item.TextVi.Trim(); existing.TextEn = item.TextEn.Trim(); }
            }
            foreach (var item in input.Indicators.Where(i => group.Indicators.All(existing => existing.Code != i.Code)))
                group.Indicators.Add(new BehaviorIndicator { Code = item.Code, Level = item.Level, TextVi = item.TextVi.Trim(), TextEn = item.TextEn.Trim() });
            var newRules = input.RubricRules.ToDictionary(r => r.Level);
            foreach (var existing in group.RubricRules)
            {
                var item = newRules[existing.Level];
                existing.AcceptedEvidence = item.AcceptedEvidence.Trim();
                existing.InsufficientEvidence = item.InsufficientEvidence.Trim();
                existing.Example = item.Example.Trim();
            }
        }
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{version}/publish")]
    public async Task<IActionResult> Publish(string version, PublishFrameworkRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReviewedBy) || string.IsNullOrWhiteSpace(request.ReviewNotes))
            return BadRequest("Record the expert reviewer and review decision before publishing.");
        if (version.EndsWith("-draft", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Copy the seed draft to a reviewed version name before publishing.");
        var framework = await Load(version);
        if (framework == null) return NotFound();
        if (framework.Status != "Draft") return Conflict("Only drafts can be published.");
        var content = new FrameworkContentRequest(framework.Groups.Select(g => new GroupInput(g.Code, g.NameVi, g.NameEn, g.Order,
            g.Indicators.Select(i => new IndicatorInput(i.Code, i.Level, i.TextVi, i.TextEn)).ToList(),
            g.RubricRules.Select(r => new RubricInput(r.Level, r.AcceptedEvidence, r.InsufficientEvidence, r.Example)).ToList())).ToList());
        var error = Validate(content);
        if (error != null) return BadRequest(error);

        await using var transaction = context.Database.IsRelational() ? await context.Database.BeginTransactionAsync() : null;
        var previous = await context.CvFrameworkVersions.SingleOrDefaultAsync(v => v.IsActive);
        if (previous != null) { previous.IsActive = false; await context.SaveChangesAsync(); }
        framework.Status = "Published";
        framework.PublishedAt = DateTimeOffset.UtcNow;
        framework.IsActive = true;
        framework.Source = $"Expert reviewed by {request.ReviewedBy.Trim()}: {request.ReviewNotes.Trim()}";
        await context.SaveChangesAsync();
        if (transaction != null) await transaction.CommitAsync();
        return Ok(new { framework.Version, framework.PublishedAt, framework.IsActive });
    }

    private async Task<CvFrameworkVersion?> Load(string version) => await context.CvFrameworkVersions
        .Include(v => v.Groups).ThenInclude(g => g.Indicators)
        .Include(v => v.Groups).ThenInclude(g => g.RubricRules)
        .SingleOrDefaultAsync(v => v.Version == version);

    private static string? Validate(FrameworkContentRequest request)
    {
        if (request.Groups == null || request.Groups.Count != 7 || request.Groups.Any(g => g is null) || !request.Groups.Select(g => g.Code).Order().SequenceEqual(Codes.Order()))
            return "Provide the seven canonical group codes exactly once.";
        if (!request.Groups.Select(g => g.Order).Order().SequenceEqual(Enumerable.Range(1, 7)))
            return "Group order must contain 1–7 exactly once.";
        foreach (var group in request.Groups)
        {
            if (string.IsNullOrWhiteSpace(group.NameVi) || string.IsNullOrWhiteSpace(group.NameEn))
                return $"{group.Code}: both names are required.";
            if (group.Indicators == null || group.Indicators.Count < 9 || group.Indicators.Count > 18 ||
                group.Indicators.GroupBy(i => i.Code).Any(g => g.Count() > 1))
                return $"{group.Code}: provide unique indicator codes and 3–6 indicators per positive level.";
            foreach (var level in new[] { 1, 2, 3 })
                if (group.Indicators.Count(i => i.Level == level) is < 3 or > 6)
                    return $"{group.Code}: level {level} requires 3–6 indicators.";
            if (group.Indicators.Any(i => string.IsNullOrWhiteSpace(i.Code) || i.Code.Length is < 3 or > 96 || !i.Code.StartsWith($"{group.Code}.{i.Level}.", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(i.TextVi) || string.IsNullOrWhiteSpace(i.TextEn)))
                return $"{group.Code}: indicator codes and bilingual text are required.";
            if (group.RubricRules == null || group.RubricRules.Count != 4 ||
                !group.RubricRules.Select(r => r.Level).Order().SequenceEqual(new[] { 0, 1, 2, 3 }) ||
                group.RubricRules.Any(r => string.IsNullOrWhiteSpace(r.AcceptedEvidence) || string.IsNullOrWhiteSpace(r.InsufficientEvidence) || string.IsNullOrWhiteSpace(r.Example)))
                return $"{group.Code}: provide complete rubric rules for levels 0–3.";
        }
        return null;
    }
}
