using Microsoft.EntityFrameworkCore;
using ETFTracker.Api.Data;
using ETFTracker.Api.Dtos;
using ETFTracker.Api.Models;

namespace ETFTracker.Api.Services;

public interface ISellService
{
    Task<SellPreviewDto> PreviewSellAsync(int holdingId, int userId, decimal qty, decimal sellPrice,
        DateOnly sellDate, bool isIrishInvestor, decimal taxRate, CancellationToken ct = default);

    Task<SellRecordDto> ConfirmSellAsync(int holdingId, int userId, decimal qty, decimal sellPrice,
        DateOnly sellDate, bool isIrishInvestor, decimal taxRate, CancellationToken ct = default);
    Task<List<SellRecordDto>> GetSellHistoryAsync(int holdingId, int userId, CancellationToken ct = default);
}

public class SellService : ISellService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SellService> _logger;

    public SellService(AppDbContext db, ILogger<SellService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── TaxEvent helper ────────────────────────────────────────────────────────
    private static TaxEvent BuildSellTaxEvent(int userId, int holdingId, int sellRecordId,
        DateOnly sellDate, decimal sellPrice, decimal totalProfit, decimal cgtPaid, decimal taxRate)
    {
        // Weighted average basis = sellPrice - (totalProfit / totalQty) is implicit;
        // we record the aggregate sell-level event (one row per sell, not per lot)
        return new TaxEvent
        {
            UserId = userId,
            HoldingId = holdingId,
            SellRecordId = sellRecordId,
            BuyTransactionId = null,
            EventType = TaxEventType.Sell,
            EventDate = sellDate,
            QuantityAtEvent = 0,           // not relevant at aggregate level; see lots in sell_records
            CostBasisPerUnit = 0,
            PricePerUnitAtEvent = sellPrice,
            TaxableGain = totalProfit,
            TaxAmount = cgtPaid,
            TaxRateUsed = taxRate,
            Status = TaxEventStatus.Pending,
            CreatedAt = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc)
        };
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public async Task<SellPreviewDto> PreviewSellAsync(int holdingId, int userId, decimal qty,
        decimal sellPrice, DateOnly sellDate, bool isIrishInvestor, decimal taxRate,
        CancellationToken ct = default)
    {
        var holding = await GetHoldingOrThrowAsync(holdingId, userId, ct);
        var (lots, available) = await ComputeLotsAsync(holding, qty, sellPrice, sellDate, isIrishInvestor, ct);

        var totalProfit = lots.Sum(l => l.ProfitOnLot);
        var cgtDue = Math.Max(0, totalProfit) * taxRate / 100m;

        return new SellPreviewDto
        {
            AvailableQuantity = available,
            TotalProfit = totalProfit,
            CgtDue = cgtDue,
            TaxRateUsed = taxRate,
            Lots = lots
        };
    }

    public async Task<SellRecordDto> ConfirmSellAsync(int holdingId, int userId, decimal qty,
        decimal sellPrice, DateOnly sellDate, bool isIrishInvestor, decimal taxRate,
        CancellationToken ct = default)
    {
        var holding = await GetHoldingOrThrowAsync(holdingId, userId, ct);
        var (lots, _) = await ComputeLotsAsync(holding, qty, sellPrice, sellDate, isIrishInvestor, ct);

        var totalProfit = lots.Sum(l => l.ProfitOnLot);
        var cgtPaid = Math.Max(0, totalProfit) * taxRate / 100m;
        var utcNow = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Persist SellRecord
            var sellRecord = new SellRecord
            {
                HoldingId = holdingId,
                SellDate = sellDate,
                SellPrice = sellPrice,
                Quantity = qty,
                TotalProfit = totalProfit,
                CgtPaid = cgtPaid,
                TaxRateUsed = taxRate,
                IsIrishInvestor = isIrishInvestor,
                CreatedAt = utcNow
            };
            _db.SellRecords.Add(sellRecord);
            await _db.SaveChangesAsync(ct);

            // Persist SellLotAllocations (need sellRecord.Id)
            // We need the full transaction rows to resolve buy dates
            var allTxns = await _db.Transactions
                .Where(t => t.HoldingId == holdingId)
                .ToDictionaryAsync(t => t.Id, ct);

            foreach (var lot in lots)
            {
                var sla = new SellLotAllocation
                {
                    SellRecordId = sellRecord.Id,
                    BuyTransactionId = lot.BuyTransactionId,
                    QuantityConsumed = lot.QuantityConsumed,
                    OriginalCostPerUnit = lot.OriginalCostPerUnit,
                    AdjustedCostPerUnit = lot.AdjustedCostPerUnit,
                    DeemedDisposalDate = lot.DeemedDisposalDate,
                    DeemedDisposalPricePerUnit = lot.DeemedDisposalPricePerUnit,
                    ProfitOnLot = lot.ProfitOnLot,
                    CreatedAt = utcNow
                };
                _db.SellLotAllocations.Add(sla);
            }
            await _db.SaveChangesAsync(ct);

            // Update Holding: decrement quantity, recalc average cost from remaining unconsumed lots
            // Load ALL existing consumed qty (including this new sell) for remaining calc
            var allConsumed = await _db.SellLotAllocations
                .Where(sla => allTxns.Keys.Contains(sla.BuyTransactionId))
                .GroupBy(sla => sla.BuyTransactionId)
                .Select(g => new { TxnId = g.Key, Consumed = g.Sum(sla => sla.QuantityConsumed) })
                .ToListAsync(ct);
            var consumedMap = allConsumed.ToDictionary(x => x.TxnId, x => x.Consumed);

            decimal remQty = 0m;
            decimal remCost = 0m;
            foreach (var (txnId, txn) in allTxns)
            {
                var consumed = consumedMap.TryGetValue(txnId, out var c) ? c : 0m;
                var remaining = txn.Quantity - consumed;
                if (remaining > 0)
                {
                    remQty += remaining;
                    remCost += remaining * txn.PurchasePrice;
                }
            }

            holding.Quantity = remQty;
            holding.AverageCost = remQty > 0 ? remCost / remQty : 0m;
            holding.UpdatedAt = utcNow;
            _db.Holdings.Update(holding);
            await _db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);

            // Insert TaxEvent for this sell (outside the main tx so ID is available)
            try
            {
                var taxEvent = BuildSellTaxEvent(
                    userId, holdingId, sellRecord.Id,
                    sellDate, sellPrice, totalProfit, cgtPaid, taxRate);
                _db.TaxEvents.Add(taxEvent);
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // Non-fatal: sell is committed, tax event can be recreated
                _logger.LogWarning(ex, "Failed to create TaxEvent for sell record {SellRecordId}", sellRecord.Id);
            }

            return new SellRecordDto
            {
                Id = sellRecord.Id,
                HoldingId = holdingId,
                SellDate = sellDate,
                SellPrice = sellPrice,
                Quantity = qty,
                TotalProfit = totalProfit,
                CgtPaid = cgtPaid,
                TaxRateUsed = taxRate,
                IsIrishInvestor = isIrishInvestor,
                CreatedAt = utcNow,
                Lots = lots
            };
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<List<SellRecordDto>> GetSellHistoryAsync(int holdingId, int userId,
        CancellationToken ct = default)
    {
        await GetHoldingOrThrowAsync(holdingId, userId, ct);

        var records = await _db.SellRecords
            .Where(sr => sr.HoldingId == holdingId)
            .Include(sr => sr.LotAllocations)
            .OrderByDescending(sr => sr.SellDate)
            .ToListAsync(ct);

        // Load buy dates for all referenced transactions
        var txnIds = records.SelectMany(r => r.LotAllocations.Select(l => l.BuyTransactionId)).Distinct().ToList();
        var txnDates = await _db.Transactions
            .Where(t => txnIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.PurchaseDate, ct);

        return records.Select(r => new SellRecordDto
        {
            Id = r.Id,
            HoldingId = r.HoldingId,
            SellDate = r.SellDate,
            SellPrice = r.SellPrice,
            Quantity = r.Quantity,
            TotalProfit = r.TotalProfit,
            CgtPaid = r.CgtPaid,
            TaxRateUsed = r.TaxRateUsed,
            IsIrishInvestor = r.IsIrishInvestor,
            CreatedAt = r.CreatedAt,
            Lots = r.LotAllocations.Select(l => new SellLotBreakdownDto
            {
                BuyTransactionId = l.BuyTransactionId,
                BuyDate = txnDates.TryGetValue(l.BuyTransactionId, out var d) ? d : default,
                QuantityConsumed = l.QuantityConsumed,
                OriginalCostPerUnit = l.OriginalCostPerUnit,
                AdjustedCostPerUnit = r.IsIrishInvestor ? l.AdjustedCostPerUnit : l.OriginalCostPerUnit,
                DeemedDisposalDate = r.IsIrishInvestor ? l.DeemedDisposalDate : null,
                DeemedDisposalPricePerUnit = r.IsIrishInvestor ? l.DeemedDisposalPricePerUnit : null,
                ProfitOnLot = l.ProfitOnLot
            }).ToList()
        }).ToList();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<Holding> GetHoldingOrThrowAsync(int holdingId, int userId, CancellationToken ct)
    {
        var holding = await _db.Holdings
            .FirstOrDefaultAsync(h => h.Id == holdingId && h.UserId == userId, ct);
        if (holding == null)
            throw new UnauthorizedAccessException("Holding not found or access denied.");
        return holding;
    }

    /// <summary>
    /// Runs the FIFO engine.  Returns the per-lot breakdown list and the total available quantity.
    /// Throws ArgumentException (400) if qty > available.
    /// </summary>
    private async Task<(List<SellLotBreakdownDto> lots, decimal availableQty)> ComputeLotsAsync(
        Holding holding, decimal qty, decimal sellPrice, DateOnly sellDate,
        bool isIrishInvestor, CancellationToken ct)
    {
        // 1. Load all buy transactions ordered by purchase date ASC
        var txns = await _db.Transactions
            .Where(t => t.HoldingId == holding.Id)
            .OrderBy(t => t.PurchaseDate)
            .ToListAsync(ct);

        // 2. Load consumed qty per transaction from ALL existing sell lot allocations
        var txnIds = txns.Select(t => t.Id).ToList();
        var consumedMap = await _db.SellLotAllocations
            .Where(sla => txnIds.Contains(sla.BuyTransactionId))
            .GroupBy(sla => sla.BuyTransactionId)
            .Select(g => new { TxnId = g.Key, Consumed = g.Sum(sla => sla.QuantityConsumed) })
            .ToDictionaryAsync(x => x.TxnId, x => x.Consumed, ct);

        decimal totalAvailable = txns.Sum(t =>
        {
            var consumed = consumedMap.TryGetValue(t.Id, out var c) ? c : 0m;
            return Math.Max(0m, t.Quantity - consumed);
        });

        if (qty > totalAvailable)
            throw new ArgumentException(
                $"Insufficient quantity. Requested: {qty}, Available: {totalAvailable}.");

        // Preload price snapshots for the holding ticker (for deemed disposal lookups)
        var ticker = holding.Ticker;
        var lots = new List<SellLotBreakdownDto>();
        decimal remaining = qty;

        foreach (var txn in txns)
        {
            if (remaining <= 0m) break;

            var consumed = consumedMap.TryGetValue(txn.Id, out var c) ? c : 0m;
            var lotAvail = txn.Quantity - consumed;
            if (lotAvail <= 0m) continue;

            var consume = Math.Min(remaining, lotAvail);
            remaining -= consume;

            var (adjustedCost, deemedDate, deemedPrice) = await ResolveLotTaxBasisAsync(
                ticker, txn.PurchaseDate, txn.PurchasePrice, sellDate, isIrishInvestor, ct);

            var profit = (sellPrice - adjustedCost) * consume;

            lots.Add(new SellLotBreakdownDto
            {
                BuyTransactionId = txn.Id,
                BuyDate = txn.PurchaseDate,
                QuantityConsumed = consume,
                OriginalCostPerUnit = txn.PurchasePrice,
                AdjustedCostPerUnit = adjustedCost,
                DeemedDisposalDate = deemedDate,
                DeemedDisposalPricePerUnit = deemedPrice,
                ProfitOnLot = profit
            });
        }

        return (lots, totalAvailable);
    }

    /// <summary>
    /// Returns the latest 8-year anniversary of <paramref name="purchaseDate"/> that is strictly
    /// before <paramref name="sellDate"/>, or null if none has occurred yet.
    /// </summary>
    private static DateOnly? GetLastAnniversaryBefore(DateOnly purchaseDate, DateOnly sellDate)
    {
        DateOnly? last = null;
        for (int years = 8; ; years += 8)
        {
            DateOnly anniversary;
            try
            {
                anniversary = new DateOnly(purchaseDate.Year + years, purchaseDate.Month, purchaseDate.Day);
            }
            catch
            {
                // Invalid date (e.g., Feb 29 on non-leap year) — use last day of month
                var y = purchaseDate.Year + years;
                var m = purchaseDate.Month;
                var day = Math.Min(purchaseDate.Day, DateTime.DaysInMonth(y, m));
                anniversary = new DateOnly(y, m, day);
            }

            if (anniversary >= sellDate) break;
            last = anniversary;
        }
        return last;
    }

    private async Task<(decimal adjustedCost, DateOnly? deemedDate, decimal? deemedPrice)> ResolveLotTaxBasisAsync(
        string ticker,
        DateOnly purchaseDate,
        decimal originalCost,
        DateOnly sellDate,
        bool isIrishInvestor,
        CancellationToken ct)
    {
        if (!isIrishInvestor)
            return (originalCost, null, null);

        var anniversary = GetLastAnniversaryBefore(purchaseDate, sellDate);
        if (!anniversary.HasValue)
            return (originalCost, null, null);

        var snap = await GetPriceOnOrBeforeAsync(ticker, anniversary.Value, ct);
        if (!snap.HasValue)
            return (originalCost, null, null);

        return (snap.Value.price, anniversary.Value, snap.Value.price);
    }

    private async Task<(decimal price, DateOnly date)?> GetPriceOnOrBeforeAsync(
        string ticker, DateOnly date, CancellationToken ct)
    {
        var cutoff = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var snap = await _db.PriceSnapshots
            .Where(ps => ps.Ticker == ticker && ps.SnapshotDate <= cutoff)
            .OrderByDescending(ps => ps.SnapshotDate)
            .FirstOrDefaultAsync(ct);

        if (snap == null) return null;
        return (snap.Price, DateOnly.FromDateTime(snap.SnapshotDate.Date));
    }
}



