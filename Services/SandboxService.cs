using FlameTrack.API.Models.Entities;
using FlameTrack.API.Models.DTOs;
using MongoDB.Driver;

namespace FlameTrack.API.Services;

public interface ISandboxService
{
    Task<SandboxDto> GetOrCreateAsync(int month, int year, string userId);
    Task<SandboxMovementDto> AddMovementAsync(string sandboxId, CreateSandboxMovementRequest request, string userId);
    Task ResetAsync(string sandboxId, string userId, bool hardReset = false);
    Task DeleteMovementAsync(string movementId, string userId);
    Task<SandboxMovementDto> UpdateMovementInclusionAsync(string movementId, bool isIncluded, string userId);
}

public class SandboxService : ISandboxService
{
    private readonly IMongoCollection<SandboxSnapshotEntity> _snapshots;
    private readonly IMongoCollection<SandboxMovementEntity> _movements;
    private readonly IMongoCollection<AccountEntity> _accounts;

    public SandboxService(IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase("FlameTrackDb");
        _snapshots = database.GetCollection<SandboxSnapshotEntity>("SandboxSnapshots");
        _movements = database.GetCollection<SandboxMovementEntity>("SandboxMovements");
        _accounts = database.GetCollection<AccountEntity>("Accounts");
    }

    public async Task<SandboxDto> GetOrCreateAsync(int month, int year, string userId)
    {
        var snapshot = await _snapshots.Find(s => s.UserId == userId && s.Month == month && s.Year == year).FirstOrDefaultAsync();

        if (snapshot == null)
        {
            snapshot = await CreateSnapshotAsync(month, year, userId);
        }

        var movements = await _movements.Find(m => m.SandboxId == snapshot.Id && m.UserId == userId).ToListAsync();
        
        var dto = new SandboxDto
        {
            Id = snapshot.Id,
            Month = snapshot.Month,
            Year = snapshot.Year,
            InitialBalances = snapshot.InitialBalances,
            Movements = movements.Select(MapToDto).ToList()
        };

        // Calculate Projected Balances
        dto.ProjectedTotalBalance = dto.InitialBalances.Values.Sum();
        foreach (var m in dto.Movements)
        {
            if (!m.IsIncludedInBalance) continue;

            if (m.Type == TransactionType.Income) dto.ProjectedTotalBalance += m.Amount;
            else dto.ProjectedTotalBalance -= m.Amount;
        }

        return dto;
    }

    private async Task<SandboxSnapshotEntity> CreateSnapshotAsync(int month, int year, string userId, bool ignoreChain = false)
    {
        Dictionary<string, decimal> initialBalances = new();

        if (!ignoreChain)
        {
            // Try to chain from previous month
            int prevMonth = month - 1;
            int prevYear = year;
            if (prevMonth == 0)
            {
                prevMonth = 12;
                prevYear--;
            }

            var prevSnapshot = await _snapshots.Find(s => s.UserId == userId && s.Month == prevMonth && s.Year == prevYear).FirstOrDefaultAsync();

            if (prevSnapshot != null)
            {
                // Calculate ending balance of previous month
                var prevMovements = await _movements.Find(m => m.SandboxId == prevSnapshot.Id).ToListAsync();
                initialBalances = new Dictionary<string, decimal>(prevSnapshot.InitialBalances);
                
                foreach (var m in prevMovements)
                {
                    if (!m.IsIncludedInBalance) continue;
                    if (!initialBalances.ContainsKey(m.AccountId)) initialBalances[m.AccountId] = 0;
                    
                    if (m.Type == TransactionType.Income) initialBalances[m.AccountId] += m.Amount;
                    else initialBalances[m.AccountId] -= m.Amount;
                }
            }
            else
            {
                // Snapshot from real reality
                var realAccounts = await _accounts.Find(a => a.UserId == userId && !a.IsArchived).ToListAsync();
                initialBalances = realAccounts.ToDictionary(a => a.Id!, a => a.Balance);
            }
        }
        else
        {
            // Force snapshot from real reality
            var realAccounts = await _accounts.Find(a => a.UserId == userId && !a.IsArchived).ToListAsync();
            initialBalances = realAccounts.ToDictionary(a => a.Id!, a => a.Balance);
        }

        var newSnapshot = new SandboxSnapshotEntity
        {
            UserId = userId,
            Month = month,
            Year = year,
            InitialBalances = initialBalances
        };

        await _snapshots.InsertOneAsync(newSnapshot);
        return newSnapshot;
    }

    public async Task<SandboxMovementDto> AddMovementAsync(string sandboxId, CreateSandboxMovementRequest request, string userId)
    {
        var movement = new SandboxMovementEntity
        {
            SandboxId = sandboxId,
            UserId = userId,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Description = request.Description,
            Amount = request.Amount,
            Type = request.Type,
            Date = DateTime.UtcNow,
            ExpectedDate = request.ExpectedDate,
            IsIncludedInBalance = request.IsIncludedInBalance
        };

        await _movements.InsertOneAsync(movement);
        return MapToDto(movement);
    }

    public async Task ResetAsync(string sandboxId, string userId, bool hardReset = false)
    {
        var snapshot = await _snapshots.Find(s => s.Id == sandboxId && s.UserId == userId).FirstOrDefaultAsync();

        await _movements.DeleteManyAsync(m => m.SandboxId == sandboxId && m.UserId == userId);
        await _snapshots.DeleteOneAsync(s => s.Id == sandboxId && s.UserId == userId);

        if (hardReset && snapshot != null)
        {
            await CreateSnapshotAsync(snapshot.Month, snapshot.Year, userId, true);
        }
    }

    public async Task DeleteMovementAsync(string movementId, string userId)
    {
        await _movements.DeleteOneAsync(m => m.Id == movementId && m.UserId == userId);
    }

    public async Task<SandboxMovementDto> UpdateMovementInclusionAsync(string movementId, bool isIncluded, string userId)
    {
        var update = Builders<SandboxMovementEntity>.Update.Set(m => m.IsIncludedInBalance, isIncluded);
        var result = await _movements.FindOneAndUpdateAsync(
            m => m.Id == movementId && m.UserId == userId,
            update,
            new FindOneAndUpdateOptions<SandboxMovementEntity> { ReturnDocument = ReturnDocument.After }
        );

        return MapToDto(result);
    }

    private SandboxMovementDto MapToDto(SandboxMovementEntity entity) => new()
    {
        Id = entity.Id,
        AccountId = entity.AccountId,
        CategoryId = entity.CategoryId,
        Description = entity.Description,
        Amount = entity.Amount,
        Type = entity.Type,
        Date = entity.Date,
        ExpectedDate = entity.ExpectedDate,
        IsIncludedInBalance = entity.IsIncludedInBalance
    };
}