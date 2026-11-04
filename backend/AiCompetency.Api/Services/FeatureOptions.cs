using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace AiCompetency.Api.Services;

public sealed class FeatureOptions
{
    public const string Section = "Features";

    // IRT/CAT/BKT are retained as an experimental extension outside the thesis scope.
    public bool AdaptiveTesting { get; set; }
}

/// <summary>Hides an endpoint (404) unless experimental adaptive testing is enabled.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiresAdaptiveTestingAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        if (!context.HttpContext.RequestServices.GetRequiredService<IOptions<FeatureOptions>>().Value.AdaptiveTesting)
            context.Result = new NotFoundResult();
    }

    public void OnResourceExecuted(ResourceExecutedContext context) { }
}
