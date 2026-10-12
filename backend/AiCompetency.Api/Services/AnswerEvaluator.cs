using System.Text.Json;
using System.Text.RegularExpressions;

namespace AiCompetency.Api.Services;

public static class AnswerEvaluator
{
    public static bool IsCorrect(string type, JsonDocument? metadata, string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer) || type == "scenario" || metadata == null ||
            metadata.RootElement.ValueKind != JsonValueKind.Object ||
            !metadata.RootElement.TryGetProperty("answer", out var expected))
            return false;

        var root = metadata.RootElement;
        if (type == "true_false" && expected.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return bool.TryParse(answer, out var submitted) && submitted == expected.GetBoolean();

        if (type == "mcq_multi")
        {
            try
            {
                var submitted = JsonSerializer.Deserialize<List<string>>(answer);
                if (submitted == null || submitted.Count == 0 || submitted.Any(string.IsNullOrWhiteSpace) ||
                    expected.ValueKind != JsonValueKind.Array ||
                    expected.EnumerateArray().Any(e => e.ValueKind != JsonValueKind.String))
                    return false;
                var correct = expected.EnumerateArray().Select(e => ChoiceKey(root, e.GetString()!)).ToHashSet();
                var actual = submitted.Select(s => ChoiceKey(root, s)).ToHashSet();
                return correct.Count > 0 && actual.Count == submitted.Count && actual.SetEquals(correct);
            }
            catch (JsonException) { return false; }
        }

        if (expected.ValueKind == JsonValueKind.Array)
        {
            if (expected.GetArrayLength() != 1) return false;
            expected = expected[0];
        }
        if (expected.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(expected.GetString())) return false;
        return type == "mcq_single"
            ? ChoiceKey(root, answer) == ChoiceKey(root, expected.GetString()!)
            : string.Equals(answer.Trim(), expected.GetString()!.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    // The generator emits letter keys while the UI submits displayed option text.
    // Resolve both against the declared choices; arbitrary prefixed text is not a choice.
    private static (int? Index, string Text) ChoiceKey(JsonElement root, string answer)
    {
        var value = answer.Trim();
        if (root.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array)
        {
            var choices = options.EnumerateArray().ToList();
            for (var index = 0; index < choices.Count; index++)
            {
                if (choices[index].ValueKind != JsonValueKind.String) continue;
                var text = choices[index].GetString()!.Trim();
                if (string.Equals(value, text, StringComparison.OrdinalIgnoreCase)) return (index, "");
                var label = Regex.Match(text, @"^([A-Z])[.)\s:]", RegexOptions.IgnoreCase);
                if (label.Success && string.Equals(value, label.Groups[1].Value, StringComparison.OrdinalIgnoreCase)) return (index, "");
            }
            if (value.Length == 1 && char.ToUpperInvariant(value[0]) is >= 'A' and <= 'Z')
            {
                var index = char.ToUpperInvariant(value[0]) - 'A';
                if (index < choices.Count) return (index, "");
            }
        }
        return (null, value.ToUpperInvariant());
    }

}
