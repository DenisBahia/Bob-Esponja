using Microsoft.EntityFrameworkCore;
using ETFTracker.Api.Data;
using ETFTracker.Api.Dtos;
using ETFTracker.Api.Models;

namespace ETFTracker.Api.Services;

public interface IHoldingsService
{
    Task<DashboardDto> GetDashboardAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<HoldingDto>> GetHoldingsAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<TransactionDto>> GetHoldingHistoryAsync(int holdingId, CancellationToken cancellationToken = default);
    Task AddTransactionAsync(int userId, CreateTransactionDto dto, CancellationToken cancellationToken = default);
    Task DeleteTransactionAsync(int transactionId, int userId, CancellationToken cancellationToken = default);
    Task UpdateTransactionAsync(int transactionId, int userId, UpdateTransactionDto dto, CancellationToken cancellationToken = default);
    Task<PortfolioEvolutionDto> GetPortfolioEvolutionAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<AssetTaxRateDto>> GetAssetTaxRatesAsync(CancellationToken cancellationToken = default);
    Task<AssetTaxRateDto> UpsertAssetTaxRateAsync(AssetTaxRateDto dto, CancellationToken cancellationToken = default);
    Task DeleteAssetTaxRateAsync(string securityType, CancellationToken cancellationToken = default);
    Task<TaxYearSummaryDto> GetTaxYearSummaryAsync(int userId, int year, CancellationToken cancellationToken = default);
    Task<List<int>> GetTaxYearsAsync(int userId, CancellationToken cancellationToken = default);
}

public class HoldingsService : IHoldingsService
{
    private readonly AppDbContext _context;
    private readonly IPriceService _priceService;
    private readonly ILogger<HoldingsService> _logger;

