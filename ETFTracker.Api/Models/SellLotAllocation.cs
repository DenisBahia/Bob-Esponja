namespace ETFTracker.Api.Models;

public class SellLotAllocation
{
    public int Id { get; set; }
    public int SellRecordId { get; set; }
    public int BuyTransactionId { get; set; }
    public decimal QuantityConsumed { get; set; }
    public decimal OriginalCostPerUnit { get; set; }
    public decimal AdjustedCostPerUnit { get; set; }
    public DateOnly? DeemedDisposalDate { get; set; }
    public decimal? DeemedDisposalPricePerUnit { get; set; }
    public decimal ProfitOnLot { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public SellRecord? SellRecord { get; set; }
    public Transaction? BuyTransaction { get; set; }
}

