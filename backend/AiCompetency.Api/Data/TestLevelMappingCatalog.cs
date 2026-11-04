using AiCompetency.Api.Models;

namespace AiCompetency.Api.Data;

public static class TestLevelMappingCatalog
{
    // The proposal as reviewed; kept unchanged for traceability.
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

    // Approved release of the draft's cut points, used for all submissions from Phase 2 onward.
    public static TestLevelMapping V1 { get; } = new()
    {
        Id = 2,
        Version = "tl-v1",
        Status = "Published",
        IsActive = true,
        IntermediateMinPercent = 50m,
        AdvancedMinPercent = 80m,
        MinItemsPerGroup = 5,
        Source = "Thesis §2.12 rule from tl-v1-draft: below 50% Basic, 50–79.99% Intermediate, 80% or more Advanced; "
            + "at least five objective items per group.",
        ReviewedBy = "Project owner confirmation of expert approval",
        ReviewNotes = "The project owner confirmed expert approval of tl-v1-draft as proposed on 2026-11-04. "
            + "The expert's name and review date were not supplied.",
        PublishedAt = new DateTimeOffset(2026, 11, 4, 0, 0, 0, TimeSpan.Zero),
        CreatedAt = new DateTimeOffset(2026, 11, 4, 0, 0, 0, TimeSpan.Zero)
    };
}
