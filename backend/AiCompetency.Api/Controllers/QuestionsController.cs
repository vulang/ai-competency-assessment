using System.ComponentModel.DataAnnotations;
using AiCompetency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AiCompetency.Api.Data;
using AiCompetency.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiCompetency.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Admin")]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly QuestionClassificationService _classification;
    private readonly IScoringService _scoring;

    public QuestionsController(ApplicationDbContext context, QuestionClassificationService classification, IScoringService scoring)
    {
        _context = context;
        _classification = classification;
        _scoring = scoring;
    }

    /// <summary>Bank coverage by group × level × type, with counts per review status.</summary>
    [HttpGet("coverage")]
    public async Task<IActionResult> GetCoverage()
    {
        var questions = await _context.Questions
            .Select(q => new { q.GroupCode, q.TargetLevel, q.Type, q.Status, Reviewed = q.ReviewedAt != null })
            .ToListAsync();
        var classified = questions.Where(q => q.GroupCode != null && q.TargetLevel != null).ToList();
        var cells = CompetencyCatalog.Groups.SelectMany(g => Enumerable.Range(1, 3).Select(level =>
        {
            var inCell = classified.Where(q => q.GroupCode == g.Code && q.TargetLevel == level).ToList();
            return new
            {
                GroupCode = g.Code,
                GroupName = g.Name,
                Level = level,
                Total = inCell.Count,
                Draft = inCell.Count(q => q.Status == QuestionStatus.Draft),
                Reviewed = inCell.Count(q => q.Status == QuestionStatus.Reviewed),
                Published = inCell.Count(q => q.Status == QuestionStatus.Published),
                Archived = inCell.Count(q => q.Status == QuestionStatus.Archived),
                Eligible = inCell.Count(q => q.Status == QuestionStatus.Published && q.Reviewed && ScoringService.IsObjective(q.Type)),
                ByType = inCell.Where(q => q.Status != QuestionStatus.Archived).GroupBy(q => q.Type)
                    .ToDictionary(t => t.Key, t => t.Count())
            };
        })).ToList();
        return Ok(new
        {
            StandardMinPerGroup = TestBlueprints.StandardMinPerGroup,
            StandardMaxPerGroup = TestBlueprints.StandardMaxPerGroup,
            Unclassified = questions.Count - classified.Count,
            Cells = cells
        });
    }

    /// <summary>Records expert review. Only reviewed questions can be published or used in test forms.</summary>
    [HttpPost("{id}/review")]
    public async Task<IActionResult> ReviewQuestion(int id, [FromBody] ReviewQuestionRequest request)
    {
        var question = await _context.Questions.Include(q => q.Skill).ThenInclude(s => s!.Domain).SingleOrDefaultAsync(q => q.Id == id);
        if (question == null)
            return NotFound();
        if (question.Status == QuestionStatus.Archived)
            return Conflict("Archived questions cannot be reviewed.");
        if (question.GroupCode == null || question.TargetLevel == null)
            return BadRequest("Classify the question's group and level before review.");
        if (ScoringService.IsObjective(question.Type) && _scoring.Options(question) == null)
            return BadRequest("The answer key does not match the question's options.");
        if ((request.Note?.Length ?? 0) > 2000)
            return BadRequest("Review notes must be at most 2000 characters.");

        question.ReviewedBy = CurrentUser.GetDisplayName(User);
        question.ReviewedAt = DateTimeOffset.UtcNow;
        question.ReviewNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (question.Status == QuestionStatus.Draft)
            question.Status = QuestionStatus.Reviewed;
        await _context.SaveChangesAsync();
        return Ok(MapToDto(question));
    }

    [HttpGet]
    public async Task<IActionResult> GetQuestions()
    {
        var questions = await _context.Questions.Include(q => q.Skill).ThenInclude(s => s!.Domain).ToListAsync();
        var dtos = questions.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuestion(int id)
    {
        var question = await _context.Questions.Include(q => q.Skill).ThenInclude(s => s!.Domain).SingleOrDefaultAsync(q => q.Id == id);
        if (question == null)
            return NotFound();

        return Ok(MapToDto(question));
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuestion([FromBody] QuestionDto dto)
    {
        QuestionClassification classification;
        try { classification = await _classification.ResolveAsync(dto.CompetencyGroupId, dto.CompetencyLevelId, dto.SkillId, dto.Difficulty); }
        catch (ValidationException error) { ModelState.AddModelError("classification", error.Message); return ValidationProblem(statusCode: 400, modelStateDictionary: ModelState); }
        var dict = new Dictionary<string, object>
        {
            ["topic"] = dto.Title,
            ["group"] = classification.GroupCode,
            ["level"] = classification.Level,
            ["tags"] = dto.Tags,
            ["options"] = dto.Options,
            ["answer"] = ParseAnswer(dto.Answer),
            ["context"] = dto.Context,
            ["responseStructure"] = dto.ResponseStructure,
            ["rubric"] = dto.Rubric,
            ["judgePromptHints"] = dto.JudgePromptHints,
            ["behaviorsAddressed"] = dto.BehaviorsAddressed
        };

        if (Enum.TryParse<QuestionStatus>(dto.Status, true, out var requested) &&
            requested is QuestionStatus.Reviewed or QuestionStatus.Published)
            return Conflict("New questions start as drafts. Review a question before publishing it.");

        var question = new Question
        {
            SkillId = classification.SkillId,
            GroupCode = classification.GroupCode,
            TargetLevel = classification.TargetLevel,
            Type = QuestionClassificationService.NormalizeType(dto.Type),
            Content = dto.Prompt,
            DifficultyLevel = dto.Difficulty,
            Status = Enum.TryParse<QuestionStatus>(dto.Status, true, out var status) ? status : QuestionStatus.Draft,
            Metadata = JsonSerializer.SerializeToDocument(dict),
            RubricScoring = dto.Rubric != null && dto.Rubric.Any() ? string.Join("\n", dto.Rubric) : null
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, MapToDto(question));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuestion(int id, [FromBody] QuestionDto dto)
    {
        var question = await _context.Questions.Include(q => q.Skill).ThenInclude(s => s!.Domain).SingleOrDefaultAsync(q => q.Id == id);
        if (question == null)
            return NotFound();

        QuestionClassification classification;
        try { classification = await _classification.ResolveAsync(dto.CompetencyGroupId, dto.CompetencyLevelId, dto.SkillId, dto.Difficulty); }
        catch (ValidationException error) { ModelState.AddModelError("classification", error.Message); return ValidationProblem(statusCode: 400, modelStateDictionary: ModelState); }
        var before = AssessedContent(question);
        question.SkillId = classification.SkillId;
        question.GroupCode = classification.GroupCode;
        question.TargetLevel = classification.TargetLevel;
        question.Type = QuestionClassificationService.NormalizeType(dto.Type);
        question.Content = dto.Prompt;
        question.DifficultyLevel = dto.Difficulty;

        // Preserve existing metadata, updating only matched values if necessary, or just overwrite relevant ones
        var existingMetadata = question.Metadata.RootElement.Clone();
        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(existingMetadata.GetRawText()) ?? new Dictionary<string, object>();
        dict["topic"] = dto.Title;
        dict["group"] = classification.GroupCode;
        dict["level"] = classification.Level;
        dict["tags"] = dto.Tags;
        dict["options"] = dto.Options;
        dict["answer"] = ParseAnswer(dto.Answer);
        dict["context"] = dto.Context;
        dict["responseStructure"] = dto.ResponseStructure;
        dict["rubric"] = dto.Rubric;
        dict["judgePromptHints"] = dto.JudgePromptHints;
        dict["behaviorsAddressed"] = dto.BehaviorsAddressed;
        question.Metadata = JsonSerializer.SerializeToDocument(dict);
        question.RubricScoring = dto.Rubric != null && dto.Rubric.Any() ? string.Join("\n", dto.Rubric) : null;

        // Delivered items must stay fixed so stored responses and CTT remain valid.
        var after = AssessedContent(question);
        var contentChanged = before.Where((node, index) => !JsonNode.DeepEquals(node, after[index])).Any();
        if (contentChanged && await IsDelivered(id))
            return Conflict("This question has been delivered in a test. Archive it and create a new question instead.");
        if (contentChanged && question.ReviewedAt != null)
            ClearReview(question);

        if (Enum.TryParse<QuestionStatus>(dto.Status, true, out var status) && status != question.Status)
        {
            if (status is QuestionStatus.Reviewed or QuestionStatus.Published && question.ReviewedAt == null)
                return Conflict(contentChanged
                    ? "The question changed after review. Review it again before publishing."
                    : "Review the question before publishing it.");
            if (status == QuestionStatus.Draft)
                ClearReview(question);
            question.Status = status;
        }

        await _context.SaveChangesAsync();

        return Ok(MapToDto(question));
    }

    private static void ClearReview(Question question)
    {
        question.ReviewedBy = null;
        question.ReviewedAt = null;
        question.ReviewNote = null;
        if (question.Status is QuestionStatus.Reviewed or QuestionStatus.Published)
            question.Status = QuestionStatus.Draft;
    }

    // Fields that define what the item measures and how it is keyed.
    private static List<JsonNode?> AssessedContent(Question q)
    {
        var root = q.Metadata.RootElement;
        JsonNode? Field(string name) => root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value)
            ? JsonNode.Parse(value.GetRawText()) : null;
        return [JsonValue.Create(q.Type), JsonValue.Create(q.Content), JsonValue.Create(q.GroupCode),
            JsonValue.Create(q.TargetLevel), Field("options"), Field("answer"), Field("context")];
    }

    private async Task<bool> IsDelivered(int id) =>
        await _context.Responses.AnyAsync(r => r.QuestionId == id) ||
        await _context.ExamQuestions.AnyAsync(eq => eq.QuestionId == id && eq.Exam!.Status != "draft");

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var question = await _context.Questions.Include(q => q.Skill).ThenInclude(s => s!.Domain).SingleOrDefaultAsync(q => q.Id == id);
        if (question == null)
            return NotFound();
        if (await IsDelivered(id))
            return Conflict("This question has been delivered in a test. Archive it instead of deleting it.");

        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("batch")]
    public async Task<IActionResult> SaveBatch([FromBody] List<BatchQuestionInput> questions)
    {
        if (questions == null || !questions.Any())
        {
            return BadRequest("No questions provided.");
        }

        var entities = new List<Question>();
        for (var index = 0; index < questions.Count; index++)
        {
            var q = questions[index];
            QuestionClassification classification;
            try { classification = await _classification.ResolveAsync(q.Group, q.Level, q.SkillId, q.Difficulty); }
            catch (ValidationException error) { ModelState.AddModelError($"questions[{index}]", error.Message); return ValidationProblem(statusCode: 400, modelStateDictionary: ModelState); }
            entities.Add(new Question
            {
                SkillId = classification.SkillId,
            GroupCode = classification.GroupCode,
            TargetLevel = classification.TargetLevel,
                Content = !string.IsNullOrEmpty(q.Stem) ? q.Stem : (q.Question ?? string.Empty),
                Type = QuestionClassificationService.NormalizeType(!string.IsNullOrEmpty(q.Type) ? q.Type : "mcq_single"),
                DifficultyLevel = q.Difficulty,
                Status = QuestionStatus.Draft,
                Metadata = JsonSerializer.SerializeToDocument(new
                {
                    options = q.Options ?? new List<string>(),
                    answer = q.Answer ?? string.Empty,
                    explanation = !string.IsNullOrEmpty(q.RationaleHidden) ? q.RationaleHidden : (q.Explanation ?? string.Empty),
                    group = classification.GroupCode,
                    topic = q.Title ?? q.Topic,
                    level = classification.Level,
                    context = q.Context ?? string.Empty,
                    responseStructure = q.ResponseStructure ?? new List<string>(),
                    rubric = q.Rubric ?? new List<string>(),
                    judgePromptHints = q.JudgePromptHints ?? string.Empty,
                    behaviorsAddressed = q.BehaviorsAddressed ?? new List<string>()
                }),
                RubricScoring = q.Rubric != null && q.Rubric.Any() ? string.Join("\n", q.Rubric) : null
            });
        }

        _context.Questions.AddRange(entities);
        await _context.SaveChangesAsync();

        return Ok(new { count = entities.Count });
    }

    private static object ParseAnswer(string answer)
    {
        if (answer.TrimStart().StartsWith('['))
        {
            try { return JsonSerializer.Deserialize<string[]>(answer) ?? []; }
            catch (JsonException) { }
        }
        return answer;
    }

    private QuestionDto MapToDto(Question q)
    {
        var root = q.Metadata.RootElement;
        return new QuestionDto
        {
            Id = q.Id.ToString(),
            SkillId = q.SkillId,
            GroupCode = q.GroupCode,
            TargetLevel = q.TargetLevel,
            Title = root.TryGetProperty("topic", out var topicProp) && topicProp.ValueKind == JsonValueKind.String ? topicProp.GetString() ?? "Untitled" : "Untitled",
            Prompt = q.Content,
            Type = q.Type,
            CompetencyGroupId = q.GroupCode ?? q.Skill?.Domain.Code ?? (root.TryGetProperty("group", out var groupProp) && groupProp.ValueKind == JsonValueKind.String ? CompetencyCatalog.GroupCode(groupProp.GetString()) ?? string.Empty : string.Empty),
            CompetencyLevelId = q.TargetLevel switch { 1 => "Basic", 2 => "Intermediate", 3 => "Advanced", _ => root.TryGetProperty("level", out var levelProp) && levelProp.ValueKind == JsonValueKind.String ? CompetencyCatalog.LevelName(levelProp.GetString()) ?? string.Empty : string.Empty },
            Difficulty = q.DifficultyLevel,
            Tags = root.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array ? tagsProp.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : new List<string>(),
            Options = root.TryGetProperty("options", out var optionsProp) && optionsProp.ValueKind == JsonValueKind.Array ? optionsProp.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : new List<string>(),
            Answer = root.TryGetProperty("answer", out var answerProp) ? (answerProp.ValueKind == JsonValueKind.String ? answerProp.GetString() ?? string.Empty : answerProp.GetRawText()) : string.Empty,
            Context = root.TryGetProperty("context", out var contextProp) && contextProp.ValueKind == JsonValueKind.String ? contextProp.GetString() ?? string.Empty : string.Empty,
            ResponseStructure = root.TryGetProperty("responseStructure", out var rsProp) && rsProp.ValueKind == JsonValueKind.Array ? rsProp.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : new List<string>(),
            Rubric = root.TryGetProperty("rubric", out var rubricProp) && rubricProp.ValueKind == JsonValueKind.Array ? rubricProp.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : new List<string>(),
            JudgePromptHints = root.TryGetProperty("judgePromptHints", out var jpProp) && jpProp.ValueKind == JsonValueKind.String ? jpProp.GetString() ?? string.Empty : string.Empty,
            BehaviorsAddressed = root.TryGetProperty("behaviorsAddressed", out var bProp) && bProp.ValueKind == JsonValueKind.Array ? bProp.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : new List<string>(),
            Status = q.Status.ToString().ToLower(),
            ReviewedBy = q.ReviewedBy,
            ReviewedAt = q.ReviewedAt,
            ReviewNote = q.ReviewNote,
            Eligible = TestBlueprints.IsEligible(q),
            UpdatedAt = DateTime.UtcNow // Assuming no updated date on backend model for now
        };
    }
}

