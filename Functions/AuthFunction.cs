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
using Microsoft.AspNetCore.RateLimiting;

namespace FlameTrack.API.Functions;

public class AuthFunction
{
    private readonly ILogger<AuthFunction> _logger;
    private readonly IAuthService _authService;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public AuthFunction(ILogger<AuthFunction> logger, IAuthService authService)
    {
        _logger = logger;
        _authService = authService;
    }

    [Function("Register")]
    [EnableRateLimiting("auth-limiter")]
    public async Task<IActionResult> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request for Register.");
        
        try
        {
            var data = await JsonSerializer.DeserializeAsync<RegisterRequestDto>(req.Body, _jsonOptions);
            if (data == null) return new BadRequestObjectResult("Invalid user data.");

            var result = await _authService.RegisterAsync(data);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }

    [Function("Login")]
    [EnableRateLimiting("auth-limiter")]
    public async Task<IActionResult> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request for Login.");
        
        try
        {
            var data = await JsonSerializer.DeserializeAsync<LoginRequestDto>(req.Body, _jsonOptions);
            if (data == null) return new BadRequestObjectResult("Invalid login data.");

            var result = await _authService.LoginAsync(data);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger, "Authentication sequence failed. Verify credentials.");
        }
    }

    [Function("Refresh")]
    [EnableRateLimiting("auth-limiter")]
    public async Task<IActionResult> Refresh(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/refresh")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request for Refresh Token.");
        
        try
        {
            var data = await JsonSerializer.DeserializeAsync<RefreshTokenRequestDto>(req.Body, _jsonOptions);
            if (data == null || string.IsNullOrEmpty(data.RefreshToken)) 
                return new BadRequestObjectResult("Invalid refresh token request.");

            var result = await _authService.RefreshAsync(data.RefreshToken);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            return new UnauthorizedObjectResult(ex.Message);
        }
    }

    [Function("UpdateProfile")]
    public async Task<IActionResult> UpdateProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "auth/profile")] HttpRequest req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        _logger.LogInformation("C# HTTP trigger function processed a request for UpdateProfile.");
        
        try
        {
            var data = await JsonSerializer.DeserializeAsync<UpdateUserRequestDto>(req.Body, _jsonOptions);
            if (data == null) return new BadRequestObjectResult("Invalid data.");

            var result = await _authService.UpdateProfileAsync(userId, data);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }

    [Function("AcceptTerms")]
    public async Task<IActionResult> AcceptTerms(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/accept-terms")] HttpRequest req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        try
        {
            var result = await _authService.AcceptTermsAsync(userId);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }

    [Function("DeleteAccount")]
    public async Task<IActionResult> DeleteAccount(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "auth/profile")] HttpRequest req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        _logger.LogInformation($"C# HTTP trigger function processed account deletion for: {userId}");

        try
        {
            await _authService.DeleteAccountAsync(userId);
            return new NoContentResult();
        }
        catch (Exception ex)
        {
            return ex.ToActionResult(_logger);
        }
    }
}
