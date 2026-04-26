namespace ETFTracker.Api.Models;

/// <summary>Stores projection-only parameters. Has no knowledge of tax rules.</summary>
public class ProjectionSettings
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal YearlyReturnPercent { get; set; }
    public decimal MonthlyBuyAmount { get; set; }
    public decimal AnnualBuyIncreasePercent { get; set; }
    public int ProjectionYears { get; set; }
    public decimal InflationPercent { get; set; }
    /// <summary>Optional override for the starting portfolio value (null = use live portfolio value).</summary>
    public decimal? StartAmount { get; set; }
    /// <summary>When true, 8-year deemed disposal events are simulated (Irish investors only).</summary>
    public bool ApplyDeemedDisposal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User? User { get; set; }
}
