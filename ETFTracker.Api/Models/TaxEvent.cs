namespace ETFTracker.Api.Models;

public enum TaxEventType
{
    Sell,
    DeemedDisposal
}

public enum TaxEventStatus
{
    Pending,
    Paid
}

public class TaxEvent
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int HoldingId { get; set; }

    /// <summary>Linked buy transaction (for DeemedDisposal events).</summary>
    public int? BuyTransactionId { get; set; }

    /// <summary>Linked sell record (for Sell events).</summary>
    public int? SellRecordId { get; set; }

    public TaxEventType EventType { get; set; }
    public DateOnly EventDate { get; set; }

    /// <summary>Units of the lot subject to this event.</summary>
    public decimal QuantityAtEvent { get; set; }

    /// <summary>Cost basis per unit at the start of this period.</summary>
    public decimal CostBasisPerUnit { get; set; }

    /// <summary>Market price per unit at the event date.</summary>
    public decimal PricePerUnitAtEvent { get; set; }

    public decimal TaxableGain { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRateUsed { get; set; }

    /// <summary>Sub-type for display and calculation routing. "CGT" | "ExitTax" | "DeemedDisposal"</summary>
    public string? TaxSubType { get; set; }

    public TaxEventStatus Status { get; set; } = TaxEventStatus.Pending;
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public User? User { get; set; }
    public Holding? Holding { get; set; }
    public Transaction? BuyTransaction { get; set; }
    public SellRecord? SellRecord { get; set; }
}
