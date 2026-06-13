using FlameTrack.API.Models.Entities;
using FlameTrack.API.Models.DTOs;
using MongoDB.Driver;

namespace FlameTrack.API.Services;

public interface IAccountService
{
    Task<List<AccountDto>> GetAllAsync(string userId);
    Task<AccountDto> CreateAsync(CreateAccountRequestDto request, string userId);
    Task<AccountDto> UpdateAsync(string accountId, UpdateAccountRequestDto request, string userId);
    Task ArchiveAsync(string accountId, string userId);
    Task UpdateBalanceAsync(string accountId, decimal delta, string userId);
    Task ResyncAllBalancesAsync(string userId);
}

public class AccountService : IAccountService
{
    private readonly IMongoCollection<AccountEntity> _accounts;
    private readonly IMongoDatabase _database;

    public AccountService(IMongoClient mongoClient)
    {
        _database = mongoClient.GetDatabase("FlameTrackDb");
        _accounts = _database.GetCollection<AccountEntity>("Accounts");
    }

    public async Task<List<AccountDto>> GetAllAsync(string userId)
    {
        var entities = await _accounts.Find(a => a.UserId == userId && !a.IsArchived).ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<AccountDto> CreateAsync(CreateAccountRequestDto request, string userId)
    {
        var entity = new AccountEntity
        {
            UserId = userId,
            Name = request.Name,
            Type = request.Type,
            InitialBalance = request.InitialBalance,
            Balance = request.InitialBalance,
            Color = request.Color,
            Currency = request.Currency
        };

        await _accounts.InsertOneAsync(entity);
        return MapToDto(entity);
    }

    public async Task<AccountDto> UpdateAsync(string accountId, UpdateAccountRequestDto request, string userId)
    {
        var filter = Builders<AccountEntity>.Filter.Eq(a => a.Id, accountId) & Builders<AccountEntity>.Filter.Eq(a => a.UserId, userId);
        var update = Builders<AccountEntity>.Update;
        var updates = new List<UpdateDefinition<AccountEntity>>();

        if (request.Name != null) updates.Add(update.Set(a => a.Name, request.Name));
        if (request.Color != null) updates.Add(update.Set(a => a.Color, request.Color));

        if (!updates.Any()) return MapToDto(await _accounts.Find(filter).FirstAsync());

        var updated = await _accounts.FindOneAndUpdateAsync(filter, update.Combine(updates), new FindOneAndUpdateOptions<AccountEntity> { ReturnDocument = ReturnDocument.After });
        return MapToDto(updated);
    }

    public async Task ArchiveAsync(string accountId, string userId)
    {
        var filter = Builders<AccountEntity>.Filter.Eq(a => a.Id, accountId) & Builders<AccountEntity>.Filter.Eq(a => a.UserId, userId);
        var account = await _accounts.Find(filter).FirstOrDefaultAsync();
        
        if (account == null) throw new Exception("Account not found.");
        if (account.Balance != 0) throw new Exception("Cannot archive account with non-zero balance. Transfer funds first.");

        var update = Builders<AccountEntity>.Update.Set(a => a.IsArchived, true);
        await _accounts.UpdateOneAsync(filter, update);
    }

    public async Task UpdateBalanceAsync(string accountId, decimal delta, string userId)
    {
        var filter = Builders<AccountEntity>.Filter.Eq(a => a.Id, accountId) & Builders<AccountEntity>.Filter.Eq(a => a.UserId, userId);
        var update = Builders<AccountEntity>.Update.Inc(a => a.Balance, delta);
        await _accounts.UpdateOneAsync(filter, update);
    }

    public async Task ResyncAllBalancesAsync(string userId)
    {
        var accounts = await _accounts.Find(a => a.UserId == userId).ToListAsync();
        var transactions = await _database.GetCollection<TransactionEntity>("Transactions").Find(t => t.UserId == userId).ToListAsync();
        var transfers = await _database.GetCollection<TransferEntity>("Transfers").Find(t => t.UserId == userId).ToListAsync();

        foreach (var account in accounts)
        {
            decimal newBalance = account.InitialBalance;

            // Apply Transactions
            var accountTransactions = transactions.Where(t => t.AccountId == account.Id);
            foreach (var t in accountTransactions)
            {
                if (t.Type == TransactionType.Income) newBalance += t.Amount;
                else newBalance -= t.Amount;
            }

            // Apply Transfers
            var outTransfers = transfers.Where(t => t.FromAccountId == account.Id);
            foreach (var t in outTransfers) newBalance -= t.Amount;

            var inTransfers = transfers.Where(t => t.ToAccountId == account.Id);
            foreach (var t in inTransfers) newBalance += t.Amount;

            // Update Account Balance
            var filter = Builders<AccountEntity>.Filter.Eq(a => a.Id, account.Id);
            var update = Builders<AccountEntity>.Update.Set(a => a.Balance, newBalance);
            await _accounts.UpdateOneAsync(filter, update);
        }
    }

    private static AccountDto MapToDto(AccountEntity entity) => new()
    {
        Id = entity.Id ?? string.Empty,
        Name = entity.Name,
        Type = (AccountType)entity.Type,
        Balance = entity.Balance,
        InitialBalance = entity.InitialBalance,
        Color = entity.Color,
        Currency = entity.Currency,
        IsArchived = entity.IsArchived
    };
}
