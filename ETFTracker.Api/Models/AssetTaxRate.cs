namespace ETFTracker.Api.Models;

/// <summary>
/// Lookup table: Exit Tax percentage per security type.
/// Seeded with ETF = 38 % and MUTUALFUND = 38 %.
/// </summary>
public class AssetTaxRate
{
    /// <summary>Primary key — matches the SecurityType values used on Holdings (e.g. "ETF", "MUTUALFUND").</summary>
    public string SecurityType { get; set; } = string.Empty;
    public decimal ExitTaxPercent { get; set; }
    public string? Label { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

