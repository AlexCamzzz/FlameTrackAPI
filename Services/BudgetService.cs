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
        // Fetch all budgets for the user. In a more advanced version, we could filter by active status or start date.
        var budgetFilter = Builders<BudgetEntity>.Filter.Eq(b => b.UserId, userId);
        var budgetEntities = await _budgets.Find(budgetFilter).ToListAsync();

        var results = new List<BudgetDto>();
        
        // Fetch all transactions for this user for the target year to minimize DB calls in the loop
        var txFilter = Builders<TransactionEntity>.Filter.Eq(t => t.UserId, userId)
                     & Builders<TransactionEntity>.Filter.Eq(t => t.Type, TransactionType.Expense)
                     & Builders<TransactionEntity>.Filter.Gte(t => t.Date, new DateTime(year, 1, 1))
                     & Builders<TransactionEntity>.Filter.Lt(t => t.Date, new DateTime(year + 1, 1, 1));

        var transactions = await _transactions.Find(txFilter).ToListAsync();

        foreach (var b in budgetEntities)
        {
            decimal spent = 0;

            if (b.Frequency == BudgetFrequency.Monthly)
            {
                // Calculate spent for the specific target month
                spent = transactions
                    .Where(t => t.CategoryId == b.CategoryId && t.Date.Month == month)
                    .Sum(t => t.Amount);
            }
            else if (b.Frequency == BudgetFrequency.Annual)
            {
                // Calculate spent for the whole target year
                spent = transactions
                    .Where(t => t.CategoryId == b.CategoryId)
                    .Sum(t => t.Amount);
            }

            results.Add(new BudgetDto
            {
                Id = b.Id ?? string.Empty,
                CategoryId = b.CategoryId,
                Limit = b.Limit,
                Spent = spent,
                Month = b.Month,
                Year = b.Year,
                Frequency = b.Frequency
            });
        }

        return results;
    }

    public async Task<BudgetDto> CreateAsync(CreateBudgetRequestDto request, string userId)
    {
        // Optional: Check if a budget for this category and frequency already exists to update it instead
        var existingFilter = Builders<BudgetEntity>.Filter.Eq(b => b.UserId, userId)
                            & Builders<BudgetEntity>.Filter.Eq(b => b.CategoryId, request.CategoryId)
                            & Builders<BudgetEntity>.Filter.Eq(b => b.Frequency, request.Frequency);
        
        var existing = await _budgets.Find(existingFilter).FirstOrDefaultAsync();

        if (existing != null)
        {
            existing.Limit = request.Limit;
            existing.Month = request.Month;
            existing.Year = request.Year;
            await _budgets.ReplaceOneAsync(b => b.Id == existing.Id, existing);
            
            var budgets = await GetAllAsync(request.Month, request.Year, userId);
            return budgets.First(b => b.Id == existing.Id);
        }

        var entity = new BudgetEntity
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Limit = request.Limit,
            Month = request.Month,
            Year = request.Year,
            Frequency = request.Frequency
        };

        await _budgets.InsertOneAsync(entity);
        
        var allBudgets = await GetAllAsync(request.Month, request.Year, userId);
        return allBudgets.FirstOrDefault(b => b.CategoryId == request.CategoryId && b.Frequency == request.Frequency) 
               ?? new BudgetDto { CategoryId = entity.CategoryId, Limit = entity.Limit, Frequency = entity.Frequency };
    }
}
