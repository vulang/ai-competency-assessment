using System.Text.Json;
using System.Text.Json.Serialization;
using AiCompetency.Api.Data;
using AiCompetency.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiCompetency.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public QuestionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetQuestions()
    {
        var questions = await _context.Questions.ToListAsync();
        var dtos = questions.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuestion(int id)
    {
        var question = await _context.Questions.FindAsync(id);
        if (question == null)
            return NotFound();

        return Ok(MapToDto(question));
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuestion([FromBody] QuestionDto dto)
    {
        var question = new Question
        {
            Type = dto.Type,
            Content = dto.Prompt,
            DifficultyLevel = dto.Difficulty,
            Status = Enum.TryParse<QuestionStatus>(dto.Status, true, out var status) ? status : QuestionStatus.Draft,
            Metadata = JsonSerializer.SerializeToDocument(new
            {
                topic = dto.Title,
                group = dto.CompetencyGroupId,
                level = dto.CompetencyLevelId,
                tags = dto.Tags
            })
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, MapToDto(question));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuestion(int id, [FromBody] QuestionDto dto)
    {
        var question = await _context.Questions.FindAsync(id);
        if (question == null)
            return NotFound();

        question.Type = dto.Type;
        question.Content = dto.Prompt;
        question.DifficultyLevel = dto.Difficulty;
        if (Enum.TryParse<QuestionStatus>(dto.Status, true, out var status))
            question.Status = status;

        // Preserve existing metadata, updating only matched values if necessary, or just overwrite relevant ones
        var existingMetadata = question.Metadata.RootElement.Clone();
        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(existingMetadata.GetRawText()) ?? new Dictionary<string, object>();
        dict["topic"] = dto.Title;
        dict["group"] = dto.CompetencyGroupId;
        dict["level"] = dto.CompetencyLevelId;
        dict["tags"] = dto.Tags;
        question.Metadata = JsonSerializer.SerializeToDocument(dict);

        await _context.SaveChangesAsync();

        return Ok(MapToDto(question));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var question = await _context.Questions.FindAsync(id);
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

        var entities = questions.Select(q => new Question
        {
            Content = !string.IsNullOrEmpty(q.Stem) ? q.Stem : (q.Question ?? string.Empty),
            Type = !string.IsNullOrEmpty(q.Type) ? q.Type : "mcq_single",
            DifficultyLevel = TransformLevel(q.Level),
            Status = QuestionStatus.Draft,
            Metadata = JsonSerializer.SerializeToDocument(new
            {
                options = q.Options ?? new List<string>(),
                answer = q.Answer ?? string.Empty,
                explanation = !string.IsNullOrEmpty(q.RationaleHidden) ? q.RationaleHidden : (q.Explanation ?? string.Empty),
                group = q.Group,
                topic = q.Topic,
                level = q.Level
            })
        }).ToList();

        _context.Questions.AddRange(entities);
        await _context.SaveChangesAsync();

        return Ok(new { count = entities.Count });
    }

    private int TransformLevel(string? level)
    {
        return level switch
        {
            "NenTang" => 1,
            "ApDung" => 2,
            "KienTao" => 3,
            _ => 1
        };
    }

    private QuestionDto MapToDto(Question q)
    {
        var root = q.Metadata.RootElement;
        return new QuestionDto
        {
            Id = q.Id.ToString(),
            Title = root.TryGetProperty("topic", out var topicProp) && topicProp.ValueKind == JsonValueKind.String ? topicProp.GetString() ?? "Untitled" : "Untitled",
            Prompt = q.Content,
            Type = q.Type,
            CompetencyGroupId = root.TryGetProperty("group", out var groupProp) && groupProp.ValueKind == JsonValueKind.String ? groupProp.GetString() ?? string.Empty : string.Empty,
            CompetencyLevelId = root.TryGetProperty("level", out var levelProp) && levelProp.ValueKind == JsonValueKind.String ? levelProp.GetString() ?? string.Empty : string.Empty,
            Difficulty = q.DifficultyLevel ?? 1,
            Tags = root.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array ? tagsProp.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : new List<string>(),
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
    public int Difficulty { get; set; } = 1;
    public List<string> Tags { get; set; } = new();
    public string Status { get; set; } = "draft";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class BatchQuestionInput
{
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
}
