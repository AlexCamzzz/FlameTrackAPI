using FlameTrack.API.Models.Entities;
using FlameTrack.API.Models.DTOs;
using MongoDB.Driver;

namespace FlameTrack.API.Services;

public interface IDebtService
{
    Task<List<DebtDto>> GetAllAsync(string userId);
    Task<DebtDto> CreateAsync(CreateDebtRequestDto request, string userId);
    Task<DebtDto> PayDebtAsync(string debtId, PayDebtRequestDto request, string userId);
    Task DeleteAsync(string debtId, string userId);
}

public class DebtService : IDebtService
{
    private readonly IMongoCollection<DebtEntity> _debts;
    private readonly ITransactionService _transactionService;

    public DebtService(IMongoClient mongoClient, ITransactionService transactionService)
    {
        var database = mongoClient.GetDatabase("FlameTrackDb");
        _debts = database.GetCollection<DebtEntity>("Debts");
        _transactionService = transactionService;
    }

    public async Task<List<DebtDto>> GetAllAsync(string userId)
    {
        var entities = await _debts.Find(d => d.UserId == userId).ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<DebtDto> CreateAsync(CreateDebtRequestDto request, string userId)
    {
        var entity = new DebtEntity
        {
            UserId = userId,
            CreditorDebtor = request.CreditorDebtor,
            Description = request.Description,
            TotalAmount = request.TotalAmount,
            RemainingAmount = request.TotalAmount,
            DueDate = request.DueDate,
            Type = request.Type,
            IsCleared = false
        };

        await _debts.InsertOneAsync(entity);
        return MapToDto(entity);
    }

    public async Task<DebtDto> PayDebtAsync(string debtId, PayDebtRequestDto request, string userId)
    {
        var filter = Builders<DebtEntity>.Filter.Eq(d => d.Id, debtId) & Builders<DebtEntity>.Filter.Eq(d => d.UserId, userId);
        var debt = await _debts.Find(filter).FirstOrDefaultAsync();

        if (debt == null) throw new Exception("Debt not found.");
        if (debt.IsCleared) throw new Exception("Debt is already cleared.");

        // Create financial transaction
        // If IOwe -> Expense (paying out)
        // If OwedToMe -> Income (receiving payment)
        var txType = debt.Type == DebtType.IOwe ? TransactionTypeDto.Expense : TransactionTypeDto.Income;
        
        var txRequest = new CreateTransactionRequestDto
        {
            AccountId = request.AccountId,
            Amount = request.Amount,
            Description = $"Debt Payment: {debt.CreditorDebtor} ({debt.Description})",
            Date = DateTime.UtcNow,
            CategoryId = "000000000000000000000000", // Standard category for debt? Maybe find a better way
            Type = txType
        };

        await _transactionService.CreateAsync(txRequest, userId);

        // Update Debt
        var newRemaining = debt.RemainingAmount - request.Amount;
        var update = Builders<DebtEntity>.Update
            .Set(d => d.RemainingAmount, Math.Max(0, newRemaining))
            .Set(d => d.IsCleared, newRemaining <= 0);

        await _debts.UpdateOneAsync(filter, update);

        var updatedDebt = await _debts.Find(filter).FirstAsync();
        return MapToDto(updatedDebt);
    }

    public async Task DeleteAsync(string debtId, string userId)
    {
        var filter = Builders<DebtEntity>.Filter.Eq(d => d.Id, debtId) & Builders<DebtEntity>.Filter.Eq(d => d.UserId, userId);
        await _debts.DeleteOneAsync(filter);
    }

    private static DebtDto MapToDto(DebtEntity entity) => new()
    {
        Id = entity.Id ?? string.Empty,
        CreditorDebtor = entity.CreditorDebtor,
        Description = entity.Description,
        TotalAmount = entity.TotalAmount,
        RemainingAmount = entity.RemainingAmount,
        DueDate = entity.DueDate,
        Type = entity.Type,
        IsCleared = entity.IsCleared
    };
}
