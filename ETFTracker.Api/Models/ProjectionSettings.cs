namespace ETFTracker.Api.Models;

public class ProjectionSettings
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal YearlyReturnPercent { get; set; }
    public decimal MonthlyBuyAmount { get; set; }
    public decimal AnnualBuyIncreasePercent { get; set; }
    public int ProjectionYears { get; set; }
    public decimal InflationPercent { get; set; }
    public decimal CgtPercent { get; set; }
    public decimal ExitTaxPercent { get; set; }
    public bool ExcludePreExistingFromTax { get; set; }
    /// <summary>SIA (Standard Investment Account) annual tax percentage — charged yearly on total portfolio value.</summary>
    public decimal SiaAnnualPercent { get; set; }
    /// <summary>Optional override for the starting portfolio value (null = use live portfolio value).</summary>
    public decimal? StartAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User? User { get; set; }
}
