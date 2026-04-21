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
    private readonly ISellService _sellService;
    private readonly ILogger<HoldingsController> _logger;

    public HoldingsController(
        IHoldingsService holdingsService,
        IPriceService priceService,
        ISharingContextService sharingContext,
        ISellService sellService,
        ILogger<HoldingsController> logger)
    {
        _holdingsService = holdingsService;
        _priceService    = priceService;
        _sharingContext  = sharingContext;
        _sellService     = sellService;
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

    /// <summary>
    /// Adds a new transaction (buy or sell) for a holding.
    /// </summary>
    /// <param name="dto">Transaction details including ticker, quantity, price, and date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message if transaction was added</returns>
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding transaction");
            return StatusCode(500, new { message = "Error adding transaction" });
        }
    }

    /// <summary>
    /// Atomically imports a batch of buy/sell transactions from a CSV upload.
    /// All rows are processed in date-ascending order within a single DB transaction.
    /// If any row fails the entire import is rolled back.
    /// </summary>
    [HttpPost("import")]
    public async Task<ActionResult<ImportTransactionsResultDto>> ImportTransactions(
        [FromBody] ImportTransactionsRequestDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "This profile is shared as read-only. You cannot import transactions." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.Rows is not { Count: > 0 })
                return BadRequest(new { message = "No rows provided." });

            var result = await _holdingsService.ImportTransactionsAsync(GetUserId(), dto, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing transactions");
            return StatusCode(500, new { message = "Error importing transactions. No changes were saved." });
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

    /// <summary>Preview a sell — computes FIFO CGT breakdown without persisting anything.</summary>
    [HttpPost("{holdingId}/sell/preview")]
    public async Task<ActionResult<SellPreviewDto>> PreviewSell(int holdingId, [FromBody] SellRequestDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "This profile is shared as read-only." });

            var result = await _sellService.PreviewSellAsync(
                holdingId, GetUserId(), dto.Quantity, dto.SellPrice,
                dto.SellDate, dto.IsIrishInvestor, dto.TaxRate, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing sell for holding {HoldingId}", holdingId);
            return StatusCode(500, new { message = "Error previewing sell" });
        }
    }

    /// <summary>Confirm a sell — persists the FIFO CGT records and updates holding quantity/cost.</summary>
    [HttpPost("{holdingId}/sell/confirm")]
    public async Task<ActionResult<SellRecordDto>> ConfirmSell(int holdingId, [FromBody] SellRequestDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "This profile is shared as read-only." });

            var result = await _sellService.ConfirmSellAsync(
                holdingId, GetUserId(), dto.Quantity, dto.SellPrice,
                dto.SellDate, dto.IsIrishInvestor, dto.TaxRate, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming sell for holding {HoldingId}", holdingId);
            return StatusCode(500, new { message = "Error confirming sell" });
        }
    }

    /// <summary>Get sell history for a holding.</summary>
    [HttpGet("{holdingId}/sell-history")]
    public async Task<ActionResult<List<SellRecordDto>>> GetSellHistory(int holdingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sellService.GetSellHistoryAsync(holdingId, GetUserId(), cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sell history for holding {HoldingId}", holdingId);
            return StatusCode(500, new { message = "Error retrieving sell history" });
        }
    }
}
