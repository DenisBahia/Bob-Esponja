namespace ETFTracker.Api.Models;

/// <summary>
/// Tracks the FIFO link between a sell transaction and the specific buy lots it consumed.
/// </summary>
public class SellAllocation
{
    public int Id { get; set; }
    public int SellTransactionId { get; set; }
    public int BuyTransactionId { get; set; }
    /// <summary>Number of units from this buy lot consumed by the sell.</summary>
    public decimal AllocatedQuantity { get; set; }
    /// <summary>Denormalised buy price for quick profit calculation.</summary>
    public decimal BuyPrice { get; set; }

    // Navigation
    public Transaction? SellTransaction { get; set; }
    public Transaction? BuyTransaction { get; set; }
}

