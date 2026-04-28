namespace ETFTracker.Api.Dtos;

public class ProjectionSettingsDto
{
    public decimal YearlyReturnPercent { get; set; }
    public decimal MonthlyBuyAmount { get; set; }
    public decimal AnnualBuyIncreasePercent { get; set; }
    public int ProjectionYears { get; set; }
    public decimal InflationPercent { get; set; }
    /// <summary>
    /// Tax rate for the final exit event — always resolved from UserSettings (ExitTaxPercent for Irish,
    /// CgtPercent for non-Irish). Never stored in projection_settings; read-only for the caller.
    /// </summary>
    public decimal CgtPercent { get; set; }
    /// <summary>
    /// Optional override for the starting portfolio value used in the projection.
    /// When null or 0, the actual current portfolio value (from live prices) is used.
    /// </summary>
    public decimal? StartAmount { get; set; }

    // ── Deemed Disposal (Irish investors only) ────────────────────────────────
    /// <summary>When true, 8-year deemed disposal events are simulated per contribution cohort.</summary>
    public bool ApplyDeemedDisposal { get; set; }
    /// <summary>
    /// DD rate resolved from UserSettings at calculation time.
    /// Read-only in the projection UI — always driven by the user's tax profile.
    /// </summary>
    public decimal DeemedDisposalPercent { get; set; }
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
    /// <summary>Exit tax paid in the final projected year (0 for all other years).</summary>
    public decimal TaxPaid { get; set; }
    /// <summary>Deemed Disposal tax paid in this year (Irish DD mode only; 0 otherwise).</summary>
    public decimal DeemedDisposalPaid { get; set; }
    /// <summary>Gross portfolio value minus tax (only differs from TotalAmount in tax years).</summary>
    public decimal AfterTaxTotalAmount { get; set; }
    /// <summary>After-tax balance corrected for inflation.</summary>
    public decimal AfterTaxInflationCorrectedAmount { get; set; }

    // ── SIA (Standard Investment Account — Irish Gov. plan from 2027+) ──────────
    /// <summary>SIA annual charge this year (siaAnnualPercent × totalAmount). 0 when SIA is disabled or user is non-Irish.</summary>
    public decimal SiaTaxDue { get; set; }
    /// <summary>Portfolio value after cumulative SIA charges up to and including this year.</summary>
    public decimal AfterSiaTotalAmount { get; set; }
}

public class ProjectionResultDto
{
    public ProjectionSettingsDto Settings { get; set; } = new();
    public List<ProjectionDataPointDto> DataPoints { get; set; } = new();
}
