namespace ETFTracker.Api.Models;

public class PriceSnapshot
{
    public int Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime SnapshotDate { get; set; }
    public string? Source { get; set; } // "Eodhd" or "Yahoo"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

