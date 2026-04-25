using System.Net;
using System.Text.Json;
using FlameTrack.API.Models.DTOs;
using FlameTrack.API.Services;
using FlameTrack.API.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace FlameTrack.API.Functions;

public class GoalsFunction
{
    private readonly ILogger _logger;
    private readonly IGoalService _goalService;

    public GoalsFunction(ILoggerFactory loggerFactory, IGoalService goalService)
    {
        _logger = loggerFactory.CreateLogger<GoalsFunction>();
        _goalService = goalService;
    }

    [Function("GetGoals")]
    public async Task<IActionResult> GetGoals(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "goals")] HttpRequestData req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        var goals = await _goalService.GetAllAsync(userId);
        return new OkObjectResult(goals);
    }

    [Function("CreateGoal")]
    public async Task<IActionResult> CreateGoal(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "goals")] HttpRequestData req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonSerializer.Deserialize<CreateGoalRequestDto>(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        if (request == null) return new BadRequestObjectResult(new { message = "Invalid request." });

        var goal = await _goalService.CreateAsync(request, userId);
        return new OkObjectResult(goal);
    }

    [Function("DepositToGoal")]
    public async Task<IActionResult> DepositToGoal(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "goals/{id}/deposit")] HttpRequestData req, string id)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonSerializer.Deserialize<DepositGoalRequestDto>(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        if (request == null || request.Amount <= 0) return new BadRequestObjectResult(new { message = "Invalid deposit request." });

        try
        {
            var updatedGoal = await _goalService.DepositAsync(id, request, userId);
            return new OkObjectResult(updatedGoal);
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }
}
