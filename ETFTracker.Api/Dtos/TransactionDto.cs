using ETFTracker.Api.Models;

namespace ETFTracker.Api.Dtos;

public class CreateTransactionDto
{
    public string Ticker { get; set; } = string.Empty;
    public TransactionType TransactionType { get; set; } = TransactionType.Buy;
    public decimal Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public DateOnly PurchaseDate { get; set; }
}

public class UpdateTransactionDto
{
    public decimal Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public DateOnly PurchaseDate { get; set; }
}

public class TransactionDto
{
    public int Id { get; set; }
    public int HoldingId { get; set; }
    /// <summary>"Buy" or "Sell"</summary>
    public string TransactionType { get; set; } = "Buy";
    public decimal Quantity { get; set; }
    /// <summary>Buy price per unit for buys; sell price per unit for sells.</summary>
    public decimal PurchasePrice { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal VariationEur { get; set; }
    public decimal VariationPercent { get; set; }

    // Sell-specific fields (null for buys)
    public decimal? TaxableProfitEur { get; set; }
    public decimal? ExitTaxPercent { get; set; }
    public decimal? ExitTaxDueEur { get; set; }
    public List<SellAllocationDto>? Allocations { get; set; }
}

public class SellAllocationDto
{
    public int BuyTransactionId { get; set; }
    public DateOnly BuyDate { get; set; }
    public decimal BuyPrice { get; set; }
    public decimal AllocatedQuantity { get; set; }
    public decimal ProfitEur { get; set; }
}

public class AssetTaxRateDto
{
    public string SecurityType { get; set; } = string.Empty;
    public decimal ExitTaxPercent { get; set; }
    public string? Label { get; set; }
}