    public HoldingsService(AppDbContext context, IPriceService priceService, ILogger<HoldingsService> logger)
    {
        _context = context;
        _priceService = priceService;
        _logger = logger;
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    public async Task<DashboardDto> GetDashboardAsync(int userId, CancellationToken cancellationToken = default)
    {
        var holdings = await GetHoldingsAsync(userId, cancellationToken);

        var dashboard = new DashboardDto();

        var totalValue    = holdings.Sum(h => h.TotalValue);
        var totalInvested = holdings.Sum(h => h.Quantity * h.AverageCost);
        dashboard.Header.TotalHoldingsAmount = totalValue;

        var totalVariationEur     = totalValue - totalInvested;
        var totalVariationPercent = totalInvested > 0 ? (totalVariationEur / totalInvested) * 100 : 0;
        dashboard.Header.TotalVariation = new PeriodMetrics
        {
            GainLossEur     = totalVariationEur,
            GainLossPercent = totalVariationPercent
        };

        dashboard.Header.DailyMetrics = new PeriodMetrics
        {
            GainLossEur       = holdings.Sum(h => h.DailyMetrics.GainLossEur),
            GainLossPercent   = CalculateWeightedPercentage(holdings, h => h.DailyMetrics.GainLossPercent, h => h.TotalValue),
            PricesUnavailable = holdings.Any(h => h.DailyMetrics.PricesUnavailable)
        };
        dashboard.Header.WeeklyMetrics = new PeriodMetrics
        {
            GainLossEur       = holdings.Sum(h => h.WeeklyMetrics.GainLossEur),
            GainLossPercent   = CalculateWeightedPercentage(holdings, h => h.WeeklyMetrics.GainLossPercent, h => h.TotalValue),
            PricesUnavailable = holdings.Any(h => h.WeeklyMetrics.PricesUnavailable)
        };
        dashboard.Header.MonthlyMetrics = new PeriodMetrics
        {
            GainLossEur       = holdings.Sum(h => h.MonthlyMetrics.GainLossEur),
            GainLossPercent   = CalculateWeightedPercentage(holdings, h => h.MonthlyMetrics.GainLossPercent, h => h.TotalValue),
            PricesUnavailable = holdings.Any(h => h.MonthlyMetrics.PricesUnavailable)
        };
        dashboard.Header.YtdMetrics = new PeriodMetrics
        {
            GainLossEur       = holdings.Sum(h => h.YtdMetrics.GainLossEur),
            GainLossPercent   = CalculateWeightedPercentage(holdings, h => h.YtdMetrics.GainLossPercent, h => h.TotalValue),
            PricesUnavailable = holdings.Any(h => h.YtdMetrics.PricesUnavailable)
        };

        dashboard.Holdings = holdings;
        return dashboard;
    }

    // ── Holdings ──────────────────────────────────────────────────────────────

    public async Task<List<HoldingDto>> GetHoldingsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var dbHoldings = await _context.Holdings
            .Where(h => h.UserId == userId)
            .Include(h => h.Transactions)
            .OrderBy(h => h.Ticker)
            .ToListAsync(cancellationToken);

        var holdingDtos = new List<HoldingDto>();

        foreach (var holding in dbHoldings)
        {
            var priceResult    = await _priceService.GetPriceWithSourceAsync(holding.Ticker, cancellationToken);
            var priceUnavailable = !priceResult.Price.HasValue;
            var displayPrice   = priceResult.Price ?? 0;
            var totalValue     = holding.Quantity * displayPrice;

            if (priceResult.Source != null)
            {
                holding.PriceSource = priceResult.Source;
                _context.Holdings.Update(holding);
            }

            if (holding.SecurityType == null)
            {
                var secType = await _priceService.GetSecurityTypeAsync(holding.Ticker, cancellationToken);
                if (secType != null)
                {
                    holding.SecurityType = secType;
                    _context.Holdings.Update(holding);
                }
            }

            // Deemed Disposal: only from buy transactions
            DateOnly? deemedDisposalDueDate = null;
            if (IsSubjectToDeemedDisposal(holding.SecurityType))
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var futureDates = holding.Transactions
                    .Where(t => t.TransactionType == TransactionType.Buy)
                    .Select(t => t.PurchaseDate.AddYears(8))
                    .Where(d => d > today)
                    .OrderBy(d => d)
                    .ToList();
                deemedDisposalDueDate = futureDates.Count > 0 ? futureDates[0] : (DateOnly?)null;
            }

            holdingDtos.Add(new HoldingDto
            {
                Id                   = holding.Id,
                Ticker               = holding.Ticker,
                EtfName              = holding.EtfName,
                Quantity             = holding.Quantity,
                AverageCost          = holding.AverageCost,
                CurrentPrice         = displayPrice,
                TotalValue           = totalValue,
                PriceUnavailable     = priceUnavailable,
                PriceSource          = priceResult.Source,
                SecurityType         = holding.SecurityType,
                DeemedDisposalDueDate = deemedDisposalDueDate,
                DailyMetrics         = await CalculatePeriodMetricsAsync(holding.Ticker, holding.Quantity, 1,  cancellationToken),
                WeeklyMetrics        = await CalculatePeriodMetricsAsync(holding.Ticker, holding.Quantity, 7,  cancellationToken),
                MonthlyMetrics       = await CalculatePeriodMetricsAsync(holding.Ticker, holding.Quantity, 30, cancellationToken),
                YtdMetrics           = await CalculateYtdMetricsAsync(holding.Ticker, holding.Quantity, cancellationToken)
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return holdingDtos;
    }

    // ── History ───────────────────────────────────────────────────────────────

    public async Task<List<TransactionDto>> GetHoldingHistoryAsync(int holdingId, CancellationToken cancellationToken = default)
    {
        var holding = await _context.Holdings
            .FirstOrDefaultAsync(h => h.Id == holdingId, cancellationToken);

        if (holding == null) return new List<TransactionDto>();

        var currentPrice = await _priceService.GetPriceAsync(holding.Ticker, cancellationToken) ?? 0;

        // Look up exit-tax rate for this security type
        decimal? taxRate = null;
        if (holding.SecurityType != null)
        {
            var taxRateRecord = await _context.AssetTaxRates
                .FindAsync(new object[] { holding.SecurityType }, cancellationToken);
            taxRate = taxRateRecord?.ExitTaxPercent;
        }

        var transactions = await _context.Transactions
            .Where(t => t.HoldingId == holdingId)
            .OrderBy(t => t.PurchaseDate)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);

        // Pre-load all sell allocations for this holding's sells
        var sellIds = transactions
            .Where(t => t.TransactionType == TransactionType.Sell)
            .Select(t => t.Id)
            .ToList();

        var allAllocations = sellIds.Count > 0
            ? await _context.SellAllocations
                .Where(a => sellIds.Contains(a.SellTransactionId))
                .ToListAsync(cancellationToken)
            : new List<SellAllocation>();

        // Build buy-transaction lookup for dates (buys are already in transactions list)
        var buyDict = transactions.ToDictionary(t => t.Id);

        var dtos = transactions.Select(t =>
        {
            if (t.TransactionType == TransactionType.Sell)
            {
                var txAllocs       = allAllocations.Where(a => a.SellTransactionId == t.Id).ToList();
                var totalAllocQty  = txAllocs.Sum(a => a.AllocatedQuantity);
                var totalBuyCost   = txAllocs.Sum(a => a.AllocatedQuantity * a.BuyPrice);
                var avgBuyPrice    = totalAllocQty > 0 ? totalBuyCost / totalAllocQty : 0m;
                var taxableProfit  = (t.PurchasePrice - avgBuyPrice) * t.Quantity;
                var exitTaxDue     = taxRate.HasValue ? taxableProfit * (taxRate.Value / 100m) : (decimal?)null;

                return new TransactionDto
                {
                    Id              = t.Id,
                    HoldingId       = t.HoldingId,
                    TransactionType = "Sell",
                    Quantity        = t.Quantity,
                    PurchasePrice   = t.PurchasePrice,
                    PurchaseDate    = t.PurchaseDate,
                    CreatedAt       = t.CreatedAt,
                    CurrentPrice    = currentPrice,
                    VariationEur    = 0,
                    VariationPercent = 0,
                    TaxableProfitEur = taxableProfit,
                    ExitTaxPercent   = taxRate,
                    ExitTaxDueEur    = exitTaxDue,
                    Allocations      = txAllocs.Select(a => new SellAllocationDto
                    {
                        BuyTransactionId = a.BuyTransactionId,
                        BuyDate          = buyDict.TryGetValue(a.BuyTransactionId, out var bt) ? bt.PurchaseDate : default,
                        BuyPrice         = a.BuyPrice,
                        AllocatedQuantity = a.AllocatedQuantity,
                        ProfitEur        = (t.PurchasePrice - a.BuyPrice) * a.AllocatedQuantity
                    }).ToList()
                };
            }
            else // Buy
            {
                var variationEur     = (currentPrice - t.PurchasePrice) * t.Quantity;
                var variationPercent = t.PurchasePrice > 0
                    ? ((currentPrice - t.PurchasePrice) / t.PurchasePrice) * 100
                    : 0;

                return new TransactionDto
                {
                    Id               = t.Id,
                    HoldingId        = t.HoldingId,
                    TransactionType  = "Buy",
                    Quantity         = t.Quantity,
                    PurchasePrice    = t.PurchasePrice,
                    PurchaseDate     = t.PurchaseDate,
                    CreatedAt        = t.CreatedAt,
                    CurrentPrice     = currentPrice,
                    VariationEur     = variationEur,
                    VariationPercent = variationPercent,
                    TaxableProfitEur = null,
                    ExitTaxPercent   = null,
                    ExitTaxDueEur    = null,
                    Allocations      = null
                };
            }
        }).ToList();

        return dtos;
    }

    // ── Add Transaction ───────────────────────────────────────────────────────

    public async Task AddTransactionAsync(int userId, CreateTransactionDto dto, CancellationToken cancellationToken = default)
    {
        using var dbTx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var utcNow = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);

            // Get or create holding
            var holding = await _context.Holdings
                .FirstOrDefaultAsync(h => h.UserId == userId && h.Ticker == dto.Ticker, cancellationToken);

            if (holding == null)
            {
                if (dto.TransactionType == TransactionType.Sell)
                    throw new InvalidOperationException($"No existing holding found for {dto.Ticker}. Cannot sell units you don't own.");

                var etfName      = await _priceService.GetEtfDescriptionAsync(dto.Ticker, cancellationToken);
                var securityType = await _priceService.GetSecurityTypeAsync(dto.Ticker, cancellationToken);

                holding = new Holding
                {
                    UserId       = userId,
                    Ticker       = dto.Ticker,
                    EtfName      = etfName,
                    SecurityType = securityType,
                    Quantity     = 0,
                    AverageCost  = 0,
                    CreatedAt    = utcNow,
                    UpdatedAt    = utcNow
                };
                _context.Holdings.Add(holding);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Validate sell quantity
            if (dto.TransactionType == TransactionType.Sell && dto.Quantity > holding.Quantity)
                throw new InvalidOperationException(
                    $"Cannot sell {dto.Quantity} units; only {holding.Quantity} available for {dto.Ticker}.");

            // Record transaction
            var newTxn = new Transaction
            {
                HoldingId       = holding.Id,
                TransactionType = dto.TransactionType,
                Quantity        = dto.Quantity,
                PurchasePrice   = dto.PurchasePrice,
                PurchaseDate    = dto.PurchaseDate,
                CreatedAt       = utcNow,
                UpdatedAt       = utcNow
            };
            _context.Transactions.Add(newTxn);
            await _context.SaveChangesAsync(cancellationToken);

            // FIFO allocation for sells
            if (dto.TransactionType == TransactionType.Sell)
                await AllocateSellFifoAsync(newTxn.Id, holding.Id, dto.Quantity, cancellationToken);

            // Recalculate holding
            await RecalculateHoldingAsync(holding.Id, utcNow, cancellationToken);

            await dbTx.CommitAsync(cancellationToken);

            // Backfill historical prices (outside DB transaction — failure is non-fatal)
            if (dto.TransactionType == TransactionType.Buy)
                await BackfillHistoricalPricesIfNeededAsync(dto.Ticker, dto.PurchaseDate, cancellationToken);
        }
        catch (Exception ex)
        {
            await dbTx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error adding transaction");
            throw;
        }
    }

