namespace ETFTracker.Api.Models;

public class SellRecord
{
    public int Id { get; set; }
    public int HoldingId { get; set; }
    public DateOnly SellDate { get; set; }
    public decimal SellPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal CgtPaid { get; set; }
    public decimal TaxRateUsed { get; set; }
    public bool IsIrishInvestor { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Holding? Holding { get; set; }
    public ICollection<SellLotAllocation> LotAllocations { get; set; } = new List<SellLotAllocation>();
}

