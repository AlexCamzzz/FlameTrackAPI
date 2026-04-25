namespace FlameTrack.API.Models.DTOs;

public class BudgetDto
{
    public string Id { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

public class CreateBudgetRequestDto
{
    public string CategoryId { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
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