    // ── Delete Transaction ────────────────────────────────────────────────────

    public async Task DeleteTransactionAsync(int transactionId, int userId, CancellationToken cancellationToken = default)
    {
        using var dbTx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var txn = await _context.Transactions
                .Include(t => t.Holding)
                .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

            if (txn == null || txn.Holding?.UserId != userId)
                throw new UnauthorizedAccessException("Transaction not found or access denied.");

            var holdingId = txn.HoldingId;
            var utcNow    = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);

            if (txn.TransactionType == TransactionType.Buy)
            {
                // Block deletion if this buy has been consumed by any sell
                var hasAllocations = await _context.SellAllocations
                    .AnyAsync(a => a.BuyTransactionId == transactionId, cancellationToken);
                if (hasAllocations)
                    throw new InvalidOperationException(
                        "Cannot delete a buy that has been partially or fully sold — delete the linked sell transactions first.");
            }
            else // Sell — remove FIFO allocations first
            {
                var allocations = await _context.SellAllocations
                    .Where(a => a.SellTransactionId == transactionId)
                    .ToListAsync(cancellationToken);
                _context.SellAllocations.RemoveRange(allocations);
                await _context.SaveChangesAsync(cancellationToken);
            }

            _context.Transactions.Remove(txn);
            await _context.SaveChangesAsync(cancellationToken);

