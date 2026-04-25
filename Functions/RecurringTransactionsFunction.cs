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

public class RecurringTransactionsFunction
{
    private readonly ILogger _logger;
    private readonly IRecurringTransactionService _recurringService;

    public RecurringTransactionsFunction(ILoggerFactory loggerFactory, IRecurringTransactionService recurringService)
    {
        _logger = loggerFactory.CreateLogger<RecurringTransactionsFunction>();
        _recurringService = recurringService;
    }

    [Function("GetRecurringTransactions")]
    public async Task<IActionResult> GetRecurringTransactions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "recurring")] HttpRequestData req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        var items = await _recurringService.GetAllAsync(userId);
        return new OkObjectResult(items);
    }

    [Function("CreateRecurringTransaction")]
    public async Task<IActionResult> CreateRecurringTransaction(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "recurring")] HttpRequestData req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonSerializer.Deserialize<CreateRecurringTransactionRequestDto>(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        if (request == null) return new BadRequestObjectResult(new { message = "Invalid request." });

        var item = await _recurringService.CreateAsync(request, userId);
        return new OkObjectResult(item);
    }

    [Function("DeleteRecurringTransaction")]
    public async Task<IActionResult> DeleteRecurringTransaction(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "recurring/{id}")] HttpRequestData req, string id)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        await _recurringService.DeleteAsync(id, userId);
        return new NoContentResult();
    }
}
