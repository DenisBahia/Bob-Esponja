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
    private readonly ILogger<HoldingsController> _logger;

    public HoldingsController(IHoldingsService holdingsService, IPriceService priceService, ILogger<HoldingsController> logger)
    {
        _holdingsService = holdingsService;
        _priceService = priceService;
        _logger = logger;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedAccessException("userId claim missing"));

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
}
