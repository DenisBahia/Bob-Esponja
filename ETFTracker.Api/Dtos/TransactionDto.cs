namespace ETFTracker.Api.Dtos;

public class CreateTransactionDto
{
    public string Ticker { get; set; } = string.Empty;
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

