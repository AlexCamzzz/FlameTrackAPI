using System.Net;
using FlameTrack.API.Extensions;
using FlameTrack.API.Models.DTOs;
using FlameTrack.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlameTrack.API.Functions;

public class SandboxFunction
{
    private readonly ILogger<SandboxFunction> _logger;
    private readonly ISandboxService _sandboxService;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public SandboxFunction(ILogger<SandboxFunction> logger, ISandboxService sandboxService)
    {
        _logger = logger;
        _sandboxService = sandboxService;
    }

    [Function("GetSandbox")]
    public async Task<IActionResult> GetSandbox(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sandbox/{year:int}/{month:int}")] HttpRequest req, int year, int month)
    {
        try
        {
            var userId = req.GetUserId();
            if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

            _logger.LogInformation($"Retrieving sandbox for {year}/{month} for user {userId}");
            var result = await _sandboxService.GetOrCreateAsync(month, year, userId);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }

    [Function("CreateSandboxMovement")]
    public async Task<IActionResult> CreateSandboxMovement(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sandbox/{sandboxId}/movements")] HttpRequest req, string sandboxId)
    {
        try
        {
            var userId = req.GetUserId();
            if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

            var data = await JsonSerializer.DeserializeAsync<CreateSandboxMovementRequest>(req.Body, _jsonOptions);
            if (data == null) return new BadRequestObjectResult("Invalid movement data.");

            var result = await _sandboxService.AddMovementAsync(sandboxId, data, userId);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }

    [Function("ResetSandbox")]
    public async Task<IActionResult> ResetSandbox(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "sandbox/{sandboxId}")] HttpRequest req, string sandboxId)
    {
        try
        {
            var userId = req.GetUserId();
            if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

            bool hardReset = req.Query.ContainsKey("hard") && bool.TryParse(req.Query["hard"], out bool hard) && hard;

            await _sandboxService.ResetAsync(sandboxId, userId, hardReset);
            return new OkResult();
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }

    [Function("DeleteSandboxMovement")]
    public async Task<IActionResult> DeleteSandboxMovement(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "sandbox/movements/{movementId}")] HttpRequest req, string movementId)
    {
        try
        {
            var userId = req.GetUserId();
            if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

            await _sandboxService.DeleteMovementAsync(movementId, userId);
            return new OkResult();
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }

    [Function("ToggleSandboxMovement")]
    public async Task<IActionResult> ToggleSandboxMovement(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "sandbox/movements/{movementId}/toggle")] HttpRequest req, string movementId)
    {
        try
        {
            var userId = req.GetUserId();
            if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

            var query = req.Query;
            if (!query.ContainsKey("isIncluded") || !bool.TryParse(query["isIncluded"], out bool isIncluded))
            {
                return new BadRequestObjectResult("Missing or invalid 'isIncluded' query parameter.");
            }

            var result = await _sandboxService.UpdateMovementInclusionAsync(movementId, isIncluded, userId);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }
}