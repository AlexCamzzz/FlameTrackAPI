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

public class AccountsFunction
{
    private readonly ILogger<AccountsFunction> _logger;
    private readonly IAccountService _accountService;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public AccountsFunction(ILogger<AccountsFunction> logger, IAccountService accountService)
    {
        _logger = logger;
        _accountService = accountService;
    }

    [Function("GetAccounts")]
    public async Task<IActionResult> GetAccounts(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "accounts")] HttpRequestData req)
    {
        try
        {
            var userId = req.GetUserId();
            if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

            var accounts = await _accountService.GetAllAsync(userId);
            return new OkObjectResult(accounts);
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }

    [Function("CreateAccount")]
    public async Task<IActionResult> CreateAccount(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "accounts")] HttpRequestData req)
    {
        try
        {
            var userId = req.GetUserId();
            if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

            var data = await JsonSerializer.DeserializeAsync<CreateAccountRequestDto>(req.Body, _jsonOptions);
            if (data == null) return new BadRequestObjectResult("Invalid data.");

            var result = await _accountService.CreateAsync(data, userId);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }

    [Function("UpdateAccount")]
    public async Task<IActionResult> UpdateAccount(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "accounts/{id}")] HttpRequestData req, string id)
    {
        try
        {
            var userId = req.GetUserId();
            if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

            var data = await JsonSerializer.DeserializeAsync<UpdateAccountRequestDto>(req.Body, _jsonOptions);
            if (data == null) return new BadRequestObjectResult("Invalid data.");

            var result = await _accountService.UpdateAsync(id, data, userId);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }

    [Function("ArchiveAccount")]
    public async Task<IActionResult> ArchiveAccount(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "accounts/{id}")] HttpRequestData req, string id)
    {
        try
        {
            var userId = req.GetUserId();
            if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

            await _accountService.ArchiveAsync(id, userId);
            return new OkResult();
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }

    [Function("ResyncBalances")]
    public async Task<IActionResult> ResyncBalances(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "accounts/resync")] HttpRequest req)
    {
        try
        {
            var userId = req.GetUserId();
            if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

            await _accountService.ResyncAllBalancesAsync(userId);
            return new OkResult();
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }
}
