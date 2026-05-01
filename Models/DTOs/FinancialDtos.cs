using FlameTrack.API.Models.Entities;

namespace FlameTrack.API.Models.DTOs;

public class BudgetDto
{
    public string Id { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public decimal Spent { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public BudgetFrequency Frequency { get; set; }
}

public class CreateBudgetRequestDto
{
    public string CategoryId { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public BudgetFrequency Frequency { get; set; } = BudgetFrequency.Monthly;
}

public class GoalDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime Deadline { get; set; }
}
public class CreateGoalRequestDto
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime Deadline { get; set; }
}

public class DepositGoalRequestDto
{
    public decimal Amount { get; set; }
    public string FromAccountId { get; set; } = string.Empty;
}

public class AccountDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public decimal InitialBalance { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Currency { get; set; } = "MXN";
    public bool IsArchived { get; set; }
}

public class CreateAccountRequestDto
{
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal InitialBalance { get; set; }
    public string Color { get; set; } = "#808080";
    public string Currency { get; set; } = "MXN";
}

public class UpdateAccountRequestDto
{
    public string? Name { get; set; }
    public string? Color { get; set; }
}

public class TransferDto
{
    public string Id { get; set; } = string.Empty;
    public string FromAccountId { get; set; } = string.Empty;
    public string ToAccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime Date { get; set; }
}

public class CreateTransferRequestDto
{
    public string FromAccountId { get; set; } = string.Empty;
    public string ToAccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime Date { get; set; }
}

public class RecurringTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public DateTime NextExecutionDate { get; set; }
    public bool IsActive { get; set; }
}

public class CreateRecurringTransactionRequestDto
{
    public string AccountId { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class CategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsStandard { get; set; }
}

public class CreateCategoryRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}