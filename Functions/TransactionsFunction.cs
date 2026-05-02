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

public class TransactionsFunction
{
    private readonly ILogger<TransactionsFunction> _logger;
    private readonly ITransactionService _transactionService;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public TransactionsFunction(ILogger<TransactionsFunction> logger, ITransactionService transactionService)
    {
        _logger = logger;
        _transactionService = transactionService;
    }

    [Function("GetTransactions")]
    public async Task<IActionResult> GetTransactions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "transactions")] HttpRequest req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        _logger.LogInformation("C# HTTP trigger function processed a request for GetTransactions.");

        int.TryParse(req.Query["page"], out int page);
        int.TryParse(req.Query["pageSize"], out int pageSize);

        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var result = await _transactionService.GetAllAsync(userId, page, pageSize);
        return new OkObjectResult(result);
    }

    [Function("CreateTransaction")]
    public async Task<IActionResult> CreateTransaction(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "transactions")] HttpRequest req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        _logger.LogInformation("C# HTTP trigger function processed a request for CreateTransaction.");
        
        var data = await JsonSerializer.DeserializeAsync<CreateTransactionRequestDto>(req.Body, _jsonOptions);

        if (data == null)
        {
            return new BadRequestObjectResult("Invalid transaction data.");
        }

        var result = await _transactionService.CreateAsync(data, userId);
        return new OkObjectResult(result);
    }

    [Function("GetDashboardSummary")]
    public async Task<IActionResult> GetDashboardSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dashboard/summary")] HttpRequest req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        _logger.LogInformation("C# HTTP trigger function processed a request for GetDashboardSummary.");
        var summary = await _transactionService.GetDashboardSummaryAsync(userId);
        return new OkObjectResult(summary);
    }

    [Function("DeleteTransaction")]
    public async Task<IActionResult> DeleteTransaction(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "transactions/{id}")] HttpRequest req, string id)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        _logger.LogInformation($"C# HTTP trigger function processed a request for DeleteTransaction: {id}");
        
        try
        {
            await _transactionService.DeleteAsync(id, userId);
            return new OkResult();
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }
}
