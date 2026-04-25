using FlameTrack.API.Models.Entities;
using FlameTrack.API.Models.DTOs;
using MongoDB.Driver;

namespace FlameTrack.API.Services;

public interface ITransferService
{
    Task<List<TransferDto>> GetAllAsync(string userId);
    Task<TransferDto> CreateAsync(CreateTransferRequestDto request, string userId);
}

public class TransferService : ITransferService
{
    private readonly IMongoCollection<TransferEntity> _transfers;
    private readonly IAccountService _accountService;

    public TransferService(IMongoClient mongoClient, IAccountService accountService)
    {
        var database = mongoClient.GetDatabase("FlameTrackDb");
        _transfers = database.GetCollection<TransferEntity>("Transfers");
        _accountService = accountService;
    }

    public async Task<List<TransferDto>> GetAllAsync(string userId)
    {
        var entities = await _transfers.Find(t => t.UserId == userId).SortByDescending(t => t.Date).ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<TransferDto> CreateAsync(CreateTransferRequestDto request, string userId)
    {
        if (request.FromAccountId == request.ToAccountId)
            throw new Exception("Source and destination accounts must be different.");

        var entity = new TransferEntity
        {
            UserId = userId,
            FromAccountId = request.FromAccountId,
            ToAccountId = request.ToAccountId,
            Amount = request.Amount,
            Note = request.Note,
            Date = request.Date
        };

        // Atomic-ish update
        await _accountService.UpdateBalanceAsync(request.FromAccountId, -request.Amount, userId);
        try 
        {
            await _accountService.UpdateBalanceAsync(request.ToAccountId, request.Amount, userId);
        }
        catch 
        {
            // Compensate
            await _accountService.UpdateBalanceAsync(request.FromAccountId, request.Amount, userId);
            throw;
        }

        await _transfers.InsertOneAsync(entity);
        return MapToDto(entity);
    }

    private static TransferDto MapToDto(TransferEntity entity) => new()
    {
        Id = entity.Id ?? string.Empty,
        FromAccountId = entity.FromAccountId,
        ToAccountId = entity.ToAccountId,
        Amount = entity.Amount,
        Note = entity.Note,
        Date = entity.Date
    };
}
