using System.Net;
using FlameTrack.API.Models.DTOs;
using FlameTrack.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
    public async Task<IActionResult> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequestData req)
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
            return new BadRequestObjectResult(new { message = ex.Message });
        }
    }

    [Function("Login")]
    public async Task<IActionResult> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req)
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
            return new UnauthorizedObjectResult(new { message = ex.Message });
        }
    }
}
