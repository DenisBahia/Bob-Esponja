namespace ETFTracker.Api.Services;

public interface IPriceService
{
    Task<decimal?> GetPriceAsync(string ticker, CancellationToken cancellationToken = default);
    Task<PriceResult> GetPriceWithSourceAsync(string ticker, CancellationToken cancellationToken = default);
    Task<string?> GetEtfDescriptionAsync(string ticker, CancellationToken cancellationToken = default);
    Task SavePriceSnapshotAsync(string ticker, decimal price, string source, CancellationToken cancellationToken = default);
    Task<decimal?> GetSnapshotPriceAsync(string ticker, DateTime date, CancellationToken cancellationToken = default);
}

public class PriceResult
{
    public decimal? Price { get; set; }
    public string? Source { get; set; }
}

public class PriceProvider
{
    public string Ticker { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Source { get; set; } = string.Empty; // "Eodhd" or "Yahoo"
    public bool Success { get; set; }
}

