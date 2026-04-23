namespace ETFTracker.Api.Models;

public class Transaction
{
    public int Id { get; set; }
    public int HoldingId { get; set; }
    public decimal Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// True = this buy is subject to Deemed Disposal and Exit Tax rules (Irish investors).
    /// False = CGT rules apply.
    /// Set by user at buy time, defaulted from AssetTypeDeemedDisposalDefault.
    /// </summary>
    public bool DeemedDisposalDue { get; set; } = false;

    // Navigation
    public Holding? Holding { get; set; }
}
