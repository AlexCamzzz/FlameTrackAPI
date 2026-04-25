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
    private readonly IMongoCollection<TransactionEntity> _transactions;

    public BudgetService(IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase("FlameTrackDb");
        _budgets = database.GetCollection<BudgetEntity>("Budgets");
        _transactions = database.GetCollection<TransactionEntity>("Transactions");
    }

    public async Task<List<BudgetDto>> GetAllAsync(int month, int year, string userId)
    {
        var budgetFilter = Builders<BudgetEntity>.Filter.Eq(b => b.Month, month) 
                         & Builders<BudgetEntity>.Filter.Eq(b => b.Year, year)
                         & Builders<BudgetEntity>.Filter.Eq(b => b.UserId, userId);
        
        var budgetEntities = await _budgets.Find(budgetFilter).ToListAsync();

        // Calculate Spent for each budget
        var results = new List<BudgetDto>();
        foreach (var b in budgetEntities)
        {
            var txFilter = Builders<TransactionEntity>.Filter.Eq(t => t.UserId, userId)
                         & Builders<TransactionEntity>.Filter.Eq(t => t.CategoryId, b.CategoryId)
                         & Builders<TransactionEntity>.Filter.Eq(t => t.Type, TransactionType.Expense);

            var transactions = await _transactions.Find(txFilter).ToListAsync();
            
            // Further filter by month/year in memory for simplicity or refine Mongo filter
            var spent = transactions
                .Where(t => t.Date.Month == month && t.Date.Year == year)
                .Sum(t => t.Amount);

            results.Add(new BudgetDto
            {
                Id = b.Id ?? string.Empty,
                CategoryId = b.CategoryId,
                Limit = b.Limit,
                Spent = spent,
                Month = b.Month,
                Year = b.Year
            });
        }

        return results;
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
        
        // After creation, return DTO with current spent (which might be > 0 if transactions already exist)
        var budgets = await GetAllAsync(request.Month, request.Year, userId);
        return budgets.FirstOrDefault(b => b.CategoryId == request.CategoryId) ?? new BudgetDto { 
            CategoryId = entity.CategoryId, Limit = entity.Limit, Month = entity.Month, Year = entity.Year 
        };
    }
}
