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

public class CategoriesFunction
{
    private readonly ILogger<CategoriesFunction> _logger;
    private readonly ICategoryService _categoryService;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public CategoriesFunction(ILogger<CategoriesFunction> logger, ICategoryService categoryService)
    {
        _logger = logger;
        _categoryService = categoryService;
    }

    [Function("GetCategories")]
    public async Task<IActionResult> GetCategories(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "categories")] HttpRequestData req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        _logger.LogInformation("C# HTTP trigger function processed a request for GetCategories.");
        var categories = await _categoryService.GetCategoriesAsync(userId);
        return new OkObjectResult(categories);
    }

    [Function("CreateCategory")]
    public async Task<IActionResult> CreateCategory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "categories")] HttpRequestData req)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        _logger.LogInformation("C# HTTP trigger function processed a request for CreateCategory.");
        
        var data = await JsonSerializer.DeserializeAsync<CreateCategoryRequestDto>(req.Body, _jsonOptions);
        if (data == null) return new BadRequestObjectResult("Invalid category data.");

        var result = await _categoryService.CreateCategoryAsync(data, userId);
        return new OkObjectResult(result);
    }

    [Function("DeleteCategory")]
    public async Task<IActionResult> DeleteCategory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "categories/{id}")] HttpRequestData req, string id)
    {
        var userId = req.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new UnauthorizedResult();

        _logger.LogInformation("C# HTTP trigger function processed a request for DeleteCategory.");
        
        await _categoryService.DeleteCategoryAsync(id, userId);
        return new OkResult();
    }
}
