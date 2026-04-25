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

public class BudgetsFunction
{
    private readonly ILogger<BudgetsFunction> _logger;
    private readonly IBudgetService _budgetService;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public BudgetsFunction(ILogger<BudgetsFunction> logger, IBudgetService budgetService)
    {
        _logger = logger;
        _budgetService = budgetService;
    }

    [Function("GetBudgets")]
    public async Task<IActionResult> GetBudgets(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "budgets")] HttpRequestData req,
        int? month, int? year)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        _logger.LogInformation("C# HTTP trigger function processed a request for GetBudgets.");
        
        // Simple fallback to current month/year if not provided
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;

        var budgets = await _budgetService.GetAllAsync(targetMonth, targetYear, userId);
        return new OkObjectResult(budgets);
    }

    [Function("CreateBudget")]
    public async Task<IActionResult> CreateBudget(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "budgets")] HttpRequestData req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        _logger.LogInformation("C# HTTP trigger function processed a request for CreateBudget.");
        
        var data = await JsonSerializer.DeserializeAsync<CreateBudgetRequestDto>(req.Body, _jsonOptions);

        if (data == null)
        {
            return new BadRequestObjectResult("Invalid budget data.");
        }

        var result = await _budgetService.CreateAsync(data, userId);
        return new OkObjectResult(result);
    }
}
