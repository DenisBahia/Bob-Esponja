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
    Task<ImportTransactionsResultDto> ImportTransactionsAsync(int userId, ImportTransactionsRequestDto dto, CancellationToken cancellationToken = default);
}

public class HoldingsService : IHoldingsService
{
    private readonly AppDbContext _context;
    private readonly IPriceService _priceService;
    private readonly IDeemedDisposalService _deemedDisposalService;
    private readonly ISellService _sellService;
    private readonly ILogger<HoldingsService> _logger;

    public HoldingsService(AppDbContext context, IPriceService priceService,
        IDeemedDisposalService deemedDisposalService, ISellService sellService, ILogger<HoldingsService> logger)
    {
        _context = context;
        _priceService = priceService;
        _deemedDisposalService = deemedDisposalService;
        _sellService = sellService;
        _logger = logger;
    }

    public async Task<DashboardDto> GetDashboardAsync(int userId, CancellationToken cancellationToken = default)
    {
        var holdings = await GetHoldingsAsync(userId, cancellationToken);
        
        var dashboard = new DashboardDto();
        
        // Calculate header metrics
        var totalValue = holdings.Sum(h => h.TotalValue);
        var totalInvested = holdings.Sum(h => h.Quantity * h.AverageCost);
        dashboard.Header.TotalHoldingsAmount = totalValue;
        
        // Calculate total variation (current total value - invested amount)
        var totalVariationEur = totalValue - totalInvested;
        var totalVariationPercent = totalInvested > 0 
            ? (totalVariationEur / totalInvested) * 100 
            : 0;
        dashboard.Header.TotalVariation = new PeriodMetrics
        {
            GainLossEur = totalVariationEur,
            GainLossPercent = totalVariationPercent
        };
        
        dashboard.Header.DailyMetrics = new PeriodMetrics
        {
            GainLossEur = holdings.Sum(h => h.DailyMetrics.GainLossEur),
            GainLossPercent = CalculateWeightedPercentage(holdings, h => h.DailyMetrics.GainLossPercent, h => h.TotalValue),
            PricesUnavailable = holdings.Any(h => h.DailyMetrics.PricesUnavailable)
        };

        dashboard.Header.WeeklyMetrics = new PeriodMetrics
        {
            GainLossEur = holdings.Sum(h => h.WeeklyMetrics.GainLossEur),
            GainLossPercent = CalculateWeightedPercentage(holdings, h => h.WeeklyMetrics.GainLossPercent, h => h.TotalValue),
            PricesUnavailable = holdings.Any(h => h.WeeklyMetrics.PricesUnavailable)
        };

        dashboard.Header.MonthlyMetrics = new PeriodMetrics
        {
            GainLossEur = holdings.Sum(h => h.MonthlyMetrics.GainLossEur),
            GainLossPercent = CalculateWeightedPercentage(holdings, h => h.MonthlyMetrics.GainLossPercent, h => h.TotalValue),
            PricesUnavailable = holdings.Any(h => h.MonthlyMetrics.PricesUnavailable)
        };

        dashboard.Header.YtdMetrics = new PeriodMetrics
        {
            GainLossEur = holdings.Sum(h => h.YtdMetrics.GainLossEur),
            GainLossPercent = CalculateWeightedPercentage(holdings, h => h.YtdMetrics.GainLossPercent, h => h.TotalValue),
            PricesUnavailable = holdings.Any(h => h.YtdMetrics.PricesUnavailable)
        };

        dashboard.Holdings = holdings;
        return dashboard;
    }

    public async Task<List<HoldingDto>> GetHoldingsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var dbHoldings = await _context.Holdings
            .Where(h => h.UserId == userId)
            .Include(h => h.Transactions)
            .OrderBy(h => h.Ticker)
            .ToListAsync(cancellationToken);

        // Keep deemed-disposal events up to date for Irish users even when no new buy is added.
        await EnsureDeemedDisposalsAreCurrentAsync(userId, dbHoldings, cancellationToken);

