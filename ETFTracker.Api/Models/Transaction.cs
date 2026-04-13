namespace ETFTracker.Api.Models;

public enum TransactionType
{
    Buy  = 0,
    Sell = 1
}

public class Transaction
{
    public int Id { get; set; }
    public int HoldingId { get; set; }
    public TransactionType TransactionType { get; set; } = TransactionType.Buy;
    public decimal Quantity { get; set; }
    /// <summary>Buy price per unit for buys; sell price per unit for sells.</summary>
    public decimal PurchasePrice { get; set; }
    /// <summary>Transaction date (buy date or sell date).</summary>
    public DateOnly PurchaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Holding? Holding { get; set; }
    /// <summary>FIFO allocations where this is the SELL side.</summary>
    public ICollection<SellAllocation> SellAllocations { get; set; } = new List<SellAllocation>();
    /// <summary>FIFO allocations where this is the BUY side.</summary>
    public ICollection<SellAllocation> BuyAllocations { get; set; } = new List<SellAllocation>();
}

