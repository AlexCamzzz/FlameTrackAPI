using FlameTrack.API.Models.Entities;
using FlameTrack.API.Models.DTOs;
using MongoDB.Driver;

namespace FlameTrack.API.Services;

public interface IGoalService
{
    Task<List<GoalDto>> GetAllAsync(string userId);
    Task<GoalDto> CreateAsync(CreateGoalRequestDto request, string userId);
    Task<GoalDto> DepositAsync(string goalId, DepositGoalRequestDto request, string userId);
}

public class GoalService : IGoalService
{
    private readonly IMongoCollection<GoalEntity> _goals;
    private readonly ITransactionService _transactionService;

    public GoalService(IMongoClient mongoClient, ITransactionService transactionService)
    {
        var database = mongoClient.GetDatabase("FlameTrackDb");
        _goals = database.GetCollection<GoalEntity>("Goals");
        _transactionService = transactionService;
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
            CurrentAmount = 0,
            Deadline = request.Deadline
        };

        await _goals.InsertOneAsync(entity);
        return MapToDto(entity);
    }

    public async Task<GoalDto> DepositAsync(string goalId, DepositGoalRequestDto request, string userId)
    {
        var filter = Builders<GoalEntity>.Filter.Eq(g => g.Id, goalId) & Builders<GoalEntity>.Filter.Eq(g => g.UserId, userId);
        var goal = await _goals.Find(filter).FirstOrDefaultAsync();

        if (goal == null) throw new Exception("Goal not found.");

        // Financially integrated deposit: Create a transaction
        var txRequest = new CreateTransactionRequestDto
        {
            AccountId = request.FromAccountId,
            Amount = request.Amount,
            Description = $"Goal Deposit: {goal.Name}",
            Date = DateTime.UtcNow,
            CategoryId = "000000000000000000000000", 
            Type = TransactionTypeDto.Expense
        };

        await _transactionService.CreateAsync(txRequest, userId);

        // Update Goal progress
        var update = Builders<GoalEntity>.Update.Inc(g => g.CurrentAmount, request.Amount);
        await _goals.UpdateOneAsync(filter, update);

        var updatedGoal = await _goals.Find(filter).FirstAsync();
        return MapToDto(updatedGoal);
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
