namespace ETFTracker.Api.Dtos;

public class ProjectionSettingsDto
{
    public decimal YearlyReturnPercent { get; set; }
    public decimal MonthlyBuyAmount { get; set; }
    public decimal AnnualBuyIncreasePercent { get; set; }
    public int ProjectionYears { get; set; }
    public decimal InflationPercent { get; set; }
    /// <summary>Single tax rate applied once in the final projected year over total profit.</summary>
    public decimal CgtPercent { get; set; }
    /// <summary>
    /// Optional override for the starting portfolio value used in the projection.
    /// When null or 0, the actual current portfolio value (from live prices) is used.
    /// </summary>
    public decimal? StartAmount { get; set; }
}

public class ProjectionDataPointDto
{
    public int Year { get; set; }
    /// <summary>Current portfolio value at the start of the year (before buys or growth).</summary>
    public decimal InitialBalance { get; set; }
    /// <summary>Sum of all monthly buy contributions for this year.</summary>
    public decimal TotalBuys { get; set; }
    /// <summary>Growth profit generated during the year (returns only, excluding contributions).</summary>
    public decimal YearProfit { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal InflationCorrectedAmount { get; set; }
    /// <summary>Tax paid this year — 0 for all years except the final projected year.</summary>
    public decimal TaxPaid { get; set; }
    /// <summary>Gross portfolio value minus tax (only differs from TotalAmount in the final year).</summary>
    public decimal AfterTaxTotalAmount { get; set; }
    /// <summary>After-tax balance corrected for inflation.</summary>
    public decimal AfterTaxInflationCorrectedAmount { get; set; }
}

public class ProjectionResultDto
{
    public ProjectionSettingsDto Settings { get; set; } = new();
    public List<ProjectionDataPointDto> DataPoints { get; set; } = new();
}
