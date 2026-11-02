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
}
