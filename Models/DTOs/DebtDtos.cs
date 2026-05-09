using FlameTrack.API.Models.Entities;

namespace FlameTrack.API.Models.DTOs;

public class DebtDto
{
    public string Id { get; set; } = string.Empty;
    public string CreditorDebtor { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime DueDate { get; set; }
    public DebtType Type { get; set; }
    public bool IsCleared { get; set; }
}

public class CreateDebtRequestDto
{
    public string CreditorDebtor { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime DueDate { get; set; }
    public DebtType Type { get; set; }
}

public class PayDebtRequestDto
{
    public decimal Amount { get; set; }
    public string AccountId { get; set; } = string.Empty;
}