        // Pre-load sell data for all holdings in one go
        var holdingIds = dbHoldings.Select(h => h.Id).ToList();

        // Tax summary per holding from tax_events (single source of truth going forward)
        var taxEventsByHolding = await _context.TaxEvents
            .Where(te => holdingIds.Contains(te.HoldingId))
            .GroupBy(te => te.HoldingId)
            .Select(g => new
            {
                HoldingId = g.Key,
                TotalPaid = g.Where(te => te.Status == TaxEventStatus.Paid).Sum(te => te.TaxAmount),
                TotalPending = g.Where(te => te.Status == TaxEventStatus.Pending).Sum(te => te.TaxAmount),
                TotalExitTaxPending = g.Where(te => te.Status == TaxEventStatus.Pending && te.TaxSubType == "ExitTax").Sum(te => te.TaxAmount),
                TotalCgtPending = g.Where(te => te.Status == TaxEventStatus.Pending && te.TaxSubType == "CGT").Sum(te => te.TaxAmount)
            })
            .ToDictionaryAsync(x => x.HoldingId, cancellationToken);

        // Total consumed quantity per buy transaction id
        var txnIds = dbHoldings.SelectMany(h => h.Transactions.Select(t => t.Id)).ToList();
        var consumedByTxn = await _context.SellLotAllocations
            .Where(sla => txnIds.Contains(sla.BuyTransactionId))
            .GroupBy(sla => sla.BuyTransactionId)
            .Select(g => new { TxnId = g.Key, Consumed = g.Sum(sla => sla.QuantityConsumed) })
            .ToDictionaryAsync(x => x.TxnId, x => x.Consumed, cancellationToken);

        // Next deemed-disposal date per holding (only for holdings that have at least one buy with DeemedDisposalDue = true)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nextDeemedDisposalByHolding = new Dictionary<int, DateOnly?>();
        foreach (var holding in dbHoldings)
        {
            DateOnly? next = null;
            foreach (var t in holding.Transactions)
            {
                if (!t.DeemedDisposalDue) continue; // Only deemed-disposal buys attract the 8-year rule
                // Skip fully sold lots — they no longer attract deemed disposal
                var consumed = consumedByTxn.TryGetValue(t.Id, out var c) ? c : 0m;
                if (t.Quantity - consumed <= 0m) continue;

                for (int years = 8; ; years += 8)
                {
                    DateOnly ann;
                    try { ann = new DateOnly(t.PurchaseDate.Year + years, t.PurchaseDate.Month, t.PurchaseDate.Day); }
                    catch { ann = new DateOnly(t.PurchaseDate.Year + years, t.PurchaseDate.Month,
                        Math.Min(t.PurchaseDate.Day, DateTime.DaysInMonth(t.PurchaseDate.Year + years, t.PurchaseDate.Month))); }

                    if (ann > today)
                    {
                        if (!next.HasValue || ann < next.Value) next = ann;
                        break;
                    }
                }
            }
            nextDeemedDisposalByHolding[holding.Id] = next;
        }


        var holdingDtos = new List<HoldingDto>();

