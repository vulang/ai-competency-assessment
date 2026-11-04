using System.Text.Json;
using AiCompetency.Api.Models;

namespace AiCompetency.Api.Services;

public enum AnswerOutcome { Correct, Incorrect, Unanswered, ManualReview }

public sealed record ScoredAnswer(AnswerOutcome Outcome, decimal Score, string Feedback)
{
    public bool IsCorrect => Outcome == AnswerOutcome.Correct;
}

/// <summary>Option layout of an objective item, used for distractor analysis.</summary>
public sealed record OptionLayout(int OptionCount, IReadOnlyList<int> Key);

public interface IScoringService
{
    ScoredAnswer Score(Question question, string? answer, decimal points);
    OptionLayout? Options(Question question);
    IReadOnlyList<int> SelectedOptions(Question question, string? answer);
}

/// <summary>Shared dichotomous scoring for standard and adaptive tests.</summary>
public sealed class ScoringService : IScoringService
{
    private static readonly HashSet<string> ObjectiveTypes = ["mcq_single", "mcq_multi", "true_false"];

    // Only these types are machine-scored and enter CTT; open scenario answers need rubric review.
    public static bool IsObjective(string type) => ObjectiveTypes.Contains(type);

    public ScoredAnswer Score(Question question, string? answer, decimal points)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return new(AnswerOutcome.Unanswered, 0, "Please provide an answer next time.");
        if (question.Type == "scenario")
            return new(AnswerOutcome.ManualReview, 0, "Answer submitted for manual review.");
        return AnswerEvaluator.IsCorrect(question.Type, question.Metadata, answer)
            ? new(AnswerOutcome.Correct, points, "Correct!")
            : new(AnswerOutcome.Incorrect, 0, "Incorrect.");
    }

    public OptionLayout? Options(Question question)
    {
        var root = question.Metadata.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("answer", out var expected)) return null;
        if (question.Type == "true_false")
            return expected.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? new(2, [expected.GetBoolean() ? 0 : 1]) : null;
        if (question.Type is not ("mcq_single" or "mcq_multi") ||
            !root.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array)
            return null;
        var keys = expected.ValueKind == JsonValueKind.Array
            ? expected.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToList()
            : expected.ValueKind == JsonValueKind.String ? [expected.GetString()!] : [];
        var indexes = keys.Select(k => AnswerEvaluator.OptionIndex(question.Metadata, k)).ToList();
        if (indexes.Count == 0 || indexes.Any(i => i == null)) return null;
        return new(options.GetArrayLength(), indexes.Select(i => i!.Value).Distinct().Order().ToList());
    }

    public IReadOnlyList<int> SelectedOptions(Question question, string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer)) return [];
        if (question.Type == "true_false")
            return bool.TryParse(answer, out var value) ? [value ? 0 : 1] : [];
        IEnumerable<string> choices = [answer];
        if (question.Type == "mcq_multi")
        {
            try { choices = JsonSerializer.Deserialize<List<string>>(answer) ?? []; }
            catch (JsonException) { return []; }
        }
        return choices.Select(c => AnswerEvaluator.OptionIndex(question.Metadata, c))
            .Where(i => i != null).Select(i => i!.Value).Distinct().Order().ToList();
    }
}
