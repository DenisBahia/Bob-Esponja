using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ETFTracker.Api.Dtos;
using ETFTracker.Api.Services;

namespace ETFTracker.Api.Controllers;

/// <summary>
/// Handles ETF holdings management, portfolio data, and transaction history.
/// Requires JWT authentication.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class HoldingsController : ControllerBase
{
    private readonly IHoldingsService _holdingsService;
    private readonly IPriceService _priceService;
    private readonly ISharingContextService _sharingContext;
    private readonly ILogger<HoldingsController> _logger;

    public HoldingsController(
        IHoldingsService holdingsService,
        IPriceService priceService,
        ISharingContextService sharingContext,
        ILogger<HoldingsController> logger)
    {
        _holdingsService = holdingsService;
        _priceService    = priceService;
        _sharingContext  = sharingContext;
        _logger          = logger;
    }

    private int GetUserId() => _sharingContext.GetEffectiveUserId();

    /// <summary>
    /// Searches for tickers/instruments via Yahoo Finance.
    /// </summary>
    /// <param name="q">Search query (e.g., "apple", "VWRL")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching instruments with symbol, name, exchange and type</returns>
    [HttpGet("search")]
    public async Task<ActionResult<List<TickerSearchResult>>> SearchTickers([FromQuery] string q, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 1)
                return Ok(new List<TickerSearchResult>());

            var results = await _priceService.SearchTickersAsync(q.Trim(), cancellationToken);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error searching tickers for query '{q}'");
            return StatusCode(500, new { message = "Error searching tickers" });
        }
    }

    /// <summary>
    /// Gets the description for a given ETF ticker.
    /// </summary>
    /// <param name="ticker">The ETF ticker symbol (e.g., "VGRO", "XGRO")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ETF description or "ETF not found" message</returns>
    [HttpGet("etf-description/{ticker}")]
    public async Task<ActionResult<object>> GetEtfDescription(string ticker, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ticker))
                return BadRequest(new { message = "Ticker is required" });

            var description = await _priceService.GetEtfDescriptionAsync(ticker.ToUpper(), cancellationToken);
            return Ok(new { description = description ?? "ETF not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting ETF description for {ticker}");
            return StatusCode(500, new { message = "Error retrieving ETF description" });
        }
    }

    /// <summary>
    /// Gets the portfolio evolution data showing how the portfolio has grown over time.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Portfolio evolution data with historical values</returns>
    [HttpGet("portfolio-evolution")]
    public async Task<ActionResult<PortfolioEvolutionDto>> GetPortfolioEvolution(CancellationToken cancellationToken = default)
    {
        try
        {
            var evolution = await _holdingsService.GetPortfolioEvolutionAsync(GetUserId(), cancellationToken);
            return Ok(evolution);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio evolution");
            return StatusCode(500, new { message = "Error retrieving portfolio evolution" });
        }
    }

    /// <summary>
    /// Gets the complete dashboard data including all holdings and portfolio summary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard object with portfolio overview and holdings list</returns>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting dashboard for user {UserId}", GetUserId());
            var dashboard = await _holdingsService.GetDashboardAsync(GetUserId(), cancellationToken);
            _logger.LogInformation("Dashboard retrieved successfully");
            return Ok(dashboard);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard");
            return StatusCode(500, new { message = "Error retrieving dashboard" });
        }
    }

    /// <summary>
    /// Gets all holdings for the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of holdings with current values and performance data</returns>
    [HttpGet]
    public async Task<ActionResult<List<HoldingDto>>> GetHoldings(CancellationToken cancellationToken = default)
    {
        try
        {
            var holdings = await _holdingsService.GetHoldingsAsync(GetUserId(), cancellationToken);
            return Ok(holdings);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting holdings");
            return StatusCode(500, new { message = "Error retrieving holdings" });
        }
    }

    /// <summary>
    /// Gets the transaction history for a specific holding.
    /// </summary>
    /// <param name="holdingId">The ID of the holding</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions (buys/sells) for the holding</returns>
    [HttpGet("{holdingId}/history")]
    public async Task<ActionResult<List<TransactionDto>>> GetHoldingHistory(int holdingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _holdingsService.GetHoldingHistoryAsync(holdingId, cancellationToken);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting history for holding {holdingId}");
            return StatusCode(500, new { message = "Error retrieving holding history" });
        }
    }

    /// <summary>Adds a new transaction (buy or sell) for a holding.</summary>
    [HttpPost("transaction")]
    public async Task<ActionResult> AddTransaction([FromBody] CreateTransactionDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "This profile is shared as read-only. You cannot add transactions." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _holdingsService.AddTransactionAsync(GetUserId(), dto, cancellationToken);
            return Ok(new { message = "Transaction added successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding transaction");
            return StatusCode(500, new { message = "Error adding transaction" });
        }
    }

    /// <summary>Deletes a transaction and recalculates the holding.</summary>
    [HttpDelete("transactions/{transactionId}")]
    public async Task<ActionResult> DeleteTransaction(int transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "This profile is shared as read-only. You cannot delete transactions." });

            await _holdingsService.DeleteTransactionAsync(transactionId, GetUserId(), cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting transaction {transactionId}");
            return StatusCode(500, new { message = "Error deleting transaction" });
        }
    }

    /// <summary>Updates a transaction's quantity, price and date; recalculates the holding.</summary>
    [HttpPatch("transactions/{transactionId}")]
    public async Task<ActionResult> UpdateTransaction(int transactionId, [FromBody] UpdateTransactionDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "This profile is shared as read-only. You cannot edit transactions." });

            await _holdingsService.UpdateTransactionAsync(transactionId, GetUserId(), dto, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating transaction {transactionId}");
            return StatusCode(500, new { message = "Error updating transaction" });
        }
    }

    // ── Asset Tax Rates ───────────────────────────────────────────────────────

    /// <summary>Returns all Exit Tax rates by security type.</summary>
    [HttpGet("tax-rates")]
    public async Task<ActionResult<List<AssetTaxRateDto>>> GetTaxRates(CancellationToken cancellationToken = default)
    {
        try
        {
            var rates = await _holdingsService.GetAssetTaxRatesAsync(cancellationToken);
            return Ok(rates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tax rates");
            return StatusCode(500, new { message = "Error retrieving tax rates" });
        }
    }

    /// <summary>Creates or updates an Exit Tax rate for a security type.</summary>
    [HttpPut("tax-rates")]
    public async Task<ActionResult<AssetTaxRateDto>> UpsertTaxRate([FromBody] AssetTaxRateDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "Read-only access." });

            if (string.IsNullOrWhiteSpace(dto.SecurityType))
                return BadRequest(new { message = "SecurityType is required." });

            var result = await _holdingsService.UpsertAssetTaxRateAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting tax rate");
            return StatusCode(500, new { message = "Error saving tax rate" });
        }
    }

    /// <summary>Deletes an Exit Tax rate for a security type.</summary>
    [HttpDelete("tax-rates/{securityType}")]
    public async Task<ActionResult> DeleteTaxRate(string securityType, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "Read-only access." });

            await _holdingsService.DeleteAssetTaxRateAsync(securityType, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting tax rate for {securityType}");
            return StatusCode(500, new { message = "Error deleting tax rate" });
        }
    }

    // ── Tax Year Summary ───────────────────────────────────────────────────────

    /// <summary>Returns the list of years that have at least one sell transaction.</summary>
    [HttpGet("tax-years")]
    public async Task<ActionResult<List<int>>> GetTaxYears(CancellationToken cancellationToken = default)
    {
        try
        {
            var years = await _holdingsService.GetTaxYearsAsync(GetUserId(), cancellationToken);
            return Ok(years);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tax years");
            return StatusCode(500, new { message = "Error retrieving tax years" });
        }
    }

    /// <summary>Returns all realized gains and exit tax due for the specified year.</summary>
    /// <param name="year">Tax year (e.g., 2026)</param>
    [HttpGet("tax-summary")]
    public async Task<ActionResult<TaxYearSummaryDto>> GetTaxSummary([FromQuery] int year, CancellationToken cancellationToken = default)
    {
        try
        {
            if (year < 2000 || year > 2100)
                return BadRequest(new { message = "Invalid year." });

            var summary = await _holdingsService.GetTaxYearSummaryAsync(GetUserId(), year, cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting tax summary for year {year}");
            return StatusCode(500, new { message = "Error retrieving tax summary" });
        }
    }
}
