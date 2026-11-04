using System.Security.Claims;

namespace AiCompetency.Api.Services;

public static class CurrentUser
{
    public static string GetRequiredId(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(id)
            ? id
            : throw new UnauthorizedAccessException("An authenticated subject is required.");
    }

    /// <summary>Human-readable identity for audit fields; falls back to the subject ID.</summary>
    public static string GetDisplayName(ClaimsPrincipal principal) =>
        new[] { "email", ClaimTypes.Email, "name", ClaimTypes.Name, "preferred_username" }
            .Select(principal.FindFirstValue).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
        ?? GetRequiredId(principal);
}
