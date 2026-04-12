namespace ETFTracker.Api.Services;

public interface IPriceService
{
    Task<decimal?> GetPriceAsync(string ticker, CancellationToken cancellationToken = default);
    Task<PriceResult> GetPriceWithSourceAsync(string ticker, CancellationToken cancellationToken = default);
    Task<string?> GetEtfDescriptionAsync(string ticker, CancellationToken cancellationToken = default);
    Task SavePriceSnapshotAsync(string ticker, decimal price, string source, CancellationToken cancellationToken = default);
    Task<decimal?> GetSnapshotPriceAsync(string ticker, DateTime date, CancellationToken cancellationToken = default);
    Task<List<TickerSearchResult>> SearchTickersAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the full daily price history from Yahoo Finance (period1 = fromDate, period2 = today)
    /// and bulk-inserts any missing <see cref="PriceSnapshot"/> rows into the database.
    /// </summary>
    /// <returns>Number of new rows inserted.</returns>
    Task<int> FetchAndSaveHistoricalPricesAsync(string ticker, DateOnly fromDate, CancellationToken cancellationToken = default);
}

public class TickerSearchResult
{
    public string Symbol { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? LongName { get; set; }
    public string? Exchange { get; set; }
    public string? QuoteType { get; set; }
    public string? TypeDisp { get; set; }
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

