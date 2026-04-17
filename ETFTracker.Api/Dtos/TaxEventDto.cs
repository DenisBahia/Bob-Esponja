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
}

public class MarkTaxEventPaidDto
{
    /// <summary>Optional override for when the payment was made (defaults to now).</summary>
    public DateTime? PaidAt { get; set; }
}

