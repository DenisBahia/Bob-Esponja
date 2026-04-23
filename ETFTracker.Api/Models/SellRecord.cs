namespace ETFTracker.Api.Models;

public class SellRecord
{
    public int Id { get; set; }
    public int HoldingId { get; set; }
    public DateOnly SellDate { get; set; }
    public decimal SellPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalProfit { get; set; }
    /// <summary>Tax amount saved/pending for this sell record.</summary>
    public decimal TaxAmountSaved { get; set; }
    public decimal TaxRateUsed { get; set; }
    /// <summary>"CGT" | "ExitTax"</summary>
    public string TaxType { get; set; } = "CGT";
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Holding? Holding { get; set; }
    public ICollection<SellLotAllocation> LotAllocations { get; set; } = new List<SellLotAllocation>();
}
