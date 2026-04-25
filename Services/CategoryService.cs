using FlameTrack.API.Models.Entities;
using FlameTrack.API.Models.DTOs;
using MongoDB.Driver;

namespace FlameTrack.API.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetCategoriesAsync(string userId);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequestDto request, string userId);
    Task DeleteCategoryAsync(string categoryId, string userId);
}

public class CategoryService : ICategoryService
{
    private readonly IMongoCollection<CategoryEntity> _categories;

    public CategoryService(IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase("FlameTrackDb");
        _categories = database.GetCollection<CategoryEntity>("Categories");
        
        SeedStandardCategories();
    }

    private void SeedStandardCategories()
    {
        if (!_categories.Find(c => c.UserId == null).Any())
        {
            var standardCategories = new List<CategoryEntity>
            {
                new CategoryEntity { Name = "Essential", Color = "#808080" },
                new CategoryEntity { Name = "Leisure", Color = "#FFAB40" },
                new CategoryEntity { Name = "Fixed", Color = "#4CAF50" },
                new CategoryEntity { Name = "Travel", Color = "#FF5722" },
                new CategoryEntity { Name = "Food", Color = "#F44336" }
            };
            _categories.InsertMany(standardCategories);
        }
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(string userId)
    {
        var filter = Builders<CategoryEntity>.Filter.Eq(c => c.UserId, null) | Builders<CategoryEntity>.Filter.Eq(c => c.UserId, userId);
        var entities = await _categories.Find(filter).ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequestDto request, string userId)
    {
        var color = string.IsNullOrEmpty(request.Color) ? GetRandomColor() : request.Color;

        var entity = new CategoryEntity
        {
            UserId = userId,
            Name = request.Name,
            Color = color
        };

        await _categories.InsertOneAsync(entity);
        return MapToDto(entity);
    }

    public async Task DeleteCategoryAsync(string categoryId, string userId)
    {
        var filter = Builders<CategoryEntity>.Filter.Eq(c => c.Id, categoryId) & Builders<CategoryEntity>.Filter.Eq(c => c.UserId, userId);
        await _categories.DeleteOneAsync(filter);
    }

    private static string GetRandomColor()
    {
        var random = new Random();
        return String.Format("#{0:X6}", random.Next(0x1000000));
    }

    private static CategoryDto MapToDto(CategoryEntity entity) => new()
    {
        Id = entity.Id ?? string.Empty,
        Name = entity.Name,
        Color = entity.Color,
        IsStandard = string.IsNullOrEmpty(entity.UserId)
    };
}