        foreach (var holding in dbHoldings)
        {
            var priceResult = await _priceService.GetPriceWithSourceAsync(holding.Ticker, cancellationToken);
            var priceUnavailable = !priceResult.Price.HasValue;
            var displayPrice = priceResult.Price ?? 0;
            var totalValue = holding.Quantity * displayPrice;

            // Update holding with price source
            if (priceResult.Source != null)
            {
                holding.PriceSource = priceResult.Source;
                _context.Holdings.Update(holding);
            }

            // Compute available quantity from FIFO remainder
            decimal availableQty = holding.Transactions.Sum(t =>
            {
                var consumed = consumedByTxn.TryGetValue(t.Id, out var c) ? c : 0m;
                return Math.Max(0m, t.Quantity - consumed);
            });

            var holdingDto = new HoldingDto
            {
                Id = holding.Id,
                Ticker = holding.Ticker,
                EtfName = holding.EtfName,
                Quantity = holding.Quantity,
                AverageCost = holding.AverageCost,
                CurrentPrice = displayPrice,
                TotalValue = totalValue,
                PriceUnavailable = priceUnavailable,
                PriceSource = priceResult.Source,
                TotalTaxPaid = taxEventsByHolding.TryGetValue(holding.Id, out var taxData) ? taxData.TotalPaid : 0m,
                TotalTaxPending = taxEventsByHolding.TryGetValue(holding.Id, out var taxData2) ? taxData2.TotalPending : 0m,
                TotalExitTaxPending = taxEventsByHolding.TryGetValue(holding.Id, out var taxData3) ? taxData3.TotalExitTaxPending : 0m,
                TotalCgtPending = taxEventsByHolding.TryGetValue(holding.Id, out var taxData4) ? taxData4.TotalCgtPending : 0m,
                AvailableQuantity = availableQty,
                NextDeemedDisposalDate = nextDeemedDisposalByHolding.TryGetValue(holding.Id, out var nd) ? nd : null,
                DailyMetrics = await CalculatePeriodMetricsAsync(holding.Ticker, holding.Quantity, 1, cancellationToken),
                WeeklyMetrics = await CalculatePeriodMetricsAsync(holding.Ticker, holding.Quantity, 7, cancellationToken),
                MonthlyMetrics = await CalculatePeriodMetricsAsync(holding.Ticker, holding.Quantity, 30, cancellationToken),
                YtdMetrics = await CalculateYtdMetricsAsync(holding.Ticker, holding.Quantity, cancellationToken)
            };

            holdingDtos.Add(holdingDto);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return holdingDtos;
    }

    public async Task<List<TransactionDto>> GetHoldingHistoryAsync(int holdingId, CancellationToken cancellationToken = default)
    {
        // Get the holding to find the ticker
        var holding = await _context.Holdings
            .FirstOrDefaultAsync(h => h.Id == holdingId, cancellationToken);

        if (holding == null)
        {
            return new List<TransactionDto>();
        }

        // Get current price for the ticker
        var currentPrice = await _priceService.GetPriceAsync(holding.Ticker, cancellationToken) ?? 0;

        // Get all transactions
        var transactions = await _context.Transactions
            .Where(t => t.HoldingId == holdingId)
            .OrderByDescending(t => t.PurchaseDate)
            .ToListAsync(cancellationToken);

        // Map to DTOs with current price and variations
        var transactionDtos = transactions.Select(t => 
        {
            var variationEur = (currentPrice - t.PurchasePrice) * t.Quantity;
            var variationPercent = t.PurchasePrice > 0 
                ? ((currentPrice - t.PurchasePrice) / t.PurchasePrice) * 100 
                : 0;

            return new TransactionDto
            {
                Id = t.Id,
                HoldingId = t.HoldingId,
                Quantity = t.Quantity,
                PurchasePrice = t.PurchasePrice,
                PurchaseDate = t.PurchaseDate,
                CreatedAt = t.CreatedAt,
                CurrentPrice = currentPrice,
                VariationEur = variationEur,
                VariationPercent = variationPercent,
                DeemedDisposalDue = t.DeemedDisposalDue
            };
        }).ToList();

        return transactionDtos;
    }

    public async Task AddTransactionAsync(int userId, CreateTransactionDto dto, CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var utcNow = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);
            
            // Get or create holding
            var holding = await _context.Holdings
                .FirstOrDefaultAsync(h => h.UserId == userId && h.Ticker == dto.Ticker, cancellationToken);

