using System.Text.Json;

namespace AiCompetency.Api.Services;

public static class QuestionPresentation
{
    public static JsonDocument CandidateMetadata(JsonDocument metadata) => JsonSerializer.SerializeToDocument(
        metadata.RootElement.EnumerateObject().Where(p => p.Name is not
            ("answer" or "explanation" or "rationale_hidden" or "rationaleHidden" or "judgePromptHints"))
            .ToDictionary(p => p.Name, p => p.Value.Clone()));
}
