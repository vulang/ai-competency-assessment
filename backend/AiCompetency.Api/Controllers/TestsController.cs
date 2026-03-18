using AiCompetency.Api.Data;
using AiCompetency.Api.DTOs;
using AiCompetency.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiCompetency.Api.Controllers;

[ApiController]
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
        var exams = await _context.Exams.ToListAsync();

        var tests = exams.Select(e => new TestBlueprintDto
        {
            // Frontend expects string ID starting with 'tb' + id, or similar. 
            // We'll map the int ID to string.
            Id = e.Id.ToString(),
            Name = e.Title,
            Description = e.Description,
            DurationMinutes = e.DurationMinutes,
            PassingScore = e.PassScore,
            QuestionCount = e.QuestionCount,
            Status = e.Status,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        });

        return Ok(tests);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TestBlueprintDto>> GetTest(int id)
    {
        var exam = await _context.Exams.FindAsync(id);

        if (exam == null)
        {
            return NotFound();
        }

        var test = new TestBlueprintDto
        {
            Id = exam.Id.ToString(),
            Name = exam.Title,
            Description = exam.Description,
            DurationMinutes = exam.DurationMinutes,
            PassingScore = exam.PassScore,
            QuestionCount = exam.QuestionCount,
            Status = exam.Status,
            CreatedAt = exam.CreatedAt,
            UpdatedAt = exam.UpdatedAt
        };

        return Ok(test);
    }

    [HttpPost]
    public async Task<ActionResult<TestBlueprintDto>> CreateTest(CreateTestBlueprintDto createDto)
    {
        var exam = new Exam
        {
            Title = createDto.Name,
            Description = createDto.Description,
            DurationMinutes = createDto.DurationMinutes,
            PassScore = createDto.PassingScore,
            QuestionCount = createDto.QuestionCount,
            Status = createDto.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Exams.Add(exam);
        await _context.SaveChangesAsync();

        var testDto = new TestBlueprintDto
        {
            Id = exam.Id.ToString(),
            Name = exam.Title,
            Description = exam.Description,
            DurationMinutes = exam.DurationMinutes,
            PassingScore = exam.PassScore,
            QuestionCount = exam.QuestionCount,
            Status = exam.Status,
            CreatedAt = exam.CreatedAt,
            UpdatedAt = exam.UpdatedAt
        };

        return CreatedAtAction(nameof(GetTest), new { id = exam.Id }, testDto);
    }
}
