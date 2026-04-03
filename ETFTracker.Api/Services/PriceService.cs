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
            // Try Eodhd first
            var eodhDescription = await GetEodhDescriptionAsync(ticker, cancellationToken);
            if (!string.IsNullOrEmpty(eodhDescription))
            {
                return eodhDescription;
            }

            // Fallback to Yahoo Finance
            _logger.LogInformation($"Eodhd description failed for {ticker}, falling back to Yahoo Finance");
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
            // Try Eodhd first
            var eodhPrice = await GetEodhPriceAsync(ticker, cancellationToken);
            if (eodhPrice.HasValue)
            {
                await SavePriceSnapshotAsync(ticker, eodhPrice.Value, "Eodhd", cancellationToken);
                return new PriceResult { Price = eodhPrice.Value, Source = "Eodhd" };
            }

            // Fallback to Yahoo Finance
            _logger.LogInformation($"Eodhd failed for {ticker}, falling back to Yahoo Finance");
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
                _logger.LogWarning($"Both APIs failed for {ticker}, using last snapshot: {lastSnapshot.Price}");
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

    private async Task<decimal?> GetYahooPriceAsync(string ticker, CancellationToken cancellationToken)
    {
        try
        {
            // Replace XETRA with DE for Yahoo Finance API
            var yahooTicker = ticker.Replace(".XETRA", ".DE", StringComparison.OrdinalIgnoreCase);
            
            // Yahoo Finance v8 chart API endpoint
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
                
                // Get the regularMarketPrice from meta object
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
            }
            else
            {
                _context.PriceSnapshots.Add(new PriceSnapshot
                {
                    Ticker = ticker,
                    Price = price,
                    SnapshotDate = today,
                    Source = source,
                    CreatedAt = DateTime.UtcNow
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
}



