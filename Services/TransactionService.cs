using FlameTrack.API.Models.Entities;
using FlameTrack.API.Models.DTOs;
using MongoDB.Driver;

namespace FlameTrack.API.Services;

public interface ITransactionService
{
    Task<List<TransactionDto>> GetAllAsync(string userId);
    Task<TransactionDto> CreateAsync(CreateTransactionRequestDto request, string userId);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(string userId);
    Task DeleteAsync(string transactionId, string userId);
}

public class TransactionService : ITransactionService
{
    private readonly IMongoCollection<TransactionEntity> _transactions;
    private readonly IMongoCollection<BudgetEntity> _budgets;
    private readonly IMongoCollection<GoalEntity> _goals;
    private readonly IAccountService _accountService;

    public TransactionService(IMongoClient mongoClient, IAccountService accountService)
    {
        var database = mongoClient.GetDatabase("FlameTrackDb");
        _transactions = database.GetCollection<TransactionEntity>("Transactions");
        _budgets = database.GetCollection<BudgetEntity>("Budgets");
        _goals = database.GetCollection<GoalEntity>("Goals");
        _accountService = accountService;
    }

    public async Task<List<TransactionDto>> GetAllAsync(string userId)
    {
        var entities = await _transactions.Find(t => t.UserId == userId).SortByDescending(t => t.Date).ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<TransactionDto> CreateAsync(CreateTransactionRequestDto request, string userId)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be a positive vector.");

        var accounts = await _accountService.GetAllAsync(userId);
        if (!accounts.Any(a => a.Id == request.AccountId))
            throw new Exception("Invalid Account ID.");

        var entity = new TransactionEntity
        {
            UserId = userId,
            AccountId = request.AccountId,
            Description = request.Description,
            Amount = request.Amount,
            Date = request.Date,
            CategoryId = request.CategoryId,
            Type = (TransactionType)request.Type
        };

        await _transactions.InsertOneAsync(entity);
        
        decimal delta = entity.Type == TransactionType.Income ? entity.Amount : -entity.Amount;
        await _accountService.UpdateBalanceAsync(entity.AccountId, delta, userId);

        return MapToDto(entity);
    }

    public async Task DeleteAsync(string transactionId, string userId)
    {
        var filter = Builders<TransactionEntity>.Filter.Eq(t => t.Id, transactionId) & Builders<TransactionEntity>.Filter.Eq(t => t.UserId, userId);
        var transaction = await _transactions.Find(filter).FirstOrDefaultAsync();

        if (transaction == null) throw new Exception("Transaction not found.");

        // Reverse balance
        decimal delta = transaction.Type == TransactionType.Income ? -transaction.Amount : transaction.Amount;
        await _accountService.UpdateBalanceAsync(transaction.AccountId, delta, userId);

        await _transactions.DeleteOneAsync(filter);
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(string userId)
    {
        var allTransactions = await _transactions.Find(t => t.UserId == userId).ToListAsync();
        var allAccounts = await _accountService.GetAllAsync(userId);
        var totalBalance = allAccounts.Sum(a => a.Balance);
        
        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;
        
        var monthlyIncome = allTransactions
            .Where(t => t.Date.Month == currentMonth && t.Date.Year == currentYear && t.Type == TransactionType.Income)
            .Sum(t => t.Amount);
            
        var monthlyExpenses = allTransactions
            .Where(t => t.Date.Month == currentMonth && t.Date.Year == currentYear && t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        var recentTransactions = allTransactions
            .OrderByDescending(t => t.Date)
            .Take(5)
            .Select(MapToDto)
            .ToList();

        var categoryExpensesMap = allTransactions
            .Where(t => t.Date.Month == currentMonth && t.Date.Year == currentYear && t.Type == TransactionType.Expense)
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        var categoryExpenses = categoryExpensesMap
            .Select(kvp => new CategoryExpenseDto
            {
                CategoryId = kvp.Key,
                Amount = kvp.Value,
                Percentage = (double)(monthlyExpenses > 0 ? (kvp.Value / monthlyExpenses * 100) : 0)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var allBudgets = await _budgets.Find(b => b.UserId == userId && b.Month == currentMonth && b.Year == currentYear).ToListAsync();
        var dashboardBudgets = allBudgets.Take(4).Select(b => 
        {
            var spent = categoryExpensesMap.ContainsKey(b.CategoryId) ? categoryExpensesMap[b.CategoryId] : 0;
            return new DashboardBudgetDto
            {
                CategoryId = b.CategoryId,
                Limit = b.Limit,
                Spent = spent,
                Percentage = b.Limit > 0 ? (double)(spent / b.Limit * 100) : 0
            };
        }).ToList();

        var allGoals = await _goals.Find(g => g.UserId == userId).ToListAsync();
        var dashboardGoals = allGoals.Take(3).Select(g => new GoalDto
        {
            Id = g.Id ?? string.Empty,
            Name = g.Name,
            TargetAmount = g.TargetAmount,
            CurrentAmount = g.CurrentAmount,
            Deadline = g.Deadline
        }).ToList();

        return new DashboardSummaryDto
        {
            TotalBalance = totalBalance,
            MonthlyIncome = monthlyIncome,
            MonthlyExpenses = monthlyExpenses,
            SavingsRate = (double)(monthlyIncome > 0 ? ((monthlyIncome - monthlyExpenses) / monthlyIncome * 100) : 0),
            RecentTransactions = recentTransactions,
            CategoryExpenses = categoryExpenses,
            Budgets = dashboardBudgets,
            Goals = dashboardGoals,
            Accounts = allAccounts
        };
    }

    private static TransactionDto MapToDto(TransactionEntity entity) => new()
    {
        Id = entity.Id ?? string.Empty,
        AccountId = entity.AccountId,
        Description = entity.Description,
        Amount = entity.Amount,
        Date = entity.Date,
        CategoryId = entity.CategoryId,
        Type = (TransactionTypeDto)entity.Type
    };
}
