namespace ETFTracker.Api.Dtos;

// ── Request ────────────────────────────────────────────────────────────────────
public class SellRequestDto
{
    public decimal Quantity { get; set; }
    public decimal SellPrice { get; set; }
    public DateOnly SellDate { get; set; }
    public bool IsIrishInvestor { get; set; }
    public decimal TaxRate { get; set; }
}

// ── Per-lot breakdown row (preview + history) ──────────────────────────────────
public class SellLotBreakdownDto
{
    public int BuyTransactionId { get; set; }
    public DateOnly BuyDate { get; set; }
    public decimal QuantityConsumed { get; set; }
    public decimal OriginalCostPerUnit { get; set; }
    public decimal AdjustedCostPerUnit { get; set; }
    public DateOnly? DeemedDisposalDate { get; set; }
    public decimal? DeemedDisposalPricePerUnit { get; set; }
    public decimal ProfitOnLot { get; set; }
    public bool DeemedDisposalDue { get; set; }
}

// ── Preview result (not yet saved) ────────────────────────────────────────────
public class SellPreviewDto
{
    public decimal AvailableQuantity { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal CgtDue { get; set; }
    public decimal TaxRateUsed { get; set; }
    /// <summary>"CGT" | "ExitTax"</summary>
    public string TaxType { get; set; } = string.Empty;
    public bool HasLosses { get; set; }
    public List<SellLotBreakdownDto> Lots { get; set; } = new();
}

// ── Saved sell record ──────────────────────────────────────────────────────────
public class SellRecordDto
{
    public int Id { get; set; }
    public int HoldingId { get; set; }
    public DateOnly SellDate { get; set; }
    public decimal SellPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal TaxAmountSaved { get; set; }
    public decimal TaxRateUsed { get; set; }
    /// <summary>"CGT" | "ExitTax"</summary>
    public string TaxType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<SellLotBreakdownDto> Lots { get; set; } = new();
}

// ── Asset-type deemed-disposal default ────────────────────────────────────────
public class AssetTypeDeemedDisposalDefaultDto
{
    public string AssetType { get; set; } = string.Empty;
    public bool DeemedDisposalDue { get; set; }
}
