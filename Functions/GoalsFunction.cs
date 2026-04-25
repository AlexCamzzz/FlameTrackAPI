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

public class GoalsFunction
{
    private readonly ILogger<GoalsFunction> _logger;
    private readonly IGoalService _goalService;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public GoalsFunction(ILogger<GoalsFunction> logger, IGoalService goalService)
    {
        _logger = logger;
        _goalService = goalService;
    }

    [Function("GetGoals")]
    public async Task<IActionResult> GetGoals(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "goals")] HttpRequestData req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        _logger.LogInformation("C# HTTP trigger function processed a request for GetGoals.");
        var goals = await _goalService.GetAllAsync(userId);
        return new OkObjectResult(goals);
    }

    [Function("CreateGoal")]
    public async Task<IActionResult> CreateGoal(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "goals")] HttpRequestData req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        _logger.LogInformation("C# HTTP trigger function processed a request for CreateGoal.");
        
        var data = await JsonSerializer.DeserializeAsync<CreateGoalRequestDto>(req.Body, _jsonOptions);

        if (data == null)
        {
            return new BadRequestObjectResult("Invalid goal data.");
        }

        var result = await _goalService.CreateAsync(data, userId);
        return new OkObjectResult(result);
    }

    [Function("DepositGoal")]
    public async Task<IActionResult> DepositGoal(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "goals/{id}/deposit")] HttpRequestData req, string id)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        _logger.LogInformation("C# HTTP trigger function processed a request for DepositGoal.");
        
        var data = await JsonSerializer.DeserializeAsync<DepositGoalRequestDto>(req.Body, _jsonOptions);

        if (data == null || data.Amount <= 0)
        {
            return new BadRequestObjectResult("Invalid deposit amount.");
        }

        try
        {
            var result = await _goalService.DepositAsync(id, data.Amount, userId);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            return new NotFoundObjectResult(ex.Message);
        }
    }
}
