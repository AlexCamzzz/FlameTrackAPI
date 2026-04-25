namespace FlameTrack.API.Models.DTOs;

public enum TransactionTypeDto
{
    Income,
    Expense
}

public class TransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public TransactionTypeDto Type { get; set; }
}

public class CreateTransactionRequestDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public TransactionTypeDto Type { get; set; }
}

public class DashboardSummaryDto
{
    public decimal TotalBalance { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public double SavingsRate { get; set; }
    public List<TransactionDto> RecentTransactions { get; set; } = new();
    public List<CategoryExpenseDto> CategoryExpenses { get; set; } = new();
    public List<DashboardBudgetDto> Budgets { get; set; } = new();
    public List<GoalDto> Goals { get; set; } = new();
}

public class DashboardBudgetDto
{
    public string CategoryId { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public decimal Spent { get; set; }
    public double Percentage { get; set; }
}

public class CategoryExpenseDto
{
    public string CategoryId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public double Percentage { get; set; }
}