namespace ETFTracker.Api.Dtos;

public class ProjectionSettingsDto
{
    public decimal YearlyReturnPercent { get; set; }
    public decimal MonthlyBuyAmount { get; set; }
    public decimal AnnualBuyIncreasePercent { get; set; }
    public int ProjectionYears { get; set; }
    public decimal InflationPercent { get; set; }
    public decimal CgtPercent { get; set; }
    public decimal ExitTaxPercent { get; set; }
    /// <summary>When true, buys made before 1 Jan 2026 are excluded from CGT and exit tax calculations.</summary>
    public bool ExcludePreExistingFromTax { get; set; }
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
    /// <summary>Deemed disposal CGT paid this year (triggered by 8-year holding rule).</summary>
    public decimal TaxPaid { get; set; }
    /// <summary>Exit tax paid in the final projected year only.</summary>
    public decimal ExitTaxPaid { get; set; }
    /// <summary>Gross portfolio value minus all cumulative taxes paid to date.</summary>
    public decimal AfterTaxTotalAmount { get; set; }
    /// <summary>After-tax balance inflation-corrected: InflationCorrectedAmount - TaxPaid - ExitTaxPaid.</summary>
    public decimal AfterTaxInflationCorrectedAmount { get; set; }
}

public class ProjectionResultDto
{
    public ProjectionSettingsDto Settings { get; set; } = new();
    public List<ProjectionDataPointDto> DataPoints { get; set; } = new();
}
