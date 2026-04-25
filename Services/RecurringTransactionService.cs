using FlameTrack.API.Models.Entities;
using FlameTrack.API.Models.DTOs;
using MongoDB.Driver;

namespace FlameTrack.API.Services;

public interface IRecurringTransactionService
{
    Task<List<RecurringTransactionDto>> GetAllAsync(string userId);
    Task<RecurringTransactionDto> CreateAsync(CreateRecurringTransactionRequestDto request, string userId);
    Task DeleteAsync(string id, string userId);
    Task ProcessPendingAsync(string userId);
}

public class RecurringTransactionService : IRecurringTransactionService
{
    private readonly IMongoCollection<RecurringTransactionEntity> _recurring;
    private readonly ITransactionService _transactionService;

    public RecurringTransactionService(IMongoClient mongoClient, ITransactionService transactionService)
    {
        var database = mongoClient.GetDatabase("FlameTrackDb");
        _recurring = database.GetCollection<RecurringTransactionEntity>("RecurringTransactions");
        _transactionService = transactionService;
    }

    public async Task<List<RecurringTransactionDto>> GetAllAsync(string userId)
    {
        // First process any pending executions to keep data fresh
        await ProcessPendingAsync(userId);

        var entities = await _recurring.Find(r => r.UserId == userId).ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<RecurringTransactionDto> CreateAsync(CreateRecurringTransactionRequestDto request, string userId)
    {
        var entity = new RecurringTransactionEntity
        {
            UserId = userId,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Description = request.Description,
            Amount = request.Amount,
            Type = request.Type,
            Frequency = request.Frequency,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            NextExecutionDate = request.StartDate, // Initially set to start date
            IsActive = true
        };

        await _recurring.InsertOneAsync(entity);
        
        // Immediate processing if start date is today or in the past
        await ProcessPendingAsync(userId);

        return MapToDto(entity);
    }

    public async Task DeleteAsync(string id, string userId)
    {
        await _recurring.DeleteOneAsync(r => r.Id == id && r.UserId == userId);
    }

    public async Task ProcessPendingAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<RecurringTransactionEntity>.Filter.Eq(r => r.UserId, userId) &
                     Builders<RecurringTransactionEntity>.Filter.Eq(r => r.IsActive, true) &
                     Builders<RecurringTransactionEntity>.Filter.Lte(r => r.NextExecutionDate, now);

        var pending = await _recurring.Find(filter).ToListAsync();

        foreach (var r in pending)
        {
            // Create the real transaction
            var txRequest = new CreateTransactionRequestDto
            {
                AccountId = r.AccountId,
                CategoryId = r.CategoryId,
                Description = r.Description + " (Recurring)",
                Amount = r.Amount,
                Type = (TransactionTypeDto)r.Type,
                Date = r.NextExecutionDate // Record on the scheduled date
            };

            await _transactionService.CreateAsync(txRequest, userId);

            // Update recurring entity state
            r.LastExecutedAt = r.NextExecutionDate;
            r.NextExecutionDate = CalculateNextDate(r.NextExecutionDate, r.Frequency);

            if (r.EndDate.HasValue && r.NextExecutionDate > r.EndDate.Value)
            {
                r.IsActive = false;
            }

            await _recurring.ReplaceOneAsync(x => x.Id == r.Id, r);
        }
    }

    private static DateTime CalculateNextDate(DateTime current, RecurrenceFrequency frequency)
    {
        return frequency switch
        {
            RecurrenceFrequency.Daily => current.AddDays(1),
            RecurrenceFrequency.Weekly => current.AddDays(7),
            RecurrenceFrequency.Monthly => current.AddMonths(1),
            RecurrenceFrequency.Yearly => current.AddYears(1),
            _ => current.AddMonths(1)
        };
    }

    private static RecurringTransactionDto MapToDto(RecurringTransactionEntity entity) => new()
    {
        Id = entity.Id ?? string.Empty,
        AccountId = entity.AccountId,
        CategoryId = entity.CategoryId,
        Description = entity.Description,
        Amount = entity.Amount,
        Type = entity.Type,
        Frequency = entity.Frequency,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        LastExecutedAt = entity.LastExecutedAt,
        NextExecutionDate = entity.NextExecutionDate,
        IsActive = entity.IsActive
    };
}
