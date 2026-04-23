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
    public decimal ExitTaxPercent { get; set; }
    public bool ExcludePreExistingFromTax { get; set; }
    public decimal SiaAnnualPercent { get; set; }
    public bool IsIrishInvestor { get; set; } = false;
    public decimal TaxFreeAllowancePerYear { get; set; } = 0m;
    public decimal DeemedDisposalPercent { get; set; } = 41m;

    // Computed data points serialised as JSON at save time (captures portfolio state)
    public string DataPointsJson { get; set; } = "[]";

    // Navigation
    public User? User { get; set; }
}
