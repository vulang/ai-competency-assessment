using AiCompetency.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AiCompetency.Api.Controllers;

[ApiController]
[Route("api/features")]
public sealed class FeaturesController(IOptions<FeatureOptions> features) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { features.Value.AdaptiveTesting });
}