            if (holding == null)
            {
                // Fetch ETF name/description
                var etfName = await _priceService.GetEtfDescriptionAsync(dto.Ticker, cancellationToken);
                
                holding = new Holding
                {
                    UserId = userId,
                    Ticker = dto.Ticker,
                    EtfName = etfName,
                    Quantity = 0,
                    AverageCost = 0,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow
                };
                _context.Holdings.Add(holding);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Add transaction
            var newTransaction = new Transaction
            {
                HoldingId = holding.Id,
                Quantity = dto.Quantity,
                PurchasePrice = dto.PurchasePrice,
                PurchaseDate = dto.PurchaseDate,
                DeemedDisposalDue = dto.DeemedDisposalDue,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };
            _context.Transactions.Add(newTransaction);
            await _context.SaveChangesAsync(cancellationToken);

            // Recalculate from remaining (unsold) quantities so prior sells stay respected.
            await RecalculateHoldingFromRemainingLotsAsync(holding, cancellationToken);
            holding.UpdatedAt = utcNow;
            
            _context.Holdings.Update(holding);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            // ── Backfill historical prices for this ticker if needed ──────────────
            // Awaited here (outside the DB transaction) so the scoped AppDbContext is
            // still alive when the Yahoo Finance call and subsequent SaveChanges run.
            // Any failure is swallowed inside BackfillHistoricalPricesIfNeededAsync,
            // so the buy is never rolled back even if the history fetch fails.
            var hasPriceHistoryCoverage = await BackfillHistoricalPricesIfNeededAsync(
                dto.Ticker, dto.PurchaseDate, cancellationToken);

            // Check for deemed-disposal events triggered by this buy's purchase date
            if (dto.IsIrishInvestor)
            {
                if (!hasPriceHistoryCoverage)
                {
                    _logger.LogWarning(
                        "Skipping deemed-disposal check for holding {HoldingId}: missing historical prices covering buy date {PurchaseDate}.",
                        holding.Id, dto.PurchaseDate);
                }
                else
                {
                    try
                    {
                        await _deemedDisposalService.CheckAndCreateDeemedDisposalEventsAsync(
                            holding.Id, userId, dto.IsIrishInvestor, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Deemed-disposal check failed for holding {HoldingId} — non-fatal.", holding.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error adding transaction");
            throw;
        }
    }

    /// <summary>
    /// Checks whether price snapshots for <paramref name="ticker"/> already cover
    /// <paramref name="purchaseDate"/>.  If not, fetches the full daily history from
    /// Yahoo Finance (period1 = purchaseDate, period2 = today) and saves it to the DB.
    /// </summary>
    private async Task<bool> BackfillHistoricalPricesIfNeededAsync(
        string ticker,
        DateOnly purchaseDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var purchaseDateUtc = purchaseDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            // Find the earliest existing snapshot for this ticker
            var earliestSnapshotDate = await _context.PriceSnapshots
                .Where(ps => ps.Ticker == ticker)
                .MinAsync(ps => (DateTime?)ps.SnapshotDate, cancellationToken);

            // Backfill when: no snapshots at all OR purchase date predates earliest snapshot
            bool needsBackfill = earliestSnapshotDate == null
                                 || earliestSnapshotDate.Value.Date > purchaseDateUtc.Date;

            if (needsBackfill)
            {
                _logger.LogInformation(
                    "Starting historical price backfill for {Ticker} from {PurchaseDate}",
                    ticker, purchaseDate);

                var saved = await _priceService.FetchAndSaveHistoricalPricesAsync(ticker, purchaseDate, cancellationToken);

                _logger.LogInformation(
                    "Historical price backfill complete for {Ticker}: {Count} snapshots saved",
                    ticker, saved);
            }
            else
            {
                _logger.LogDebug(
                    "Price history for {Ticker} already covers {PurchaseDate} — skipping backfill",
                    ticker, purchaseDate);
            }

            // Re-check coverage after optional backfill; deemed-disposal depends on this.
            var earliestAfterBackfill = await _context.PriceSnapshots
                .Where(ps => ps.Ticker == ticker)
                .MinAsync(ps => (DateTime?)ps.SnapshotDate, cancellationToken);

            var hasCoverage = earliestAfterBackfill != null
                              && earliestAfterBackfill.Value.Date <= purchaseDateUtc.Date;

            if (!hasCoverage)
            {
                _logger.LogWarning(
                    "Historical prices for {Ticker} still do not cover buy date {PurchaseDate} after backfill attempt.",
                    ticker, purchaseDate);
            }

            return hasCoverage;
        }
        catch (Exception ex)
        {
            // Never surface this error to the caller — the buy was already committed successfully
            _logger.LogError(ex,
                "Error during historical price backfill for {Ticker} (purchase date {PurchaseDate})",
                ticker, purchaseDate);
            return false;
        }
    }

    private async Task RecalculateHoldingFromRemainingLotsAsync(Holding holding, CancellationToken cancellationToken)
    {
        var allTransactions = await _context.Transactions
            .Where(t => t.HoldingId == holding.Id)
            .ToListAsync(cancellationToken);

        if (allTransactions.Count == 0)
        {
            holding.Quantity = 0;
            holding.AverageCost = 0;
            return;
        }

        var txnIds = allTransactions.Select(t => t.Id).ToList();
        var consumedByTxn = await _context.SellLotAllocations
            .Where(sla => txnIds.Contains(sla.BuyTransactionId))
            .GroupBy(sla => sla.BuyTransactionId)
            .Select(g => new { TxnId = g.Key, Consumed = g.Sum(sla => sla.QuantityConsumed) })
            .ToDictionaryAsync(x => x.TxnId, x => x.Consumed, cancellationToken);

        decimal remainingQty = 0m;
        decimal remainingCost = 0m;

        foreach (var txn in allTransactions)
        {
            var consumed = consumedByTxn.TryGetValue(txn.Id, out var c) ? c : 0m;
            var remaining = Math.Max(0m, txn.Quantity - consumed);
            if (remaining <= 0m) continue;

            remainingQty += remaining;
            remainingCost += remaining * txn.PurchasePrice;
        }

        holding.Quantity = remainingQty;
        holding.AverageCost = remainingQty > 0m ? remainingCost / remainingQty : 0m;
    }

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

            // Prevent deletion of a buy that has been (partially) sold
            var hasAllocations = await _context.SellLotAllocations
                .AnyAsync(sla => sla.BuyTransactionId == transactionId, cancellationToken);
            if (hasAllocations)
                throw new InvalidOperationException(
                    "Cannot delete this buy — it has been (partially) sold. Delete the sell record first.");

            var holdingId = txn.HoldingId;
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
                }
                else
                {
                    await RecalculateHoldingFromRemainingLotsAsync(holding, cancellationToken);
                    holding.UpdatedAt = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);
                    _context.Holdings.Update(holding);
                }
                await _context.SaveChangesAsync(cancellationToken);
            }

            await dbTx.CommitAsync(cancellationToken);
        }
        catch
        {
            await dbTx.RollbackAsync(cancellationToken);
            throw;
        }
    }

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

            txn.Quantity = dto.Quantity;
            txn.PurchasePrice = dto.PurchasePrice;
            txn.PurchaseDate = dto.PurchaseDate;
            txn.UpdatedAt = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);
            _context.Transactions.Update(txn);
            await _context.SaveChangesAsync(cancellationToken);

            // Recalculate holding averages
            var holdingId = txn.HoldingId;
            var allTxns = await _context.Transactions
                .Where(t => t.HoldingId == holdingId)
                .ToListAsync(cancellationToken);

            var holding = await _context.Holdings.FindAsync(new object[] { holdingId }, cancellationToken);
            if (holding != null)
            {
                await RecalculateHoldingFromRemainingLotsAsync(holding, cancellationToken);
                holding.UpdatedAt = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);
                _context.Holdings.Update(holding);
                await _context.SaveChangesAsync(cancellationToken);
            }

            await dbTx.CommitAsync(cancellationToken);
        }
        catch
        {
            await dbTx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PortfolioEvolutionDto> GetPortfolioEvolutionAsync(int userId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        // Load all holdings (and their transactions) for the user
        var dbHoldings = await _context.Holdings
            .Where(h => h.UserId == userId)
            .Include(h => h.Transactions)
            .ToListAsync(cancellationToken);

        // Load all sells for those holdings once (used to decrement quantity over time)
        var holdingIds = dbHoldings.Select(h => h.Id).ToList();
        var sellRecords = await _context.SellRecords
            .Where(sr => holdingIds.Contains(sr.HoldingId))
            .Select(sr => new { sr.HoldingId, sr.SellDate, sr.Quantity })
            .ToListAsync(cancellationToken);

        // Derive start date from the user's earliest transaction (so backfilled history is visible)
        var allTransactionDates = dbHoldings
            .SelectMany(h => h.Transactions)
            .Select(t => t.PurchaseDate)
            .ToList();

        var startDate = allTransactionDates.Count > 0
            ? allTransactionDates.Min()
            : today;

        // Collect all distinct tickers
        var tickers = dbHoldings.Select(h => h.Ticker).Distinct().ToList();

        // Load all price snapshots for those tickers in the date range (include a bit before start for carry-forward)
        var snapshotStart = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(-30);
        var snapshotEnd = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59, DateTimeKind.Utc);

        var allSnapshots = await _context.PriceSnapshots
            .Where(ps => tickers.Contains(ps.Ticker) && ps.SnapshotDate >= snapshotStart && ps.SnapshotDate <= snapshotEnd)
            .ToListAsync(cancellationToken);

        // Build a lookup: ticker -> sorted list of (date, price)
        var priceByTicker = allSnapshots
            .GroupBy(ps => ps.Ticker)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(ps => ps.SnapshotDate)
                      .Select(ps => (Date: DateOnly.FromDateTime(ps.SnapshotDate.Date), Price: ps.Price))
                      .ToList()
            );

        // Collect all buy dates
        var allTransactions = dbHoldings.SelectMany(h => h.Transactions).ToList();
        var buyDates = allTransactions.Select(t => t.PurchaseDate).ToHashSet();

        var dataPoints = new List<PortfolioEvolutionDataPointDto>();

        for (var date = startDate; date <= today; date = date.AddDays(1))
        {
            decimal totalValue = 0m;

            foreach (var holding in dbHoldings)
            {
                // Quantity held as of this date = buys up to date minus sells up to date
                var boughtQty = holding.Transactions
                    .Where(t => t.PurchaseDate <= date)
                    .Sum(t => t.Quantity);
                var soldQty = sellRecords
                    .Where(sr => sr.HoldingId == holding.Id && sr.SellDate <= date)
                    .Sum(sr => sr.Quantity);
                var quantity = Math.Max(0m, boughtQty - soldQty);

                if (quantity <= 0) continue;

                // Find the best price: exact match or latest snapshot on/before this date
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
                Date = date.ToString("yyyy-MM-dd"),
                TotalValue = totalValue,
                HasBuy = buyDates.Contains(date)
            });
        }

        return new PortfolioEvolutionDto { DataPoints = dataPoints };
    }

    private async Task<PeriodMetrics> CalculatePeriodMetricsAsync(string ticker, decimal quantity, int days, CancellationToken cancellationToken)
    {
        var currentPrice = await _priceService.GetPriceAsync(ticker, cancellationToken);
        var priceFromDaysAgo = await GetPriceFromDaysAgoAsync(ticker, days, cancellationToken);

        // Return zero metrics with flag if current price or historical price is unavailable
        if (!currentPrice.HasValue || !priceFromDaysAgo.HasValue)
        {
            return new PeriodMetrics { GainLossEur = 0, GainLossPercent = 0, PricesUnavailable = true };
        }

        var gainLossEur = (currentPrice.Value - priceFromDaysAgo.Value) * quantity;
        var gainLossPercent = priceFromDaysAgo.Value > 0 
            ? ((currentPrice.Value - priceFromDaysAgo.Value) / priceFromDaysAgo.Value) * 100 
            : 0;

        return new PeriodMetrics
        {
            GainLossEur = gainLossEur,
            GainLossPercent = gainLossPercent,
            PricesUnavailable = false
        };
    }

    private async Task<PeriodMetrics> CalculateYtdMetricsAsync(string ticker, decimal quantity, CancellationToken cancellationToken)
    {
        var currentPrice = await _priceService.GetPriceAsync(ticker, cancellationToken);
        var firstPriceOfYear = await GetFirstPriceOfYearAsync(ticker, cancellationToken);

        // Return zero metrics with flag if current price or first price of year is unavailable
        if (!currentPrice.HasValue || !firstPriceOfYear.HasValue)
        {
            return new PeriodMetrics { GainLossEur = 0, GainLossPercent = 0, PricesUnavailable = true };
        }

        var gainLossEur = (currentPrice.Value - firstPriceOfYear.Value) * quantity;
        var gainLossPercent = firstPriceOfYear.Value > 0 
            ? ((currentPrice.Value - firstPriceOfYear.Value) / firstPriceOfYear.Value) * 100 
            : 0;

        return new PeriodMetrics
        {
            GainLossEur = gainLossEur,
            GainLossPercent = gainLossPercent,
            PricesUnavailable = false
        };
    }

    private async Task<decimal?> GetPriceFromDaysAgoAsync(string ticker, int days, CancellationToken cancellationToken)
    {
        var targetDate = DateTime.UtcNow.AddDays(-days).Date;
        
        // Try to get exact date first
        var snapshot = await _context.PriceSnapshots
            .Where(ps => ps.Ticker == ticker && ps.SnapshotDate == targetDate)
            .OrderByDescending(ps => ps.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot != null)
            return snapshot.Price;

        // If not found, get the closest earlier snapshot
        snapshot = await _context.PriceSnapshots
            .Where(ps => ps.Ticker == ticker && ps.SnapshotDate < targetDate)
            .OrderByDescending(ps => ps.SnapshotDate)
            .FirstOrDefaultAsync(cancellationToken);

        return snapshot?.Price;
    }

    private async Task<decimal?> GetFirstPriceOfYearAsync(string ticker, CancellationToken cancellationToken)
    {
        var currentYear = DateTime.UtcNow.Year;
        var yearStart = new DateTime(currentYear, 1, 1);
        
        // Get the first available price from the current year
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

    private int DaysSinceYearStart()
    {
        var today = DateTime.UtcNow.Date;
        var yearStart = new DateTime(today.Year, 1, 1);
        return (today - yearStart).Days;
    }

    private async Task EnsureDeemedDisposalsAreCurrentAsync(
        int userId,
        List<Holding> holdings,
        CancellationToken cancellationToken)
    {
        if (holdings.Count == 0) return;

        var isIrishInvestor = await _context.ProjectionSettings
            .Where(ps => ps.UserId == userId)
            .Select(ps => ps.IsIrishInvestor)
            .FirstOrDefaultAsync(cancellationToken);

        if (!isIrishInvestor) return;

        foreach (var holding in holdings)
        {
            // Ensure we have historical coverage from the earliest buy before calculating deemed disposal.
            var earliestBuyDate = holding.Transactions.Count > 0
                ? holding.Transactions.Min(t => t.PurchaseDate)
                : (DateOnly?)null;

            if (!earliestBuyDate.HasValue) continue;

            var hasPriceHistoryCoverage = await BackfillHistoricalPricesIfNeededAsync(
                holding.Ticker, earliestBuyDate.Value, cancellationToken);

            if (!hasPriceHistoryCoverage)
            {
                _logger.LogWarning(
                    "Skipping deemed-disposal refresh for holding {HoldingId}: missing historical prices covering earliest buy date {PurchaseDate}.",
                    holding.Id, earliestBuyDate.Value);
                continue;
            }

            try
            {
                await _deemedDisposalService.CheckAndCreateDeemedDisposalEventsAsync(
                    holding.Id, userId, isIrishInvestor: true, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Deemed-disposal refresh failed during holdings load for holding {HoldingId}.",
                    holding.Id);
            }
        }
    }

    public async Task<ImportTransactionsResultDto> ImportTransactionsAsync(
        int userId, ImportTransactionsRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Rows == null || dto.Rows.Count == 0)
            throw new ArgumentException("No rows provided.");

        // Fetch user tax settings
        var userSettings = await _context.ProjectionSettings
            .FirstOrDefaultAsync(ps => ps.UserId == userId, cancellationToken);
        var isIrishInvestor = userSettings?.IsIrishInvestor ?? true;
        var taxRate = isIrishInvestor
            ? (userSettings?.ExitTaxPercent ?? 41m)
            : (userSettings?.CgtPercent ?? 33m);

        // Sort rows by date ascending for correct FIFO processing
        var rows = dto.Rows.OrderBy(r => r.Date).ToList();

        // Collect tickers processed so we can run side-effects after commit
        var buyTickers = new List<(string Ticker, DateOnly PurchaseDate)>();

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var utcNow = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);

            foreach (var row in rows)
            {
                var ticker = row.Ticker.ToUpperInvariant();
                var op = row.Operation.ToUpperInvariant();

                if (op == "BUY")
                {
                    // Get or create holding
                    var holding = await _context.Holdings
                        .FirstOrDefaultAsync(h => h.UserId == userId && h.Ticker == ticker, cancellationToken);

                    if (holding == null)
                    {
                        var etfName = await _priceService.GetEtfDescriptionAsync(ticker, cancellationToken);
                        holding = new Holding
                        {
                            UserId = userId,
                            Ticker = ticker,
                            EtfName = etfName,
                            Quantity = 0,
                            AverageCost = 0,
                            CreatedAt = utcNow,
                            UpdatedAt = utcNow
                        };
                        _context.Holdings.Add(holding);
                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    _context.Transactions.Add(new Transaction
                    {
                        HoldingId = holding.Id,
                        Quantity = row.Quantity,
                        PurchasePrice = row.Price,
                        PurchaseDate = row.Date,
                        CreatedAt = utcNow,
                        UpdatedAt = utcNow
                    });
                    await _context.SaveChangesAsync(cancellationToken);

                    await RecalculateHoldingFromRemainingLotsAsync(holding, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    buyTickers.Add((ticker, row.Date));
                }
                else if (op == "SELL")
                {
                    var holding = await _context.Holdings
                        .FirstOrDefaultAsync(h => h.UserId == userId && h.Ticker == ticker, cancellationToken);

                    if (holding == null)
                        throw new ArgumentException(
                            $"Cannot sell '{ticker}': no existing holding found. Ensure BUY rows for this ticker precede the SELL row.");

                    await _sellService.ConfirmSellNoTxAsync(
                        holding.Id, userId,
                        row.Quantity, row.Price, row.Date,
                        isIrishInvestor, taxRate, cancellationToken);
                }
                else
                {
                    throw new ArgumentException($"Unknown operation '{row.Operation}'. Expected BUY or SELL.");
                }
            }

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        // ── Post-commit side-effects (non-fatal) ────────────────────────────────
        foreach (var (ticker, purchaseDate) in buyTickers.DistinctBy(x => x.Ticker))
        {
            try
            {
                await BackfillHistoricalPricesIfNeededAsync(ticker, purchaseDate, cancellationToken);
                if (isIrishInvestor)
                {
                    var holding = await _context.Holdings
                        .FirstOrDefaultAsync(h => h.UserId == userId && h.Ticker == ticker, cancellationToken);
                    if (holding != null)
                        await _deemedDisposalService.CheckAndCreateDeemedDisposalEventsAsync(
                            holding.Id, userId, isIrishInvestor, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Non-fatal post-import side-effect failed for ticker {Ticker}", ticker);
            }
        }

        return new ImportTransactionsResultDto { Imported = rows.Count };
    }
}


