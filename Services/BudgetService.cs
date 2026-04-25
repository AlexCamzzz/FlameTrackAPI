using FlameTrack.API.Models.Entities;
using FlameTrack.API.Models.DTOs;
using MongoDB.Driver;

namespace FlameTrack.API.Services;

public interface IBudgetService
{
    Task<List<BudgetDto>> GetAllAsync(int month, int year, string userId);
    Task<BudgetDto> CreateAsync(CreateBudgetRequestDto request, string userId);
}

public class BudgetService : IBudgetService
{
    private readonly IMongoCollection<BudgetEntity> _budgets;

    public BudgetService(IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase("FlameTrackDb");
        _budgets = database.GetCollection<BudgetEntity>("Budgets");
    }

    public async Task<List<BudgetDto>> GetAllAsync(int month, int year, string userId)
    {
        var filter = Builders<BudgetEntity>.Filter.Eq(b => b.Month, month) 
                   & Builders<BudgetEntity>.Filter.Eq(b => b.Year, year)
                   & Builders<BudgetEntity>.Filter.Eq(b => b.UserId, userId);
        var entities = await _budgets.Find(filter).ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<BudgetDto> CreateAsync(CreateBudgetRequestDto request, string userId)
    {
        var entity = new BudgetEntity
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Limit = request.Limit,
            Month = request.Month,
            Year = request.Year
        };

        await _budgets.InsertOneAsync(entity);
        return MapToDto(entity);
    }

    private static BudgetDto MapToDto(BudgetEntity entity) => new()
    {
        Id = entity.Id ?? string.Empty,
        CategoryId = entity.CategoryId,
        Limit = entity.Limit,
        Month = entity.Month,
        Year = entity.Year
    };
}
