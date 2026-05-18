using System.Net;
using FlameTrack.API.Extensions;
using FlameTrack.API.Models.DTOs;
using FlameTrack.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlameTrack.API.Functions;

public class AiFunction
{
    private readonly ILogger<AiFunction> _logger;
    private readonly IAiService _aiService;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public AiFunction(ILogger<AiFunction> logger, IAiService aiService)
    {
        _logger = logger;
        _aiService = aiService;
    }

    [Function("AskAi")]
    public async Task<IActionResult> AskAi(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ai/ask")] HttpRequestData req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        var data = await JsonSerializer.DeserializeAsync<AiRequestDto>(req.Body, _jsonOptions);
        if (data == null || string.IsNullOrWhiteSpace(data.Message)) 
            return new BadRequestObjectResult("Message is required.");

        try 
        {
            var result = await _aiService.AskAsync(userId, data.Message);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AI Service for user {UserId}", userId);
            return new BadRequestObjectResult(new { error = ex.Message });
        }
    }
}
