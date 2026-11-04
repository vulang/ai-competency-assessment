using System.Globalization;
using System.Text;
using System.Text.Json;
using AiCompetency.Api.Data;
using AiCompetency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AiCompetency.Api.Services;

public sealed record ItemAnalysisItem(int QuestionId, int OrderIndex, string Title, string Type,
    string? GroupCode, int? TargetLevel, List<string> Options);

public sealed record ItemAnalysisReport(int ExamId, string ExamTitle, string ExamStatus, DateTime GeneratedAt,
    List<ItemAnalysisItem> Items, int ExcludedNonObjective, CttAnalysis Analysis);

/// <summary>Response matrix of one test form, rebuilt from stored responses on every request.</summary>
public sealed record ResponseMatrix(Exam Exam, List<ExamQuestion> Items, int ExcludedNonObjective,
    List<(ExamResult Session, List<double> Scores, List<List<int>> Selections)> Rows);

/// <summary>
/// Builds CTT input for a test form from completed sessions and calls the ML service.
/// Respondents are identified by session ID only. Rows are ordered by completion time.
/// </summary>
public sealed class ItemAnalysisService(ApplicationDbContext context, MlService ml, IScoringService scoring)
{
    public async Task<ResponseMatrix?> LoadMatrixAsync(int examId)
    {
        var exam = await context.Exams.Include(e => e.ExamQuestions).ThenInclude(eq => eq.Question)
            .SingleOrDefaultAsync(e => e.Id == examId);
        if (exam == null) return null;
        var ordered = exam.ExamQuestions.OrderBy(eq => eq.OrderIndex).ThenBy(eq => eq.QuestionId).ToList();
        var items = ordered.Where(eq => ScoringService.IsObjective(eq.Question!.Type) && eq.PointValue > 0).ToList();
        var sessions = await context.ExamResults.Include(s => s.Responses)
            .Where(s => s.ExamId == examId && s.Status == "Completed")
            .OrderBy(s => s.EndTime).ThenBy(s => s.Id)
            .ToListAsync();
        var rows = sessions.Select(session =>
        {
            var answers = session.Responses.ToDictionary(r => r.QuestionId);
            var scores = items.Select(eq => answers.TryGetValue(eq.QuestionId, out var r) && r.ScoreEarned >= eq.PointValue ? 1d : 0d).ToList();
            var selections = items.Select(eq => scoring.SelectedOptions(eq.Question!, answers.GetValueOrDefault(eq.QuestionId)?.FinalAnswer).ToList()).ToList();
            return (session, scores, selections);
        }).ToList();
        return new ResponseMatrix(exam, items, ordered.Count - items.Count, rows);
    }

    /// <returns>null when the exam does not exist; throws when the ML service is unavailable.</returns>
    public async Task<ItemAnalysisReport?> AnalyzeAsync(int examId)
    {
        var matrix = await LoadMatrixAsync(examId);
        if (matrix == null) return null;
        if (matrix.Items.Count == 0)
            throw new InvalidOperationException("This test has no objective questions to analyze.");
        var request = new CttAnalyzeRequest(
            matrix.Items.Select(eq =>
            {
                var layout = scoring.Options(eq.Question!);
                return new CttItemInput(eq.QuestionId.ToString(CultureInfo.InvariantCulture), eq.Question!.GroupCode,
                    layout?.OptionCount, layout?.Key.ToList() ?? []);
            }).ToList(),
            matrix.Rows.Select(r => new CttRespondentInput(r.Session.Id.ToString(), r.Scores, r.Selections)).ToList());
        var analysis = await ml.AnalyzeCttAsync(request)
            ?? throw new HttpRequestException("The ML service did not return a CTT analysis.");
        var items = matrix.Items.Select(eq => new ItemAnalysisItem(eq.QuestionId, eq.OrderIndex, QuestionPresentation.Title(eq.Question!),
            eq.Question!.Type, eq.Question.GroupCode, eq.Question.TargetLevel, Options(eq.Question))).ToList();
        return new ItemAnalysisReport(matrix.Exam.Id, matrix.Exam.Title, matrix.Exam.Status, DateTime.UtcNow,
            items, matrix.ExcludedNonObjective, analysis);
    }

