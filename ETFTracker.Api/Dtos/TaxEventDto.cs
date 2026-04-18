namespace ETFTracker.Api.Dtos;

public class TaxEventDto
{
    public int Id { get; set; }
    public int HoldingId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string? EtfName { get; set; }
    public int? BuyTransactionId { get; set; }
    public int? SellRecordId { get; set; }
    public string EventType { get; set; } = string.Empty;   // "Sell" | "DeemedDisposal"
    public DateOnly EventDate { get; set; }
    public decimal QuantityAtEvent { get; set; }
    public decimal CostBasisPerUnit { get; set; }
    public decimal PricePerUnitAtEvent { get; set; }
    public decimal TaxableGain { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRateUsed { get; set; }
    public string Status { get; set; } = string.Empty;      // "Pending" | "Paid"
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TaxSummaryDto
{
    public decimal TotalPending { get; set; }
    public decimal TotalPaid { get; set; }
    public DateOnly? NextDeemedDisposalDate { get; set; }
    public List<TaxEventDto> Events { get; set; } = new();

    // ── Tax-free allowance ───────────────────────────────────────────────────
    /// <summary>Annual allowance from ProjectionSettings (0 = disabled or Irish investor).</summary>
    public decimal AnnualTaxFreeAllowance { get; set; }
    /// <summary>Per-year totals after applying the allowance (pending events only).</summary>
    public List<TaxYearAllowanceSummaryDto> AllowanceByYear { get; set; } = new();
    /// <summary>Total pending after deducting the allowance across all years.</summary>
    public decimal TotalPendingAfterAllowance { get; set; }
}

public class MarkTaxEventPaidDto
{
    /// <summary>Optional override for when the payment was made (defaults to now).</summary>
    public DateTime? PaidAt { get; set; }
}

/// <summary>One row per calendar year summarising the allowance impact on pending tax.</summary>
public class TaxYearAllowanceSummaryDto
{
    public int Year { get; set; }
    public decimal TotalTaxableGain { get; set; }
    public decimal TaxBeforeAllowance { get; set; }
    /// <summary>Portion of the annual allowance consumed (capped at tax before allowance).</summary>
    public decimal AllowanceApplied { get; set; }
    /// <summary>Tax after deducting the allowance (floored at 0).</summary>
    public decimal TaxAfterAllowance { get; set; }
}
