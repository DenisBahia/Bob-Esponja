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

    Task ConfirmSellNoTxAsync(int holdingId, int userId, decimal qty, decimal sellPrice,
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
        DateOnly sellDate, decimal sellPrice, decimal totalProfit, decimal taxAmountSaved,
        decimal taxRate, string taxType)
    {
        return new TaxEvent
        {
            UserId = userId,
            HoldingId = holdingId,
            SellRecordId = sellRecordId,
            BuyTransactionId = null,
            EventType = TaxEventType.Sell,
            EventDate = sellDate,
            QuantityAtEvent = 0,
            CostBasisPerUnit = 0,
            PricePerUnitAtEvent = sellPrice,
            TaxableGain = totalProfit,
            TaxAmount = taxAmountSaved,
            TaxRateUsed = taxRate,
            TaxSubType = taxType,
            Status = TaxEventStatus.Pending,
            CreatedAt = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc)
        };
    }

    private static string DetermineTaxType(List<SellLotBreakdownDto> lots)
    {
        var dddQty = lots.Where(l => l.DeemedDisposalDue).Sum(l => l.QuantityConsumed);
        var totalQty = lots.Sum(l => l.QuantityConsumed);
        return (totalQty > 0 && dddQty / totalQty > 0.5m) ? "ExitTax" : "CGT";
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public async Task<SellPreviewDto> PreviewSellAsync(int holdingId, int userId, decimal qty,
        decimal sellPrice, DateOnly sellDate, bool isIrishInvestor, decimal taxRate,
        CancellationToken ct = default)
    {
        var holding = await GetHoldingOrThrowAsync(holdingId, userId, ct);
        var (lots, available) = await ComputeLotsAsync(holding, qty, sellPrice, sellDate, ct);

        var totalProfit = lots.Sum(l => l.ProfitOnLot);
        var taxType = DetermineTaxType(lots);
        var taxDue = taxType == "ExitTax" ? Math.Max(0, totalProfit) * taxRate / 100m : 0m;

        return new SellPreviewDto
        {
            AvailableQuantity = available,
            TotalProfit = totalProfit,
            CgtDue = taxDue,
            TaxRateUsed = taxRate,
            TaxType = taxType,
            HasLosses = totalProfit < 0,
            Lots = lots
        };
    }

    public async Task<SellRecordDto> ConfirmSellAsync(int holdingId, int userId, decimal qty,
        decimal sellPrice, DateOnly sellDate, bool isIrishInvestor, decimal taxRate,
        CancellationToken ct = default)
    {
        var holding = await GetHoldingOrThrowAsync(holdingId, userId, ct);
        var (lots, _) = await ComputeLotsAsync(holding, qty, sellPrice, sellDate, ct);

        var totalProfit = lots.Sum(l => l.ProfitOnLot);
        var taxType = DetermineTaxType(lots);
        var taxAmountSaved = taxType == "ExitTax" ? Math.Max(0, totalProfit) * taxRate / 100m : 0m;
        var utcNow = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var sellRecord = new SellRecord
            {
                HoldingId = holdingId,
                SellDate = sellDate,
                SellPrice = sellPrice,
                Quantity = qty,
                TotalProfit = totalProfit,
                TaxAmountSaved = taxAmountSaved,
                TaxRateUsed = taxRate,
                TaxType = taxType,
                CreatedAt = utcNow
            };
            _db.SellRecords.Add(sellRecord);
            await _db.SaveChangesAsync(ct);

            var allTxns = await _db.Transactions
                .Where(t => t.HoldingId == holdingId)
                .ToDictionaryAsync(t => t.Id, ct);

            foreach (var lot in lots)
            {
                _db.SellLotAllocations.Add(new SellLotAllocation
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
                });
            }
            await _db.SaveChangesAsync(ct);

            var allConsumed = await _db.SellLotAllocations
                .Where(sla => allTxns.Keys.Contains(sla.BuyTransactionId))
                .GroupBy(sla => sla.BuyTransactionId)
                .Select(g => new { TxnId = g.Key, Consumed = g.Sum(sla => sla.QuantityConsumed) })
                .ToListAsync(ct);
            var consumedMap = allConsumed.ToDictionary(x => x.TxnId, x => x.Consumed);

            decimal remQty = 0m, remCost = 0m;
            foreach (var (txnId, txn) in allTxns)
            {
                var consumed = consumedMap.TryGetValue(txnId, out var c) ? c : 0m;
                var remaining = txn.Quantity - consumed;
                if (remaining > 0) { remQty += remaining; remCost += remaining * txn.PurchasePrice; }
            }
            holding.Quantity = remQty;
            holding.AverageCost = remQty > 0 ? remCost / remQty : 0m;
            holding.UpdatedAt = utcNow;
            _db.Holdings.Update(holding);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            try
            {
                var taxEvent = BuildSellTaxEvent(userId, holdingId, sellRecord.Id,
                    sellDate, sellPrice, totalProfit, taxAmountSaved, taxRate, taxType);
                _db.TaxEvents.Add(taxEvent);
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
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
                TaxAmountSaved = taxAmountSaved,
                TaxRateUsed = taxRate,
                TaxType = taxType,
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

    public async Task ConfirmSellNoTxAsync(int holdingId, int userId, decimal qty,
        decimal sellPrice, DateOnly sellDate, bool isIrishInvestor, decimal taxRate,
        CancellationToken ct = default)
    {
        var holding = await GetHoldingOrThrowAsync(holdingId, userId, ct);
        var (lots, _) = await ComputeLotsAsync(holding, qty, sellPrice, sellDate, ct);

        var totalProfit = lots.Sum(l => l.ProfitOnLot);
        var taxType = DetermineTaxType(lots);
        var taxAmountSaved = taxType == "ExitTax" ? Math.Max(0, totalProfit) * taxRate / 100m : 0m;
        var utcNow = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);

        var sellRecord = new SellRecord
        {
            HoldingId = holdingId,
            SellDate = sellDate,
            SellPrice = sellPrice,
            Quantity = qty,
            TotalProfit = totalProfit,
            TaxAmountSaved = taxAmountSaved,
            TaxRateUsed = taxRate,
            TaxType = taxType,
            CreatedAt = utcNow
        };
        _db.SellRecords.Add(sellRecord);
        await _db.SaveChangesAsync(ct);

        var allTxns = await _db.Transactions
            .Where(t => t.HoldingId == holdingId)
            .ToDictionaryAsync(t => t.Id, ct);

        foreach (var lot in lots)
        {
            _db.SellLotAllocations.Add(new SellLotAllocation
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
            });
        }
        await _db.SaveChangesAsync(ct);

        var allConsumed = await _db.SellLotAllocations
            .Where(sla => allTxns.Keys.Contains(sla.BuyTransactionId))
            .GroupBy(sla => sla.BuyTransactionId)
            .Select(g => new { TxnId = g.Key, Consumed = g.Sum(sla => sla.QuantityConsumed) })
            .ToListAsync(ct);
        var consumedMap = allConsumed.ToDictionary(x => x.TxnId, x => x.Consumed);

        decimal remQty = 0m, remCost = 0m;
        foreach (var (txnId, txn) in allTxns)
        {
            var consumed = consumedMap.TryGetValue(txnId, out var c) ? c : 0m;
            var remaining = txn.Quantity - consumed;
            if (remaining > 0) { remQty += remaining; remCost += remaining * txn.PurchasePrice; }
        }
        holding.Quantity = remQty;
        holding.AverageCost = remQty > 0 ? remCost / remQty : 0m;
        holding.UpdatedAt = utcNow;
        _db.Holdings.Update(holding);
        await _db.SaveChangesAsync(ct);

        try
        {
            var taxEvent = BuildSellTaxEvent(userId, holdingId, sellRecord.Id,
                sellDate, sellPrice, totalProfit, taxAmountSaved, taxRate, taxType);
            _db.TaxEvents.Add(taxEvent);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create TaxEvent inside import for sell record {SellRecordId}", sellRecord.Id);
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

        var txnIds = records.SelectMany(r => r.LotAllocations.Select(l => l.BuyTransactionId)).Distinct().ToList();
        var txns = await _db.Transactions
            .Where(t => txnIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, ct);

        return records.Select(r => new SellRecordDto
        {
            Id = r.Id,
            HoldingId = r.HoldingId,
            SellDate = r.SellDate,
            SellPrice = r.SellPrice,
            Quantity = r.Quantity,
            TotalProfit = r.TotalProfit,
            TaxAmountSaved = r.TaxAmountSaved,
            TaxRateUsed = r.TaxRateUsed,
            TaxType = r.TaxType,
            CreatedAt = r.CreatedAt,
            Lots = r.LotAllocations.Select(l => new SellLotBreakdownDto
            {
                BuyTransactionId = l.BuyTransactionId,
                BuyDate = txns.TryGetValue(l.BuyTransactionId, out var t) ? t.PurchaseDate : default,
                QuantityConsumed = l.QuantityConsumed,
                OriginalCostPerUnit = l.OriginalCostPerUnit,
                AdjustedCostPerUnit = r.TaxType == "ExitTax" ? l.AdjustedCostPerUnit : l.OriginalCostPerUnit,
                DeemedDisposalDate = r.TaxType == "ExitTax" ? l.DeemedDisposalDate : null,
                DeemedDisposalPricePerUnit = r.TaxType == "ExitTax" ? l.DeemedDisposalPricePerUnit : null,
                ProfitOnLot = l.ProfitOnLot,
                DeemedDisposalDue = txns.TryGetValue(l.BuyTransactionId, out var t2) && t2.DeemedDisposalDue
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

    private async Task<(List<SellLotBreakdownDto> lots, decimal availableQty)> ComputeLotsAsync(
        Holding holding, decimal qty, decimal sellPrice, DateOnly sellDate, CancellationToken ct)
    {
        var txns = await _db.Transactions
            .Where(t => t.HoldingId == holding.Id)
            .OrderBy(t => t.PurchaseDate)
            .ToListAsync(ct);

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

            // Per-lot deemed disposal: only adjust cost basis if this specific buy is flagged
            (decimal adjustedCost, DateOnly? deemedDate, decimal? deemedPrice) =
                txn.DeemedDisposalDue
                    ? await ResolveLotTaxBasisAsync(ticker, txn.PurchaseDate, txn.PurchasePrice, sellDate, ct)
                    : (txn.PurchasePrice, null, null);

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
                ProfitOnLot = profit,
                DeemedDisposalDue = txn.DeemedDisposalDue
            });
        }

        return (lots, totalAvailable);
    }

    private static DateOnly? GetLastAnniversaryBefore(DateOnly purchaseDate, DateOnly sellDate)
    {
        DateOnly? last = null;
        for (int years = 8; ; years += 8)
        {
            DateOnly anniversary;
            try { anniversary = new DateOnly(purchaseDate.Year + years, purchaseDate.Month, purchaseDate.Day); }
            catch
            {
                var y = purchaseDate.Year + years;
                var m = purchaseDate.Month;
                anniversary = new DateOnly(y, m, Math.Min(purchaseDate.Day, DateTime.DaysInMonth(y, m)));
            }
            if (anniversary >= sellDate) break;
            last = anniversary;
        }
        return last;
    }

    private async Task<(decimal adjustedCost, DateOnly? deemedDate, decimal? deemedPrice)> ResolveLotTaxBasisAsync(
        string ticker, DateOnly purchaseDate, decimal originalCost, DateOnly sellDate, CancellationToken ct)
    {
        var anniversary = GetLastAnniversaryBefore(purchaseDate, sellDate);
        if (!anniversary.HasValue)
            return (originalCost, null, null);

        var cutoff = anniversary.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var snap = await _db.PriceSnapshots
            .Where(ps => ps.Ticker == ticker && ps.SnapshotDate <= cutoff)
            .OrderByDescending(ps => ps.SnapshotDate)
            .FirstOrDefaultAsync(ct);

        if (snap == null) return (originalCost, null, null);
        return (snap.Price, anniversary.Value, snap.Price);
    }
}

