using Microsoft.EntityFrameworkCore;
using ETFTracker.Api.Data;
using ETFTracker.Api.Models;

namespace ETFTracker.Api.Services;

public class PriceService : IPriceService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PriceService> _logger;

    public PriceService(HttpClient httpClient, AppDbContext context, IConfiguration configuration, ILogger<PriceService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> GetEtfDescriptionAsync(string ticker, CancellationToken cancellationToken = default)
    {
        try
        {
            // EODHD DISABLED - Use Yahoo Finance only
            // var eodhDescription = await GetEodhDescriptionAsync(ticker, cancellationToken);
            // if (!string.IsNullOrEmpty(eodhDescription))
            // {
            //     return eodhDescription;
            // }

            // Use Yahoo Finance
            _logger.LogInformation($"Fetching description from Yahoo Finance for {ticker}");
            var yahooDescription = await GetYahooDescriptionAsync(ticker, cancellationToken);
            if (!string.IsNullOrEmpty(yahooDescription))
            {
                return yahooDescription;
            }

            _logger.LogWarning($"Failed to get ETF description for {ticker} from any source");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting ETF description for {ticker}");
            return null;
        }
    }

    // EODHD DISABLED - Kept for reference only
    /*
    private async Task<string?> GetEodhDescriptionAsync(string ticker, CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = _configuration["ExternalApis:EodhApi:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_EODHD_API_KEY_HERE")
            {
                _logger.LogWarning("Eodhd API key not configured");
                return null;
            }

            var url = $"https://eodhd.com/api/real-time/{ticker}?api_token={apiKey}&fmt=json";
            
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Eodhd API returned {response.StatusCode} for {ticker} description");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
            
            if (jsonDoc.RootElement.TryGetProperty("name", out var nameElement))
            {
                var name = nameElement.GetString();
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error calling Eodhd API for description of {ticker}");
            return null;
        }
    }
    */

    private async Task<string?> GetYahooDescriptionAsync(string ticker, CancellationToken cancellationToken)
    {
        try
        {
            // Replace XETRA with DE for Yahoo Finance API
            var yahooTicker = ticker.Replace(".XETRA", ".DE", StringComparison.OrdinalIgnoreCase);
            
            // Yahoo Finance v8 chart API endpoint
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{yahooTicker}?interval=1d&range=2d";
            
            // Add User-Agent header to avoid being blocked
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Yahoo API returned {response.StatusCode} for {ticker} description");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
            
            if (jsonDoc.RootElement.TryGetProperty("chart", out var chartElement) &&
                chartElement.TryGetProperty("result", out var result) &&
                result.GetArrayLength() > 0)
            {
                var resultItem = result[0];
                
                // Try to get the name/description from meta
                if (resultItem.TryGetProperty("meta", out var meta))
                {
                    // Try longName first, then shortName
                    if (meta.TryGetProperty("longName", out var longNameElement))
                    {
                        var longName = longNameElement.GetString();
                        if (!string.IsNullOrEmpty(longName) && longName != "null")
                        {
                            return longName;
                        }
                    }

                    if (meta.TryGetProperty("shortName", out var shortNameElement))
                    {
                        var shortName = shortNameElement.GetString();
                        if (!string.IsNullOrEmpty(shortName) && shortName != "null")
                        {
                            return shortName;
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error calling Yahoo Finance API for description of {ticker}");
            return null;
        }
    }

    public async Task<decimal?> GetPriceAsync(string ticker, CancellationToken cancellationToken = default)
    {
        var result = await GetPriceWithSourceAsync(ticker, cancellationToken);
        return result.Price;
    }

    public async Task<PriceResult> GetPriceWithSourceAsync(string ticker, CancellationToken cancellationToken = default)
    {
        try
        {
            // EODHD DISABLED - Use Yahoo Finance only
            // var eodhPrice = await GetEodhPriceAsync(ticker, cancellationToken);
            // if (eodhPrice.HasValue)
            // {
            //     await SavePriceSnapshotAsync(ticker, eodhPrice.Value, "Eodhd", cancellationToken);
            //     return new PriceResult { Price = eodhPrice.Value, Source = "Eodhd" };
            // }

            // Use Yahoo Finance
            _logger.LogInformation($"Fetching price from Yahoo Finance for {ticker}");
            var yahooPrice = await GetYahooPriceAsync(ticker, cancellationToken);
            if (yahooPrice.HasValue)
            {
                await SavePriceSnapshotAsync(ticker, yahooPrice.Value, "Yahoo", cancellationToken);
                return new PriceResult { Price = yahooPrice.Value, Source = "Yahoo" };
            }

            // If both fail, try to get the latest snapshot from database
            var lastSnapshot = await _context.PriceSnapshots
                .Where(ps => ps.Ticker == ticker)
                .OrderByDescending(ps => ps.SnapshotDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastSnapshot != null)
            {
                _logger.LogWarning($"Yahoo API failed for {ticker}, using last snapshot: {lastSnapshot.Price}");
                return new PriceResult { Price = lastSnapshot.Price, Source = lastSnapshot.Source ?? "Cache" };
            }

            _logger.LogError($"Failed to get price for {ticker} from any source");
            return new PriceResult { Price = null, Source = null };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting price for {ticker}");
            return new PriceResult { Price = null, Source = null };
        }
    }

    // EODHD DISABLED - Kept for reference only
    /*
    private async Task<decimal?> GetEodhPriceAsync(string ticker, CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = _configuration["ExternalApis:EodhApi:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_EODHD_API_KEY_HERE")
            {
                _logger.LogWarning("Eodhd API key not configured");
                return null;
            }

            var url = $"https://eodhd.com/api/real-time/{ticker}?api_token={apiKey}&fmt=json";
            
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Eodhd API returned {response.StatusCode} for {ticker}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
            
            if (jsonDoc.RootElement.TryGetProperty("close", out var closeElement) &&
                decimal.TryParse(closeElement.GetString(), out var price))
            {
                return price;
            }

            _logger.LogWarning($"Could not parse price from Eodhd response for {ticker}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error calling Eodhd API for {ticker}");
            return null;
        }
    }
    */

    private async Task<decimal?> GetYahooPriceAsync(string ticker, CancellationToken cancellationToken)
    {
        try
        {
            // Replace XETRA with DE for Yahoo Finance API
            var yahooTicker = ticker.Replace(".XETRA", ".DE", StringComparison.OrdinalIgnoreCase);
            
            // Yahoo Finance v8 chart API endpoint - request 2 days to get previous close
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{yahooTicker}?interval=1d&range=2d";
            
            // Log the full URL for debugging
            Console.WriteLine($"[Yahoo Finance] Fetching price for ticker '{ticker}' (converted to '{yahooTicker}')");
            Console.WriteLine($"[Yahoo Finance] URL: {url}");
            
            // Add User-Agent header to avoid being blocked
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Yahoo API returned {response.StatusCode} for {ticker} (requested as {yahooTicker})");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
            
            if (jsonDoc.RootElement.TryGetProperty("chart", out var chartElement) &&
                chartElement.TryGetProperty("result", out var result) &&
                result.GetArrayLength() > 0)
            {
                var resultItem = result[0];
                
                // First, try to save yesterday's closing price if it exists (only once per day)
                if (resultItem.TryGetProperty("indicators", out var indicators) &&
                    indicators.TryGetProperty("quote", out var quoteArray) &&
                    quoteArray.GetArrayLength() > 0)
                {
                    var quote = quoteArray[0];
                    if (quote.TryGetProperty("close", out var closePrices) &&
                        closePrices.GetArrayLength() >= 2)
                    {
                        var yesterdayCloseText = closePrices[0].GetRawText();
                        if (!string.IsNullOrEmpty(yesterdayCloseText) && yesterdayCloseText != "null" &&
                            decimal.TryParse(yesterdayCloseText, System.Globalization.CultureInfo.InvariantCulture, out var yesterdayClose) &&
                            yesterdayClose > 0)
                        {
                            // Save yesterday's closing price (only if not already saved today)
                            await SavePreviousDayPriceAsync(ticker, yesterdayClose, "Yahoo", cancellationToken);
                            Console.WriteLine($"[Yahoo Finance] ✓ Saved previous day closing price for {ticker}: {yesterdayClose}");
                            _logger.LogInformation($"Saved previous day closing price for {ticker}: {yesterdayClose}");
                        }
                    }
                }
                
                // Get the regularMarketPrice from meta object (today's price)
                if (resultItem.TryGetProperty("meta", out var meta))
                {
                    if (meta.TryGetProperty("regularMarketPrice", out var priceElement))
                    {
                        var priceText = priceElement.GetRawText();
                        if (!string.IsNullOrEmpty(priceText) && priceText != "null")
                        {
                            if (decimal.TryParse(priceText, System.Globalization.CultureInfo.InvariantCulture, out var price) && price > 0)
                            {
                                Console.WriteLine($"[Yahoo Finance] ✓ Successfully got price for {ticker}: {price}");
                                _logger.LogInformation($"Successfully got price from Yahoo for {ticker} (requested as {yahooTicker}): {price}");
                                return price;
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"[Yahoo Finance] ✗ Could not parse price from response for {ticker}");
            _logger.LogWarning($"Could not parse price from Yahoo response for {ticker} (requested as {yahooTicker})");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Yahoo Finance] ✗ Error fetching price for {ticker}: {ex.Message}");
            _logger.LogWarning(ex, $"Error calling Yahoo Finance API for {ticker}");
            return null;
        }
    }

    public async Task SavePriceSnapshotAsync(string ticker, decimal price, string source, CancellationToken cancellationToken = default)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var existingSnapshot = await _context.PriceSnapshots
                .FirstOrDefaultAsync(ps => ps.Ticker == ticker && ps.SnapshotDate == today, cancellationToken);

            if (existingSnapshot != null)
            {
                existingSnapshot.Price = price;
                existingSnapshot.Source = source;
                existingSnapshot.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.PriceSnapshots.Add(new PriceSnapshot
                {
                    Ticker = ticker,
                    Price = price,
                    SnapshotDate = today,
                    Source = source,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving price snapshot for {ticker}");
            throw;
        }
    }

    private async Task SavePreviousDayPriceAsync(string ticker, decimal price, string source, CancellationToken cancellationToken = default)
    {
        try
        {
            var yesterday = DateTime.UtcNow.AddDays(-1).Date;
            
            // Check if a snapshot for yesterday already exists
            var existingSnapshot = await _context.PriceSnapshots
                .FirstOrDefaultAsync(ps => ps.Ticker == ticker && ps.SnapshotDate == yesterday, cancellationToken);

            // Only save if it doesn't exist yet (first login of the day)
            if (existingSnapshot == null)
            {
                _context.PriceSnapshots.Add(new PriceSnapshot
                {
                    Ticker = ticker,
                    Price = price,
                    SnapshotDate = yesterday,
                    Source = source,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"Saved previous day closing price for {ticker} on {yesterday:yyyy-MM-dd}: {price}");
            }
            else
            {
                _logger.LogInformation($"Previous day closing price for {ticker} already exists, skipping duplicate save");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving previous day price snapshot for {ticker}");
            // Don't throw - this is a secondary operation and shouldn't fail the main price fetch
        }
    }

    public async Task<List<TickerSearchResult>> SearchTickersAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"https://query1.finance.yahoo.com/v1/finance/search?q={encodedQuery}&quotesCount=10&newsCount=0&enableFuzzyQuery=false&quotesQueryId=tss_match_phrase_query";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Yahoo Finance search returned {response.StatusCode} for query '{query}'");
                return new List<TickerSearchResult>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(content);

            var results = new List<TickerSearchResult>();

            if (jsonDoc.RootElement.TryGetProperty("quotes", out var quotes))
            {
                foreach (var quote in quotes.EnumerateArray())
                {
                    if (!quote.TryGetProperty("symbol", out var symbolEl)) continue;
                    var symbol = symbolEl.GetString();
                    if (string.IsNullOrEmpty(symbol)) continue;

                    results.Add(new TickerSearchResult
                    {
                        Symbol      = symbol,
                        ShortName   = quote.TryGetProperty("shortname",  out var sn)  ? sn.GetString()  : null,
                        LongName    = quote.TryGetProperty("longname",   out var ln)  ? ln.GetString()  : null,
                        Exchange    = quote.TryGetProperty("exchange",   out var ex)  ? ex.GetString()  : null,
                        QuoteType   = quote.TryGetProperty("quoteType",  out var qt)  ? qt.GetString()  : null,
                        TypeDisp    = quote.TryGetProperty("typeDisp",   out var td)  ? td.GetString()  : null
                    });
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error searching tickers for query '{query}'");
            return new List<TickerSearchResult>();
        }
    }

    public async Task<decimal?> GetSnapshotPriceAsync(string ticker, DateTime date, CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await _context.PriceSnapshots
                .Where(ps => ps.Ticker == ticker && ps.SnapshotDate == date.Date)
                .OrderByDescending(ps => ps.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            return snapshot?.Price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting snapshot price for {ticker} on {date:yyyy-MM-dd}");
            return null;
        }
    }

    public async Task<int> FetchAndSaveHistoricalPricesAsync(
        string ticker,
        DateOnly fromDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Yahoo Finance uses ".DE" instead of ".XETRA"
            var yahooTicker = ticker.Replace(".XETRA", ".DE", StringComparison.OrdinalIgnoreCase);

            var period1 = new DateTimeOffset(fromDate.Year, fromDate.Month, fromDate.Day, 0, 0, 0, TimeSpan.Zero)
                .ToUnixTimeSeconds();
            var period2 = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(yahooTicker)}" +
                      $"?period1={period1}&period2={period2}&interval=1d";

            _logger.LogInformation(
                "Fetching historical prices for {Ticker} (Yahoo: {YahooTicker}) from {FromDate} — URL: {Url}",
                ticker, yahooTicker, fromDate, url);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Yahoo Finance historical API returned {StatusCode} for {Ticker}",
                    response.StatusCode, ticker);
                return 0;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(content);

            if (!jsonDoc.RootElement.TryGetProperty("chart", out var chart) ||
                !chart.TryGetProperty("result", out var resultArray) ||
                resultArray.GetArrayLength() == 0)
            {
                _logger.LogWarning("No historical data returned for {Ticker}", ticker);
                return 0;
            }

            var resultItem = resultArray[0];

            if (!resultItem.TryGetProperty("timestamp", out var timestampsEl) ||
                !resultItem.TryGetProperty("indicators", out var indicators) ||
                !indicators.TryGetProperty("quote", out var quoteArray) ||
                quoteArray.GetArrayLength() == 0)
            {
                _logger.LogWarning("Unexpected response structure for historical data of {Ticker}", ticker);
                return 0;
            }

            var quoteItem = quoteArray[0];
            if (!quoteItem.TryGetProperty("close", out var closePricesEl))
            {
                _logger.LogWarning("No close prices in historical data for {Ticker}", ticker);
                return 0;
            }

            // Load existing snapshot dates to skip duplicates
            var existingDates = (await _context.PriceSnapshots
                .Where(ps => ps.Ticker == ticker)
                .Select(ps => ps.SnapshotDate)
                .ToListAsync(cancellationToken))
                .ToHashSet();

            var timestampList = timestampsEl.EnumerateArray().ToList();
            var closePriceList = closePricesEl.EnumerateArray().ToList();

            var newSnapshots = new List<PriceSnapshot>();
            var now = DateTime.UtcNow;

            for (var i = 0; i < timestampList.Count; i++)
            {
                if (i >= closePriceList.Count) break;

                // Parse close price — Yahoo returns null on non-trading days
                var closeRaw = closePriceList[i].GetRawText();
                if (string.IsNullOrEmpty(closeRaw) || closeRaw == "null") continue;
                if (!decimal.TryParse(closeRaw,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var closePrice) || closePrice <= 0)
                    continue;

                var unixTs = timestampList[i].GetInt64();
                var snapshotDate = DateTimeOffset.FromUnixTimeSeconds(unixTs).UtcDateTime.Date;

                if (existingDates.Contains(snapshotDate)) continue;

                newSnapshots.Add(new PriceSnapshot
                {
                    Ticker      = ticker,
                    Price       = closePrice,
                    SnapshotDate = snapshotDate,
                    Source      = "Yahoo",
                    CreatedAt   = now,
                    UpdatedAt   = now
                });

                // Track so we don't double-add in the same batch
                existingDates.Add(snapshotDate);
            }

            if (newSnapshots.Count > 0)
            {
                _context.PriceSnapshots.AddRange(newSnapshots);
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Historical price backfill for {Ticker}: {Count} new snapshots saved",
                ticker, newSnapshots.Count);

            return newSnapshots.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching historical prices for {Ticker}", ticker);
            return 0;
        }
    }
}



