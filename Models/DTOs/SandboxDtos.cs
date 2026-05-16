using FlameTrack.API.Models.Entities;

namespace FlameTrack.API.Models.DTOs;

public class SandboxDto
{
    public string? Id { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public Dictionary<string, decimal> InitialBalances { get; set; } = new();
    public decimal ProjectedTotalBalance { get; set; }
    public List<SandboxMovementDto> Movements { get; set; } = new();
}

public class SandboxMovementDto
{
    public string? Id { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public bool IsIncludedInBalance { get; set; }
}

public class CreateSandboxMovementRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public bool IsIncludedInBalance { get; set; } = true;
}

public class SandboxSummaryDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public bool HasData { get; set; }
}