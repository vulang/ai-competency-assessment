using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;
using AiCompetency.Api.Data;
using AiCompetency.Api.DTOs;
using AiCompetency.Api.Models;
using AiCompetency.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiCompetency.Api.Controllers;

/// <summary>
/// Test forms built from blueprints (thesis §2.12). A draft form can be assembled and edited;
/// publishing freezes its questions so every candidate answers the same items for CTT.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Admin")]
[Route("api/[controller]")]
public class TestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TestsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TestBlueprintDto>>> GetTests()
    {
        var exams = await _context.Exams.Include(e => e.ExamQuestions)
            .Where(e => e.Blueprint != null || e.Description != LegacyDynamicDescription)
            .OrderByDescending(e => e.UpdatedAt).ToListAsync();
        return Ok(exams.Select(e => Fill(new TestBlueprintDto(), e)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TestDetailDto>> GetTest(int id)
    {
        var exam = await LoadExam(id);
        if (exam == null) return NotFound();
        return Ok(await Detail(exam));
    }

    /// <summary>Assembles a form from the current bank without saving it.</summary>
    [HttpPost("preview")]
    public async Task<ActionResult<TestPreviewDto>> Preview(SaveTestRequest request)
    {
        var spec = ToSpec(request);
        var error = ValidateRequest(request, spec);
        if (error != null) return BadRequest(error);
        var assembly = TestBlueprints.Assemble(spec, await EligibleQuestions());
        var items = assembly.Questions.Select((q, index) => ToQuestionDto(q, index + 1, 1m)).ToList();
        return Ok(new TestPreviewDto(spec.Seed, items, Coverage(spec, items), assembly.Warnings, StandardIssues(items)));
    }

    [HttpPost]
    public async Task<ActionResult<TestDetailDto>> CreateTest(SaveTestRequest request)
    {
        var spec = ToSpec(request);
        var error = ValidateRequest(request, spec);
        if (error != null) return BadRequest(error);
        var exam = new Exam { Status = "draft", CreatedAt = DateTime.UtcNow };
        Apply(exam, request, spec);
        var assembly = TestBlueprints.Assemble(spec, await EligibleQuestions());
        Attach(exam, assembly.Questions);
        _context.Exams.Add(exam);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTest), new { id = exam.Id }, await Detail(exam));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TestDetailDto>> UpdateTest(int id, SaveTestRequest request)
    {
        var exam = await LoadExam(id);
        if (exam == null) return NotFound();
        if (exam.Status != "draft") return Conflict("Only draft tests can be edited. Clone this test to change it.");
        var current = TestBlueprints.Deserialize(exam.Blueprint);
        var spec = ToSpec(request) with { Seed = request.Seed ?? current?.Seed ?? NewSeed() };
        var error = ValidateRequest(request, spec);
        if (error != null) return BadRequest(error);
        Apply(exam, request, spec);
        await _context.SaveChangesAsync();
        return Ok(await Detail(exam));
    }

    /// <summary>Replaces the draft's questions with a fresh assembly from its blueprint.</summary>
    [HttpPost("{id:int}/assemble")]
    public async Task<ActionResult<TestDetailDto>> Assemble(int id, AssembleTestRequest request)
    {
        var exam = await LoadExam(id);
        if (exam == null) return NotFound();
        if (exam.Status != "draft") return Conflict("Only draft tests can be reassembled.");
        var spec = TestBlueprints.Deserialize(exam.Blueprint);
        if (spec == null) return BadRequest("This test has no blueprint.");
        if (request.Seed.HasValue) spec = spec with { Seed = request.Seed.Value };
        exam.Blueprint = TestBlueprints.Serialize(spec);
        var assembly = TestBlueprints.Assemble(spec, await EligibleQuestions());
        _context.ExamQuestions.RemoveRange(exam.ExamQuestions);
        exam.ExamQuestions.Clear();
        Attach(exam, assembly.Questions);
        exam.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        var detail = await Detail(exam);
        return Ok(detail);
    }

    /// <summary>Sets the draft's questions and their order.</summary>
    [HttpPut("{id:int}/questions")]
    public async Task<ActionResult<TestDetailDto>> SetQuestions(int id, TestQuestionsRequest request)
    {
        var exam = await LoadExam(id);
        if (exam == null) return NotFound();
        if (exam.Status != "draft") return Conflict("Only draft tests can be edited. Clone this test to change it.");
        if (request.QuestionIds.Distinct().Count() != request.QuestionIds.Count)
            return BadRequest("A question can appear only once in a test.");
        var questions = await _context.Questions.Where(q => request.QuestionIds.Contains(q.Id)).ToDictionaryAsync(q => q.Id);
        var missing = request.QuestionIds.Where(q => !questions.ContainsKey(q)).ToList();
        if (missing.Count > 0) return BadRequest($"Unknown question(s): {string.Join(", ", missing)}.");
        var ineligible = request.QuestionIds.Where(q => !TestBlueprints.IsEligible(questions[q])).ToList();
        if (ineligible.Count > 0)
            return BadRequest($"Only reviewed, published objective questions can be used: {string.Join(", ", ineligible)}.");
        _context.ExamQuestions.RemoveRange(exam.ExamQuestions);
        exam.ExamQuestions.Clear();
        Attach(exam, request.QuestionIds.Select(q => questions[q]));
        exam.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(await Detail(exam));
    }

    [HttpPost("{id:int}/publish")]
    public async Task<ActionResult<TestDetailDto>> Publish(int id)
    {
        var exam = await LoadExam(id);
        if (exam == null) return NotFound();
        if (exam.Status != "draft") return Conflict("Only draft tests can be published.");
        var spec = TestBlueprints.Deserialize(exam.Blueprint);
        if (spec == null) return BadRequest("This test has no blueprint.");
        if (exam.ExamQuestions.Count == 0) return BadRequest("Add questions before publishing.");
        var ineligible = exam.ExamQuestions.Where(eq => !TestBlueprints.IsEligible(eq.Question!)).Select(eq => eq.QuestionId).ToList();
        if (ineligible.Count > 0)
            return BadRequest($"Questions must be reviewed, published and objective: {string.Join(", ", ineligible)}.");
        if (exam.IsStandard)
        {
            var issues = StandardIssues(exam.ExamQuestions.Select(eq => ToQuestionDto(eq.Question!, eq.OrderIndex, eq.PointValue)));
            if (issues.Count > 0) return BadRequest(string.Join(" ", issues));
            if (await _context.Exams.AnyAsync(e => e.IsStandard && e.Status == "active" && e.Id != exam.Id))
                return Conflict("Archive the current standard test before publishing another.");
        }
        exam.Status = "active";
        exam.QuestionCount = exam.ExamQuestions.Count;
        exam.PassScore = (int)Math.Ceiling(exam.ExamQuestions.Sum(eq => eq.PointValue) * spec.PassPercent / 100m);
        exam.PublishedAt = exam.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(await Detail(exam));
    }

    [HttpPost("{id:int}/archive")]
    public async Task<ActionResult<TestDetailDto>> Archive(int id)
    {
        var exam = await LoadExam(id);
        if (exam == null) return NotFound();
        if (exam.Status == "archived") return Conflict("The test is already archived.");
        exam.Status = "archived";
        exam.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(await Detail(exam));
    }

    [HttpPost("{id:int}/clone")]
    public async Task<ActionResult<TestDetailDto>> Clone(int id)
    {
        var source = await LoadExam(id);
        if (source == null) return NotFound();
        var clone = new Exam
        {
            Title = Truncate($"Copy of {source.Title}", 255),
            Description = source.Description,
            DurationMinutes = source.DurationMinutes,
            Blueprint = source.Blueprint,
            IsStandard = source.IsStandard && source.Blueprint != null,
            Status = "draft",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Attach(clone, source.ExamQuestions.OrderBy(eq => eq.OrderIndex).Select(eq => eq.Question!), source.ExamQuestions);
        _context.Exams.Add(clone);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTest), new { id = clone.Id }, await Detail(clone));
    }

    /// <summary>CTT report for a test form (thesis §2.12, Appendix H), recomputed from stored responses.</summary>
    [HttpGet("{id:int}/item-analysis")]
    public async Task<ActionResult<ItemAnalysisReport>> ItemAnalysis(int id, [FromServices] ItemAnalysisService analysis)
    {
        try
        {
            var report = await analysis.AnalyzeAsync(id);
            return report == null ? NotFound() : Ok(report);
        }
        catch (InvalidOperationException error) { return BadRequest(error.Message); }
        catch (HttpRequestException) { return StatusCode(StatusCodes.Status503ServiceUnavailable, "The analysis service is unavailable."); }
    }

    /// <summary>CSV exports: items (Appendix H table), reliability, or the scored response matrix.</summary>
    [HttpGet("{id:int}/item-analysis/export")]
    public async Task<IActionResult> ExportItemAnalysis(int id, [FromQuery] string kind, [FromServices] ItemAnalysisService analysis)
    {
        if (kind == "responses")
        {
            var matrix = await analysis.LoadMatrixAsync(id);
            return matrix == null ? NotFound() : Csv(ItemAnalysisService.ResponsesCsv(matrix), $"test-{id}-responses.csv");
        }
        if (kind is not ("items" or "reliability")) return BadRequest("Choose items, reliability, or responses.");
        try
        {
            var report = await analysis.AnalyzeAsync(id);
            if (report == null) return NotFound();
            return kind == "items"
                ? Csv(ItemAnalysisService.ItemsCsv(report), $"test-{id}-items.csv")
                : Csv(ItemAnalysisService.ReliabilityCsv(report), $"test-{id}-reliability.csv");
        }
        catch (InvalidOperationException error) { return BadRequest(error.Message); }
        catch (HttpRequestException) { return StatusCode(StatusCodes.Status503ServiceUnavailable, "The analysis service is unavailable."); }
    }

    private FileContentResult Csv(string content, string name) =>
        File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(content)).ToArray(), "text/csv", name);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTest(int id)
    {
        var exam = await _context.Exams.FindAsync(id);
        if (exam == null) return NotFound();
        if (exam.Status != "draft" || await _context.ExamResults.AnyAsync(s => s.ExamId == id))
            return Conflict("Only unused drafts can be deleted. Archive published tests instead.");
        _context.Exams.Remove(exam);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Phase 0 created one hidden exam per start-standard call; they are not managed forms.
    private const string LegacyDynamicDescription = "Dynamically generated standard assessment.";

    private Task<Exam?> LoadExam(int id) => _context.Exams
        .Include(e => e.ExamQuestions).ThenInclude(eq => eq.Question)
        .SingleOrDefaultAsync(e => e.Id == id);

    private async Task<List<Question>> EligibleQuestions() =>
        await TestBlueprints.Eligible(_context.Questions).ToListAsync();

    private static TestBlueprintSpec ToSpec(SaveTestRequest request) => new()
    {
        GroupQuotas = request.GroupQuotas.Where(q => q.Value > 0).ToDictionary(q => q.Key, q => q.Value),
        LevelMix = request.LevelMix.Where(l => l.Value > 0).ToDictionary(l => l.Key, l => l.Value),
        Seed = request.Seed ?? NewSeed(),
        PassPercent = request.PassPercent
    };

    private static long NewSeed() => RandomNumberGenerator.GetInt32(1, int.MaxValue);

    private static string? ValidateRequest(SaveTestRequest request, TestBlueprintSpec spec)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 255)
            return "Name is required and must be at most 255 characters.";
        if ((request.Description?.Length ?? 0) > 1000) return "Description must be at most 1000 characters.";
        if (request.DurationMinutes is < 1 or > 600) return "Duration must be between 1 and 600 minutes.";
        return TestBlueprints.Validate(spec);
    }

    private static void Apply(Exam exam, SaveTestRequest request, TestBlueprintSpec spec)
    {
        exam.Title = request.Name.Trim();
        exam.Description = request.Description?.Trim() ?? string.Empty;
        exam.DurationMinutes = request.DurationMinutes;
        exam.IsStandard = request.IsStandard;
        exam.Blueprint = TestBlueprints.Serialize(spec);
        exam.UpdatedAt = DateTime.UtcNow;
    }

    private static void Attach(Exam exam, IEnumerable<Question> questions, IEnumerable<ExamQuestion>? points = null)
    {
        var pointValues = points?.ToDictionary(eq => eq.QuestionId, eq => eq.PointValue);
        var order = 0;
        foreach (var question in questions)
            exam.ExamQuestions.Add(new ExamQuestion
            {
                Question = question,
                QuestionId = question.Id,
                OrderIndex = ++order,
                PointValue = pointValues?.GetValueOrDefault(question.Id, 1m) ?? 1m
            });
        exam.QuestionCount = exam.ExamQuestions.Count;
    }

    private async Task<TestDetailDto> Detail(Exam exam)
    {
        var spec = TestBlueprints.Deserialize(exam.Blueprint);
        var questions = exam.ExamQuestions.OrderBy(eq => eq.OrderIndex)
            .Select(eq => ToQuestionDto(eq.Question!, eq.OrderIndex, eq.PointValue)).ToList();
        var detail = Fill(new TestDetailDto(), exam);
        detail.Questions = questions;
        detail.Coverage = Coverage(spec, questions);
        detail.StandardIssues = exam.IsStandard ? StandardIssues(questions) : [];
        detail.HasSessions = exam.Id != 0 && await _context.ExamResults.AnyAsync(s => s.ExamId == exam.Id);
        return detail;
    }

    private static T Fill<T>(T dto, Exam exam) where T : TestBlueprintDto
    {
        dto.Id = exam.Id.ToString();
        dto.Name = exam.Title;
        dto.Description = exam.Description;
        dto.DurationMinutes = exam.DurationMinutes;
        dto.PassingScore = exam.PassScore;
        dto.QuestionCount = exam.ExamQuestions.Count > 0 ? exam.ExamQuestions.Count : exam.QuestionCount;
        dto.Status = exam.Status;
        dto.IsStandard = exam.IsStandard;
        dto.Blueprint = TestBlueprints.Deserialize(exam.Blueprint);
        dto.CreatedAt = exam.CreatedAt;
        dto.UpdatedAt = exam.UpdatedAt;
        dto.PublishedAt = exam.PublishedAt;
        return dto;
    }

    private static TestQuestionDto ToQuestionDto(Question q, int order, decimal points) => new(
        q.Id, order, points, QuestionPresentation.Title(q), q.Type, q.GroupCode, q.TargetLevel, q.DifficultyLevel,
        q.Status.ToString().ToLowerInvariant(), TestBlueprints.IsEligible(q));

    private static List<GroupCoverageDto> Coverage(TestBlueprintSpec? spec, IReadOnlyCollection<TestQuestionDto> questions) =>
        CompetencyCatalog.Groups.Select(g =>
        {
            var inGroup = questions.Where(q => q.GroupCode == g.Code).ToList();
            return new GroupCoverageDto(g.Code, g.Name, spec?.GroupQuotas.GetValueOrDefault(g.Code) ?? 0, inGroup.Count,
                inGroup.Count(q => q.TargetLevel == 1), inGroup.Count(q => q.TargetLevel == 2), inGroup.Count(q => q.TargetLevel == 3));
        }).ToList();

    private static List<string> StandardIssues(IEnumerable<TestQuestionDto> questions) =>
        TestBlueprints.StandardIssues(questions.Where(q => q.GroupCode != null)
            .GroupBy(q => q.GroupCode!).ToDictionary(g => g.Key, g => g.Count()));

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..(length - 1)] + "…";
}
