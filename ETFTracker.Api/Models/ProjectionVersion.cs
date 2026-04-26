namespace ETFTracker.Api.Models;

public class ProjectionVersion
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string VersionName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime SavedAt { get; set; }

    // Settings snapshot
    public decimal YearlyReturnPercent { get; set; }
    public decimal MonthlyBuyAmount { get; set; }
    public decimal AnnualBuyIncreasePercent { get; set; }
    public int ProjectionYears { get; set; }
    public decimal InflationPercent { get; set; }
    public decimal CgtPercent { get; set; }
    public decimal? StartAmount { get; set; }
    /// <summary>Snapshot: whether DD was applied when this version was saved.</summary>
    public bool ApplyDeemedDisposal { get; set; }
    /// <summary>Snapshot of the DD rate used when this version was saved.</summary>
    public decimal DeemedDisposalPercent { get; set; }

    // Computed data points serialised as JSON at save time (captures portfolio state)
    public string DataPointsJson { get; set; } = "[]";

    // Navigation
    public User? User { get; set; }
}
