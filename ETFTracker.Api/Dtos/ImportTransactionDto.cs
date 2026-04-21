namespace ETFTracker.Api.Dtos;

public class ImportTransactionRowDto
{
    /// <summary>"BUY" or "SELL"</summary>
    public string Operation { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public DateOnly Date { get; set; }
}

public class ImportTransactionsRequestDto
{
    public List<ImportTransactionRowDto> Rows { get; set; } = new();
}

public class ImportTransactionsResultDto
{
    public int Imported { get; set; }
}

