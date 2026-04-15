using AiCompetency.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiCompetency.Api.Controllers;

[ApiController]
[Route("api/question-generation")]
public class QuestionGenerationController : ControllerBase
{
    private readonly QuestionGeneratorService _service;

    public QuestionGenerationController(QuestionGeneratorService service)
    {
        _service = service;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerationRequest request)
    {
        try
        {
            var questions = await _service.GenerateQuestionsAsync(request);
            return Ok(questions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("generate-from-plan")]
    public async Task<IActionResult> GenerateFromPlan([FromBody] GenerationPlanRequest request)
    {
        try
        {
            var questions = await _service.GenerateFromPlanAsync(request);
            return Ok(questions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // New endpoint for agentic generation
    [HttpPost("generate-agentic")]
    public async Task<IActionResult> GenerateAgentic([FromBody] AgenticRequest request)
    {
        try
        {
            var result = await _service.GenerateAgenticAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
