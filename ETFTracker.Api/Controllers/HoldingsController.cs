using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ETFTracker.Api.Dtos;
using ETFTracker.Api.Services;

namespace ETFTracker.Api.Controllers;

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