public class QuestionDto
{
    public string? GroupCode { get; set; }
    public int? TargetLevel { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string CompetencyGroupId { get; set; } = string.Empty;
    public string CompetencyLevelId { get; set; } = string.Empty;
    [Range(1, 5)]
    public int? Difficulty { get; set; }
    public int? SkillId { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> Options { get; set; } = new();
    public string Answer { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public List<string> ResponseStructure { get; set; } = new();
    public List<string> Rubric { get; set; } = new();
    public string JudgePromptHints { get; set; } = string.Empty;
    public List<string> BehaviorsAddressed { get; set; } = new();
    public string Status { get; set; } = "draft";
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    /// <summary>Reviewed, published and objective: usable in a test form.</summary>
    public bool Eligible { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed record ReviewQuestionRequest(string? Note);

public class BatchQuestionInput
{
    [JsonPropertyName("difficulty"), Range(1, 5)]
    public int? Difficulty { get; set; }

    [JsonPropertyName("skill_id")]
    public int? SkillId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("level")]
    public string? Level { get; set; }
    
    [JsonPropertyName("stem")]
    public string? Stem { get; set; }
    
    [JsonPropertyName("question")]
    public string? Question { get; set; }
    
    [JsonPropertyName("options")]
    public List<string>? Options { get; set; }
    
    [JsonPropertyName("answer")]
    public object? Answer { get; set; } 
    
    [JsonPropertyName("rationale_hidden")]
    public string? RationaleHidden { get; set; }
    
    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }
    
    [JsonPropertyName("group")]
    public string? Group { get; set; }
    
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("context")]
    public string? Context { get; set; }
    
    [JsonPropertyName("response_structure")]
    public List<string>? ResponseStructure { get; set; }
    
    [JsonPropertyName("rubric")]
    public List<string>? Rubric { get; set; }
    
    [JsonPropertyName("judge_prompt_hints")]
    public string? JudgePromptHints { get; set; }
    
    [JsonPropertyName("behaviors_addressed")]
    public List<string>? BehaviorsAddressed { get; set; }
}
