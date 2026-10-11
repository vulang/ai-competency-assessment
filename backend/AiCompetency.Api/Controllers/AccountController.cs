using System.ComponentModel.DataAnnotations;
using AiCompetency.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AiCompetency.Api.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountController(
    ApplicationDbContext context,
    UserManager<IdentityUser> users,
    RoleManager<IdentityRole> roles) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCandidateRequest request, CancellationToken cancellationToken)
    {
        if (!await roles.RoleExistsAsync("Candidate"))
            return Problem("Candidate registration is temporarily unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);

        // Identity saves user creation and role assignment separately. Commit both
        // together so an assignment failure cannot leave a partially registered user.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var email = request.Email.Trim();
        var user = new IdentityUser { UserName = email, Email = email };
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors)
                ModelState.AddModelError(error.Code.StartsWith("Password", StringComparison.Ordinal) ? "password" : "email", error.Description);
            return ValidationProblem(ModelState);
        }

        // Public signup never accepts a role from the caller.
        var assigned = await users.AddToRoleAsync(user, "Candidate");
        if (!assigned.Succeeded)
            return Problem("Candidate registration is temporarily unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);

        await transaction.CommitAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { Email = email, Message = "Account created. Sign in to start your assessment." });
    }
}

public sealed class RegisterCandidateRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}
