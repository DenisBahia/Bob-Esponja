namespace ETFTracker.Api.Dtos;

public class TaxSellEntryDto
{
    public int TransactionId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string? EtfName { get; set; }
    /// <summary>ISO date string (yyyy-MM-dd)</summary>
    public string SellDate { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal SellPrice { get; set; }
    /// <summary>FIFO-weighted average buy price for the sold units.</summary>
    public decimal WeightedBuyPrice { get; set; }
    public decimal TaxableProfit { get; set; }
    public decimal? ExitTaxPercent { get; set; }
    public decimal? ExitTaxDue { get; set; }
    public string? SecurityType { get; set; }
}

public class TaxYearSummaryDto
{
    public int Year { get; set; }
    public List<TaxSellEntryDto> Entries { get; set; } = new();
    public decimal TotalTaxableProfit { get; set; }
    /// <summary>Sum of exit tax due across all entries that have a rate configured.</summary>
    public decimal TotalExitTaxDue { get; set; }
    /// <summary>True when at least one entry has no exit-tax rate configured.</summary>
    public bool HasMissingRates { get; set; }
}

