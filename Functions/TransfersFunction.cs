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

public class TransfersFunction
{
    private readonly ILogger<TransfersFunction> _logger;
    private readonly ITransferService _transferService;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public TransfersFunction(ILogger<TransfersFunction> logger, ITransferService transferService)
    {
        _logger = logger;
        _transferService = transferService;
    }

    [Function("GetTransfers")]
    public async Task<IActionResult> GetTransfers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "transfers")] HttpRequestData req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        var transfers = await _transferService.GetAllAsync(userId);
        return new OkObjectResult(transfers);
    }

    [Function("CreateTransfer")]
    public async Task<IActionResult> CreateTransfer(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "transfers")] HttpRequestData req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        var data = await JsonSerializer.DeserializeAsync<CreateTransferRequestDto>(req.Body, _jsonOptions);
        if (data == null) return new BadRequestObjectResult("Invalid data.");

        try
        {
            var result = await _transferService.CreateAsync(data, userId);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }
}
