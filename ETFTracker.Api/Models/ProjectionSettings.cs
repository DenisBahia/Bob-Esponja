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
    /// <summary>True when the user is subject to the Irish Exit Tax / Deemed Disposal regime.
    /// When true the tax-free allowance feature is suppressed.</summary>
    public bool IsIrishInvestor { get; set; } = false;
    /// <summary>Annual tax-free CGT allowance (e.g. £3,000 UK). 0 = disabled.
    /// Not applicable for Irish investors.</summary>
    public decimal TaxFreeAllowancePerYear { get; set; } = 0m;
    /// <summary>Tax rate applied to 8-year deemed disposal events (Irish investors only).
    /// Defaults to ExitTaxPercent when not explicitly set. User cannot override this at point-of-use.</summary>
    public decimal DeemedDisposalPercent { get; set; } = 41m;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User? User { get; set; }
}
