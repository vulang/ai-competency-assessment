using System.Diagnostics;
using System.Text.Json;
using System.Collections.Generic;
using System.Net.Http.Json;

namespace AiCompetency.Api.Services;

public class QuestionGeneratorService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<QuestionGeneratorService> _logger;
    private readonly string _serviceUrl;

    public QuestionGeneratorService(HttpClient httpClient, IConfiguration configuration, ILogger<QuestionGeneratorService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        // In local development with Docker Compose, this should valid
        // But if running backend locally and service in docker, use localhost:5001
        // We'll read from config or default to localhost
        _serviceUrl = configuration.GetValue<string>("QuestionGeneratorUrl") ?? "http://localhost:5001";
    }

    public async Task<List<GeneratedQuestion>> GenerateQuestionsAsync(GenerationRequest request)
    {
        try 
        {
            _logger.LogInformation($"Sending generation request to {_serviceUrl}/generate");
            
            var response = await _httpClient.PostAsJsonAsync($"{_serviceUrl}/generate", new 
            {
                topic = request.Topic ?? "general",
                count = request.Count,
                difficulty = request.Difficulty
            });

            response.EnsureSuccessStatusCode();
            
            var questions = await response.Content.ReadFromJsonAsync<List<GeneratedQuestion>>();
            return questions ?? new List<GeneratedQuestion>();
        }
        catch (Exception ex)
        {
             _logger.LogError($"Error calling question generator service: {ex.Message}");
             throw;
        }
    }

    public async Task<List<Dictionary<string, object>>> GenerateFromPlanAsync(GenerationPlanRequest request)
    {
        try
        {
            _logger.LogInformation($"Sending generation request to {_serviceUrl}/generate-from-plan");

            var response = await _httpClient.PostAsJsonAsync($"{_serviceUrl}/generate-from-plan", request);

            response.EnsureSuccessStatusCode();

            var questions = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
            return questions ?? new List<Dictionary<string, object>>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error calling question generator service: {ex.Message}");
            throw;
        }
    }

    // New method for agentic generation
    public async Task<object> GenerateAgenticAsync(AgenticRequest request)
    {
        try
        {
            _logger.LogInformation($"Sending generation request to {_serviceUrl}/generate-agentic");
            var response = await _httpClient.PostAsJsonAsync($"{_serviceUrl}/generate-agentic", request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<object>();
            return result ?? new {};
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error calling question generator service (agentic): {ex.Message}");
            throw;
        }
    }
}

public record GenerationRequest(string Topic, int Count, string Difficulty);

// Adjust this record to match the actual output of the python script
public record GeneratedQuestion
{
    public string Question { get; set; } = "";
    public List<string> Options { get; set; } = new();
    public string Answer { get; set; } = "";
    public string Explanation { get; set; } = "";
}

public class MixItem
{
    public string Group { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<int> Difficulty { get; set; } = new();
    public string Topic { get; set; } = "";
    public int Count { get; set; } = 1;
}

public class GenerationPlanRequest
{
    public int Total { get; set; } = 100;
    public List<MixItem> Mix { get; set; } = new();
}

// DTO matching Python AgenticRequest
public record AgenticRequest(List<MixItem> Mix, int? Total = null, bool Debug = false, int Concurrency = 4);
