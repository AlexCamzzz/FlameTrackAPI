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

public class DebtsFunction
{
    private readonly ILogger _logger;
    private readonly IDebtService _debtService;

    public DebtsFunction(ILoggerFactory loggerFactory, IDebtService debtService)
    {
        _logger = loggerFactory.CreateLogger<DebtsFunction>();
        _debtService = debtService;
    }

    [Function("GetDebts")]
    public async Task<IActionResult> GetDebts(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "debts")] HttpRequestData req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        var debts = await _debtService.GetAllAsync(userId);
        return new OkObjectResult(debts);
    }

    [Function("CreateDebt")]
    public async Task<IActionResult> CreateDebt(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "debts")] HttpRequestData req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonSerializer.Deserialize<CreateDebtRequestDto>(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        if (request == null) return new BadRequestObjectResult(new { message = "Invalid request." });

        var debt = await _debtService.CreateAsync(request, userId);
        return new OkObjectResult(debt);
    }

    [Function("PayDebt")]
    public async Task<IActionResult> PayDebt(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "debts/{id}/pay")] HttpRequestData req, string id)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonSerializer.Deserialize<PayDebtRequestDto>(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        if (request == null || request.Amount <= 0) return new BadRequestObjectResult(new { message = "Invalid payment request." });

        try
        {
            var updatedDebt = await _debtService.PayDebtAsync(id, request, userId);
            return new OkObjectResult(updatedDebt);
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }

    [Function("DeleteDebt")]
    public async Task<IActionResult> DeleteDebt(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "debts/{id}")] HttpRequestData req, string id)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        await _debtService.DeleteAsync(id, userId);
        return new NoContentResult();
    }
}
