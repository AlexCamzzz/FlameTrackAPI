using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FlameTrack.API.Models.Entities;

public enum TransactionType
{
    Income,
    Expense
}

public class TransactionEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    
    // Updated: using CategoryId instead of Category string and Tags list
    [BsonRepresentation(BsonType.ObjectId)]
    public string CategoryId { get; set; } = string.Empty;
    
    public TransactionType Type { get; set; }
}

public class BudgetEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string CategoryId { get; set; } = string.Empty;
    
    public decimal Limit { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

// Unified Tag/Category Entity
public class CategoryEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    public string? UserId { get; set; } // null means standard category
    
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class GoalEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime Deadline { get; set; }
}