            var remaining = await _context.Transactions
                .Where(t => t.HoldingId == holdingId)
                .ToListAsync(cancellationToken);

            var holding = await _context.Holdings.FindAsync(new object[] { holdingId }, cancellationToken);
            if (holding != null)
            {
                if (remaining.Count == 0)
                {
                    _context.Holdings.Remove(holding);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    await RecalculateHoldingAsync(holdingId, utcNow, cancellationToken);
                }
            }

            await dbTx.CommitAsync(cancellationToken);
        }
        catch
        {
            await dbTx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // ── Update Transaction ────────────────────────────────────────────────────

    public async Task UpdateTransactionAsync(int transactionId, int userId, UpdateTransactionDto dto, CancellationToken cancellationToken = default)
    {
        using var dbTx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var txn = await _context.Transactions
                .Include(t => t.Holding)
                .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

            if (txn == null || txn.Holding?.UserId != userId)
                throw new UnauthorizedAccessException("Transaction not found or access denied.");

            var utcNow    = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);
            var holdingId = txn.HoldingId;

            txn.Quantity      = dto.Quantity;
            txn.PurchasePrice = dto.PurchasePrice;
            txn.PurchaseDate  = dto.PurchaseDate;
            txn.UpdatedAt     = utcNow;
            _context.Transactions.Update(txn);
            await _context.SaveChangesAsync(cancellationToken);

            if (txn.TransactionType == TransactionType.Buy)
            {
                // Find all sells that consumed this buy and re-run their FIFO allocations
                var affectedSellIds = await _context.SellAllocations
                    .Where(a => a.BuyTransactionId == transactionId)
                    .Select(a => a.SellTransactionId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (affectedSellIds.Count > 0)
                {
                    var affectedSells = await _context.Transactions
                        .Where(t => affectedSellIds.Contains(t.Id))
                        .OrderBy(t => t.PurchaseDate)
                        .ThenBy(t => t.Id)
                        .ToListAsync(cancellationToken);

                    foreach (var sell in affectedSells)
                    {
                        var existing = await _context.SellAllocations
                            .Where(a => a.SellTransactionId == sell.Id)
                            .ToListAsync(cancellationToken);
                        _context.SellAllocations.RemoveRange(existing);
                        await _context.SaveChangesAsync(cancellationToken);

                        await AllocateSellFifoAsync(sell.Id, holdingId, sell.Quantity, cancellationToken);
                    }
                }
            }

            await RecalculateHoldingAsync(holdingId, utcNow, cancellationToken);
            await dbTx.CommitAsync(cancellationToken);
        }
        catch
        {
            await dbTx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // ── Portfolio Evolution ───────────────────────────────────────────────────

    public async Task<PortfolioEvolutionDto> GetPortfolioEvolutionAsync(int userId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var dbHoldings = await _context.Holdings
            .Where(h => h.UserId == userId)
            .Include(h => h.Transactions)
            .ToListAsync(cancellationToken);

        var allTransactions = dbHoldings.SelectMany(h => h.Transactions).ToList();

        var startDate = allTransactions.Count > 0
            ? allTransactions.Min(t => t.PurchaseDate)
            : today;

        var tickers = dbHoldings.Select(h => h.Ticker).Distinct().ToList();

        var snapshotStart = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(-30);
        var snapshotEnd   = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59, DateTimeKind.Utc);

        var allSnapshots = await _context.PriceSnapshots
            .Where(ps => tickers.Contains(ps.Ticker) && ps.SnapshotDate >= snapshotStart && ps.SnapshotDate <= snapshotEnd)
            .ToListAsync(cancellationToken);

        var priceByTicker = allSnapshots
            .GroupBy(ps => ps.Ticker)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(ps => ps.SnapshotDate)
                      .Select(ps => (Date: DateOnly.FromDateTime(ps.SnapshotDate.Date), Price: ps.Price))
                      .ToList()
            );

        var buyDates  = allTransactions.Where(t => t.TransactionType == TransactionType.Buy) .Select(t => t.PurchaseDate).ToHashSet();
        var sellDates = allTransactions.Where(t => t.TransactionType == TransactionType.Sell).Select(t => t.PurchaseDate).ToHashSet();

        var dataPoints = new List<PortfolioEvolutionDataPointDto>();

        for (var date = startDate; date <= today; date = date.AddDays(1))
        {
            decimal totalValue = 0m;

            foreach (var holding in dbHoldings)
            {
                var buyQty  = holding.Transactions.Where(t => t.TransactionType == TransactionType.Buy  && t.PurchaseDate <= date).Sum(t => t.Quantity);
                var sellQty = holding.Transactions.Where(t => t.TransactionType == TransactionType.Sell && t.PurchaseDate <= date).Sum(t => t.Quantity);
                var quantity = buyQty - sellQty;

                if (quantity <= 0) continue;
                if (!priceByTicker.TryGetValue(holding.Ticker, out var prices)) continue;

                var price = prices
                    .Where(p => p.Date <= date)
                    .Select(p => (decimal?)p.Price)
                    .LastOrDefault();

                if (price.HasValue)
                    totalValue += quantity * price.Value;
            }

            dataPoints.Add(new PortfolioEvolutionDataPointDto
            {
                Date       = date.ToString("yyyy-MM-dd"),
                TotalValue = totalValue,
                HasBuy     = buyDates.Contains(date),
                HasSell    = sellDates.Contains(date)
            });
        }

        return new PortfolioEvolutionDto { DataPoints = dataPoints };
    }

    // ── Asset Tax Rates ───────────────────────────────────────────────────────

    public async Task<List<AssetTaxRateDto>> GetAssetTaxRatesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AssetTaxRates
            .OrderBy(r => r.SecurityType)
            .Select(r => new AssetTaxRateDto
            {
                SecurityType   = r.SecurityType,
                ExitTaxPercent = r.ExitTaxPercent,
                Label          = r.Label
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AssetTaxRateDto> UpsertAssetTaxRateAsync(AssetTaxRateDto dto, CancellationToken cancellationToken = default)
    {
        var utcNow   = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);
        var existing = await _context.AssetTaxRates.FindAsync(new object[] { dto.SecurityType }, cancellationToken);

        if (existing == null)
        {
            existing = new AssetTaxRate
            {
                SecurityType   = dto.SecurityType.ToUpperInvariant(),
                ExitTaxPercent = dto.ExitTaxPercent,
                Label          = dto.Label,
                CreatedAt      = utcNow,
                UpdatedAt      = utcNow
            };
            _context.AssetTaxRates.Add(existing);
        }
        else
        {
            existing.ExitTaxPercent = dto.ExitTaxPercent;
            existing.Label          = dto.Label;
            existing.UpdatedAt      = utcNow;
            _context.AssetTaxRates.Update(existing);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new AssetTaxRateDto { SecurityType = existing.SecurityType, ExitTaxPercent = existing.ExitTaxPercent, Label = existing.Label };
    }

    public async Task DeleteAssetTaxRateAsync(string securityType, CancellationToken cancellationToken = default)
    {
        var record = await _context.AssetTaxRates.FindAsync(new object[] { securityType }, cancellationToken);
        if (record == null) return;
        _context.AssetTaxRates.Remove(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ── Tax Year Summary ──────────────────────────────────────────────────────

    public async Task<List<int>> GetTaxYearsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var holdingIds = await _context.Holdings
            .Where(h => h.UserId == userId)
            .Select(h => h.Id)
            .ToListAsync(cancellationToken);

        if (holdingIds.Count == 0) return new List<int>();

        var years = await _context.Transactions
            .Where(t => holdingIds.Contains(t.HoldingId) && t.TransactionType == TransactionType.Sell)
            .Select(t => t.PurchaseDate.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(cancellationToken);

        return years;
    }

    public async Task<TaxYearSummaryDto> GetTaxYearSummaryAsync(int userId, int year, CancellationToken cancellationToken = default)
    {
        var holdingMeta = await _context.Holdings
            .Where(h => h.UserId == userId)
            .Select(h => new { h.Id, h.Ticker, h.EtfName, h.SecurityType })
            .ToListAsync(cancellationToken);

        var holdingMap = holdingMeta.ToDictionary(h => h.Id);
        var holdingIds = holdingMeta.Select(h => h.Id).ToList();

        if (holdingIds.Count == 0)
            return new TaxYearSummaryDto { Year = year };

        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd   = new DateOnly(year, 12, 31);

        var sells = await _context.Transactions
            .Where(t => holdingIds.Contains(t.HoldingId)
                     && t.TransactionType == TransactionType.Sell
                     && t.PurchaseDate >= yearStart
                     && t.PurchaseDate <= yearEnd)
            .OrderBy(t => t.PurchaseDate)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);

        if (sells.Count == 0)
            return new TaxYearSummaryDto { Year = year };

        var sellIds = sells.Select(s => s.Id).ToList();
        var allocations = await _context.SellAllocations
            .Where(a => sellIds.Contains(a.SellTransactionId))
            .ToListAsync(cancellationToken);

        var taxRates = await _context.AssetTaxRates
            .ToDictionaryAsync(r => r.SecurityType.ToUpperInvariant(), r => r.ExitTaxPercent, cancellationToken);

        var entries = new List<TaxSellEntryDto>();

        foreach (var sell in sells)
        {
            holdingMap.TryGetValue(sell.HoldingId, out var holding);

            var txAllocs       = allocations.Where(a => a.SellTransactionId == sell.Id).ToList();
            var totalAllocQty  = txAllocs.Sum(a => a.AllocatedQuantity);
            var totalBuyCost   = txAllocs.Sum(a => a.AllocatedQuantity * a.BuyPrice);
            var weightedBuy    = totalAllocQty > 0 ? totalBuyCost / totalAllocQty : 0m;
            var taxableProfit  = (sell.PurchasePrice - weightedBuy) * sell.Quantity;

            decimal? exitTaxPct = null;
            decimal? exitTaxDue = null;
            var secType = holding?.SecurityType?.ToUpperInvariant();
            if (secType != null && taxRates.TryGetValue(secType, out var rate))
            {
                exitTaxPct = rate;
                exitTaxDue = taxableProfit * (rate / 100m);
            }

            entries.Add(new TaxSellEntryDto
            {
                TransactionId    = sell.Id,
                Ticker           = holding?.Ticker ?? "—",
                EtfName          = holding?.EtfName,
                SellDate         = sell.PurchaseDate.ToString("yyyy-MM-dd"),
                QuantitySold     = sell.Quantity,
                SellPrice        = sell.PurchasePrice,
                WeightedBuyPrice = weightedBuy,
                TaxableProfit    = taxableProfit,
                ExitTaxPercent   = exitTaxPct,
                ExitTaxDue       = exitTaxDue,
                SecurityType     = holding?.SecurityType
            });
        }

        return new TaxYearSummaryDto
        {
            Year               = year,
            Entries            = entries,
            TotalTaxableProfit = entries.Sum(e => e.TaxableProfit),
            TotalExitTaxDue    = entries.Sum(e => e.ExitTaxDue ?? 0m),
            HasMissingRates    = entries.Any(e => e.ExitTaxPercent == null)
        };
    }

    // ── FIFO helpers ──────────────────────────────────────────────────────────

    private async Task AllocateSellFifoAsync(int sellTransactionId, int holdingId, decimal sellQty, CancellationToken cancellationToken)
    {
        var buys = await _context.Transactions
            .Where(t => t.HoldingId == holdingId && t.TransactionType == TransactionType.Buy)
            .OrderBy(t => t.PurchaseDate)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);

        var buyIds = buys.Select(b => b.Id).ToList();
        var alreadyAllocated = await _context.SellAllocations
            .Where(a => buyIds.Contains(a.BuyTransactionId))
            .GroupBy(a => a.BuyTransactionId)
            .Select(g => new { BuyId = g.Key, Qty = g.Sum(a => a.AllocatedQuantity) })
            .ToDictionaryAsync(x => x.BuyId, x => x.Qty, cancellationToken);

        var remaining    = sellQty;
        var newAllocations = new List<SellAllocation>();

        foreach (var buy in buys)
        {
            if (remaining <= 0) break;

            var consumed  = alreadyAllocated.GetValueOrDefault(buy.Id, 0m);
            var available = buy.Quantity - consumed;
            if (available <= 0) continue;

            var toAllocate = Math.Min(available, remaining);
            newAllocations.Add(new SellAllocation
            {
                SellTransactionId = sellTransactionId,
                BuyTransactionId  = buy.Id,
                AllocatedQuantity = toAllocate,
                BuyPrice          = buy.PurchasePrice
            });
            remaining -= toAllocate;
        }

        _context.SellAllocations.AddRange(newAllocations);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task RecalculateHoldingAsync(int holdingId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var allTxns = await _context.Transactions
            .Where(t => t.HoldingId == holdingId)
            .ToListAsync(cancellationToken);

        var buys     = allTxns.Where(t => t.TransactionType == TransactionType.Buy).ToList();
        var buyIds   = buys.Select(b => b.Id).ToList();
        var sellQty  = allTxns.Where(t => t.TransactionType == TransactionType.Sell).Sum(t => t.Quantity);

        var allocations = await _context.SellAllocations
            .Where(a => buyIds.Contains(a.BuyTransactionId))
            .GroupBy(a => a.BuyTransactionId)
            .Select(g => new { BuyId = g.Key, Qty = g.Sum(a => a.AllocatedQuantity) })
            .ToDictionaryAsync(x => x.BuyId, x => x.Qty, cancellationToken);

        decimal remainingCost = 0m;
        decimal remainingQty  = 0m;
        foreach (var buy in buys)
        {
            var allocated = allocations.GetValueOrDefault(buy.Id, 0m);
            var leftover  = buy.Quantity - allocated;
            if (leftover > 0)
            {
                remainingQty  += leftover;
                remainingCost += leftover * buy.PurchasePrice;
            }
        }

        var holding = await _context.Holdings.FindAsync(new object[] { holdingId }, cancellationToken);
        if (holding != null)
        {
            holding.Quantity    = buys.Sum(b => b.Quantity) - sellQty;
            holding.AverageCost = remainingQty > 0 ? remainingCost / remainingQty : 0m;
            holding.UpdatedAt   = utcNow;
            _context.Holdings.Update(holding);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    // ── Historical price backfill ─────────────────────────────────────────────

    private async Task BackfillHistoricalPricesIfNeededAsync(string ticker, DateOnly purchaseDate, CancellationToken cancellationToken)
    {
        try
        {
            var purchaseDateUtc = purchaseDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var earliestSnapshotDate = await _context.PriceSnapshots
                .Where(ps => ps.Ticker == ticker)
                .MinAsync(ps => (DateTime?)ps.SnapshotDate, cancellationToken);

            bool needsBackfill = earliestSnapshotDate == null
                                 || earliestSnapshotDate.Value.Date > purchaseDateUtc.Date;

            if (!needsBackfill) return;

            _logger.LogInformation("Starting historical price backfill for {Ticker} from {PurchaseDate}", ticker, purchaseDate);
            var saved = await _priceService.FetchAndSaveHistoricalPricesAsync(ticker, purchaseDate, cancellationToken);
            _logger.LogInformation("Backfill complete for {Ticker}: {Count} snapshots saved", ticker, saved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during historical price backfill for {Ticker}", ticker);
        }
    }

    // ── Period metric helpers ─────────────────────────────────────────────────

    private async Task<PeriodMetrics> CalculatePeriodMetricsAsync(string ticker, decimal quantity, int days, CancellationToken cancellationToken)
    {
        var currentPrice     = await _priceService.GetPriceAsync(ticker, cancellationToken);
        var priceFromDaysAgo = await GetPriceFromDaysAgoAsync(ticker, days, cancellationToken);

        if (!currentPrice.HasValue || !priceFromDaysAgo.HasValue)
            return new PeriodMetrics { GainLossEur = 0, GainLossPercent = 0, PricesUnavailable = true };

        var gainLossEur     = (currentPrice.Value - priceFromDaysAgo.Value) * quantity;
        var gainLossPercent = priceFromDaysAgo.Value > 0
            ? ((currentPrice.Value - priceFromDaysAgo.Value) / priceFromDaysAgo.Value) * 100
            : 0;

        return new PeriodMetrics { GainLossEur = gainLossEur, GainLossPercent = gainLossPercent, PricesUnavailable = false };
    }

    private async Task<PeriodMetrics> CalculateYtdMetricsAsync(string ticker, decimal quantity, CancellationToken cancellationToken)
    {
        var currentPrice     = await _priceService.GetPriceAsync(ticker, cancellationToken);
        var firstPriceOfYear = await GetFirstPriceOfYearAsync(ticker, cancellationToken);

        if (!currentPrice.HasValue || !firstPriceOfYear.HasValue)
            return new PeriodMetrics { GainLossEur = 0, GainLossPercent = 0, PricesUnavailable = true };

        var gainLossEur     = (currentPrice.Value - firstPriceOfYear.Value) * quantity;
        var gainLossPercent = firstPriceOfYear.Value > 0
            ? ((currentPrice.Value - firstPriceOfYear.Value) / firstPriceOfYear.Value) * 100
            : 0;

        return new PeriodMetrics { GainLossEur = gainLossEur, GainLossPercent = gainLossPercent, PricesUnavailable = false };
    }

    private async Task<decimal?> GetPriceFromDaysAgoAsync(string ticker, int days, CancellationToken cancellationToken)
    {
        var targetDate = DateTime.UtcNow.AddDays(-days).Date;

        var snapshot = await _context.PriceSnapshots
            .Where(ps => ps.Ticker == ticker && ps.SnapshotDate == targetDate)
            .OrderByDescending(ps => ps.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot != null) return snapshot.Price;

        snapshot = await _context.PriceSnapshots
            .Where(ps => ps.Ticker == ticker && ps.SnapshotDate < targetDate)
            .OrderByDescending(ps => ps.SnapshotDate)
            .FirstOrDefaultAsync(cancellationToken);

        return snapshot?.Price;
    }

    private async Task<decimal?> GetFirstPriceOfYearAsync(string ticker, CancellationToken cancellationToken)
    {
        var currentYear = DateTime.UtcNow.Year;
        var snapshot = await _context.PriceSnapshots
            .Where(ps => ps.Ticker == ticker && ps.SnapshotDate.Year == currentYear)
            .OrderBy(ps => ps.SnapshotDate)
            .FirstOrDefaultAsync(cancellationToken);

        return snapshot?.Price;
    }

    private decimal CalculateWeightedPercentage(List<HoldingDto> holdings, Func<HoldingDto, decimal> percentSelector, Func<HoldingDto, decimal> weightSelector)
    {
        var totalWeight = holdings.Sum(weightSelector);
        if (totalWeight <= 0) return 0;
        return holdings.Sum(h => (percentSelector(h) / 100m) * weightSelector(h)) / totalWeight * 100;
    }

    private static bool IsSubjectToDeemedDisposal(string? securityType)
        => securityType?.ToUpperInvariant() is "ETF" or "MUTUALFUND" or "FUND";
}
