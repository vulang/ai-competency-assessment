using System.ComponentModel.DataAnnotations;
using AiCompetency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;
using System.Text.Json;
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

    public QuestionsController(ApplicationDbContext context, QuestionClassificationService classification)
    {
        _context = context;
        _classification = classification;
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
            ["group"] = classification.Group,
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

        var question = new Question
        {
            SkillId = classification.SkillId,
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
        question.SkillId = classification.SkillId;
        question.Type = QuestionClassificationService.NormalizeType(dto.Type);
        question.Content = dto.Prompt;
        question.DifficultyLevel = dto.Difficulty;
        if (Enum.TryParse<QuestionStatus>(dto.Status, true, out var status))
            question.Status = status;

        // Preserve existing metadata, updating only matched values if necessary, or just overwrite relevant ones
        var existingMetadata = question.Metadata.RootElement.Clone();
        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(existingMetadata.GetRawText()) ?? new Dictionary<string, object>();
        dict["topic"] = dto.Title;
        dict["group"] = classification.Group;
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

        await _context.SaveChangesAsync();

        return Ok(MapToDto(question));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var question = await _context.Questions.Include(q => q.Skill).ThenInclude(s => s!.Domain).SingleOrDefaultAsync(q => q.Id == id);
        if (question == null)
            return NotFound();

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
                Content = !string.IsNullOrEmpty(q.Stem) ? q.Stem : (q.Question ?? string.Empty),
                Type = QuestionClassificationService.NormalizeType(!string.IsNullOrEmpty(q.Type) ? q.Type : "mcq_single"),
                DifficultyLevel = q.Difficulty,
                Status = QuestionStatus.Draft,
                Metadata = JsonSerializer.SerializeToDocument(new
                {
                    options = q.Options ?? new List<string>(),
                    answer = q.Answer ?? string.Empty,
                    explanation = !string.IsNullOrEmpty(q.RationaleHidden) ? q.RationaleHidden : (q.Explanation ?? string.Empty),
                    group = classification.Group,
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
            Title = root.TryGetProperty("topic", out var topicProp) && topicProp.ValueKind == JsonValueKind.String ? topicProp.GetString() ?? "Untitled" : "Untitled",
            Prompt = q.Content,
            Type = q.Type,
            CompetencyGroupId = q.Skill?.Domain.Name ?? (root.TryGetProperty("group", out var groupProp) && groupProp.ValueKind == JsonValueKind.String ? groupProp.GetString() ?? string.Empty : string.Empty),
            CompetencyLevelId = root.TryGetProperty("level", out var levelProp) && levelProp.ValueKind == JsonValueKind.String ? CompetencyCatalog.LevelName(levelProp.GetString()) ?? levelProp.GetString() ?? string.Empty : string.Empty,
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
            UpdatedAt = DateTime.UtcNow // Assuming no updated date on backend model for now
        };
    }
}

public class QuestionDto
{
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
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

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
