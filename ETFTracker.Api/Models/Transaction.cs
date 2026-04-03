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

    // Navigation
    public Holding? Holding { get; set; }
}