    public static string ItemsCsv(ItemAnalysisReport report)
    {
        var maxOptions = report.Analysis.Items.Select(i => i.Distractors.Count).DefaultIfEmpty(0).Max();
        var csv = new StringBuilder();
        csv.AppendLine(string.Join(',', new[] { "question_id", "order", "group", "level", "type", "title", "n", "p",
                "item_rest_r", "item_rest_r_group", "discrimination_index", "alpha_if_deleted", "omit_rate", "flags", "key" }
            .Concat(Enumerable.Range(0, maxOptions).Select(o => $"option_{(char)('A' + o)}"))));
        var info = report.Items.ToDictionary(i => i.QuestionId.ToString(CultureInfo.InvariantCulture));
        foreach (var item in report.Analysis.Items)
        {
            var meta = info[item.ItemId];
            var key = string.Concat(item.Distractors.Where(d => d.IsKey).Select(d => (char)('A' + d.Option)));
            csv.AppendLine(string.Join(',', new[] { item.ItemId, meta.OrderIndex.ToString(CultureInfo.InvariantCulture),
                    meta.GroupCode ?? "", meta.TargetLevel?.ToString(CultureInfo.InvariantCulture) ?? "", meta.Type, Quote(meta.Title),
                    item.N.ToString(CultureInfo.InvariantCulture), Number(item.P), Number(item.ItemRestR), Number(item.ItemRestRGroup),
                    Number(item.DiscriminationIndex), Number(item.AlphaIfDeleted), Number(item.OmitRate),
                    string.Join(';', item.Flags), key }
                .Concat(Enumerable.Range(0, maxOptions).Select(o => o < item.Distractors.Count ? Number(item.Distractors[o].Proportion) : ""))));
        }
        return csv.ToString();
    }

    public static string ReliabilityCsv(ItemAnalysisReport report)
    {
        var a = report.Analysis;
        var csv = new StringBuilder("scope,n_respondents,n_items,mean_percent,alpha,omega,excluded_items,notes\n");
        var overallMean = a.Total.Mean.HasValue && a.NItems > 0 ? a.Total.Mean / a.NItems * 100 : null;
        csv.AppendLine(string.Join(',', "TOTAL", a.NRespondents.ToString(CultureInfo.InvariantCulture),
            a.Reliability.NItems.ToString(CultureInfo.InvariantCulture), Number(overallMean), Number(a.Reliability.Alpha),
            Number(a.Reliability.Omega), Quote(string.Join(';', a.Reliability.ExcludedItems)), Quote(string.Join(' ', a.Reliability.Notes))));
        foreach (var g in a.Groups)
            csv.AppendLine(string.Join(',', g.Group, a.NRespondents.ToString(CultureInfo.InvariantCulture),
                g.NItems.ToString(CultureInfo.InvariantCulture), Number(g.MeanPercent), Number(g.Alpha), Number(g.Omega),
                Quote(string.Join(';', g.ExcludedItems)), Quote(string.Join(' ', g.Notes))));
        return csv.ToString();
    }

    /// <summary>Scored 0/1 matrix for independent re-analysis; rows are pseudonymous session IDs.</summary>
    public static string ResponsesCsv(ResponseMatrix matrix)
    {
        var csv = new StringBuilder();
        csv.AppendLine(string.Join(',', new[] { "session_id", "completed_at" }
            .Concat(matrix.Items.Select(eq => $"q{eq.QuestionId}")).Append("total")));
        foreach (var (session, scores, _) in matrix.Rows)
            csv.AppendLine(string.Join(',', new[] { session.Id.ToString(), session.EndTime?.ToString("O", CultureInfo.InvariantCulture) ?? "" }
                .Concat(scores.Select(s => s.ToString(CultureInfo.InvariantCulture)))
                .Append(scores.Sum().ToString(CultureInfo.InvariantCulture))));
        return csv.ToString();
    }

    private static string Number(double? value) => value?.ToString("0.####", CultureInfo.InvariantCulture) ?? "";

    // Quotes CSV text and neutralizes leading characters that spreadsheets treat as formulas.
    private static string Quote(string value)
    {
        if (value.Length > 0 && "=+-@".Contains(value[0])) value = "'" + value;
        return value.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }

    private static List<string> Options(Question q) =>
        q.Type == "true_false" ? ["True", "False"]
        : q.Metadata.RootElement.ValueKind == JsonValueKind.Object &&
          q.Metadata.RootElement.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array
            ? options.EnumerateArray().Select(o => o.ValueKind == JsonValueKind.String ? o.GetString()! : o.GetRawText()).ToList()
            : [];
}
