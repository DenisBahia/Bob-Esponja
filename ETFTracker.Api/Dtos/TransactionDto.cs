namespace ETFTracker.Api.Dtos;

public class CreateTransactionDto
{
    public string Ticker { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public DateOnly PurchaseDate { get; set; }
    /// <summary>Required to trigger deemed-disposal checks. Pass true for Irish investors.</summary>
    public bool IsIrishInvestor { get; set; }
    /// <summary>Exit-tax rate (%) to use for deemed-disposal calculations. Typically 41 for Irish ETFs.</summary>
    public decimal TaxRate { get; set; }
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
    public decimal Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal VariationEur { get; set; }
    public decimal VariationPercent { get; set; }
}

