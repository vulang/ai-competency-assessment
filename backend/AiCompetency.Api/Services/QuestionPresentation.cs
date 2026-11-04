using System.Text.Json;
using AiCompetency.Api.Models;

namespace AiCompetency.Api.Services;

public static class QuestionPresentation
{
    /// <summary>The question's topic, or the start of its prompt when it has none.</summary>
    public static string Title(Question q) =>
        q.Metadata.RootElement.ValueKind == JsonValueKind.Object &&
        q.Metadata.RootElement.TryGetProperty("topic", out var topic) && topic.ValueKind == JsonValueKind.String &&
        !string.IsNullOrWhiteSpace(topic.GetString())
            ? topic.GetString()!
            : q.Content.Length <= 80 ? q.Content : q.Content[..79] + "…";

    public static JsonDocument CandidateMetadata(JsonDocument metadata) => JsonSerializer.SerializeToDocument(
        metadata.RootElement.EnumerateObject().Where(p => p.Name is not
            ("answer" or "explanation" or "rationale_hidden" or "rationaleHidden" or "judgePromptHints"))
            .ToDictionary(p => p.Name, p => p.Value.Clone()));
}
