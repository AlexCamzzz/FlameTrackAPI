using FlameTrack.API.Models.Entities;
using FlameTrack.API.Models.DTOs;
using MongoDB.Driver;

namespace FlameTrack.API.Services;

public interface IGoalService
{
    Task<List<GoalDto>> GetAllAsync(string userId);
    Task<GoalDto> CreateAsync(CreateGoalRequestDto request, string userId);
    Task<GoalDto> DepositAsync(string goalId, decimal amount, string userId);
}

public class GoalService : IGoalService
{
    private readonly IMongoCollection<GoalEntity> _goals;

    public GoalService(IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase("FlameTrackDb");
        _goals = database.GetCollection<GoalEntity>("Goals");
    }

    public async Task<List<GoalDto>> GetAllAsync(string userId)
    {
        var entities = await _goals.Find(g => g.UserId == userId).ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<GoalDto> CreateAsync(CreateGoalRequestDto request, string userId)
    {
        var entity = new GoalEntity
        {
            UserId = userId,
            Name = request.Name,
            TargetAmount = request.TargetAmount,
            CurrentAmount = request.CurrentAmount,
            Deadline = request.Deadline
        };

        await _goals.InsertOneAsync(entity);
        return MapToDto(entity);
    }

    public async Task<GoalDto> DepositAsync(string goalId, decimal amount, string userId)
    {
        var filter = Builders<GoalEntity>.Filter.Eq(g => g.Id, goalId) & Builders<GoalEntity>.Filter.Eq(g => g.UserId, userId);
        var update = Builders<GoalEntity>.Update.Inc(g => g.CurrentAmount, amount);
        
        var updated = await _goals.FindOneAndUpdateAsync(
            filter, 
            update, 
            new FindOneAndUpdateOptions<GoalEntity> { ReturnDocument = ReturnDocument.After }
        );

        if (updated == null) throw new Exception("Goal not found");

        return MapToDto(updated);
    }

    private static GoalDto MapToDto(GoalEntity entity) => new()
    {
        Id = entity.Id ?? string.Empty,
        Name = entity.Name,
        TargetAmount = entity.TargetAmount,
        CurrentAmount = entity.CurrentAmount,
        Deadline = entity.Deadline
    };
}
