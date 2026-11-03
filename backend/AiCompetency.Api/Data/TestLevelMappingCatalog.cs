using AiCompetency.Api.Models;

namespace AiCompetency.Api.Data;

public static class TestLevelMappingCatalog
{
    // Proposed cut points awaiting expert approval; levels stay empty until a version is published.
    public static TestLevelMapping Draft { get; } = new()
    {
        Id = 1,
        Version = "tl-v1-draft",
        Status = "Draft",
        IsActive = false,
        IntermediateMinPercent = 50m,
        AdvancedMinPercent = 80m,
        MinItemsPerGroup = 5,
        Source = "Proposed for thesis §2.12: below 50% Basic, 50–79.99% Intermediate, 80% or more Advanced; "
            + "at least five objective items per group. Expert review pending.",
        CreatedAt = new DateTimeOffset(2026, 11, 3, 0, 0, 0, TimeSpan.Zero)
    };
}
