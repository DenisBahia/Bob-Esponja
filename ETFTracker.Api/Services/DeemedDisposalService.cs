using Microsoft.EntityFrameworkCore;
using ETFTracker.Api.Data;
using ETFTracker.Api.Models;

namespace ETFTracker.Api.Services;

public interface IDeemedDisposalService
{
    /// <summary>
    /// Checks all buy transactions for a holding and inserts any missing
    /// deemed-disposal TaxEvent rows for anniversaries that have already passed.
    /// Safe to call multiple times — duplicate events are skipped via the unique index.
    /// The tax rate is always loaded from the user's stored DeemedDisposalPercent default
    /// and cannot be overridden at point-of-use.
    /// </summary>
    Task CheckAndCreateDeemedDisposalEventsAsync(
        int holdingId, int userId, bool isIrishInvestor,
        CancellationToken ct = default);
}

public class DeemedDisposalService : IDeemedDisposalService
{
    private readonly AppDbContext _db;
    private readonly ILogger<DeemedDisposalService> _logger;

    public DeemedDisposalService(AppDbContext db, ILogger<DeemedDisposalService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task CheckAndCreateDeemedDisposalEventsAsync(
        int holdingId, int userId, bool isIrishInvestor,
        CancellationToken ct = default)
    {
        if (!isIrishInvestor) return;

        // Always load the deemed-disposal rate from user defaults — not overridable at call site
        var projSettings = await _db.UserSettings
            .FirstOrDefaultAsync(ps => ps.UserId == userId, ct);
        var taxRate = projSettings?.DeemedDisposalPercent
                      ?? projSettings?.ExitTaxPercent
                      ?? 41m;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var threshold = today.AddYears(-8);

        // Only process transactions where deemed_disposal_due = true
        var transactions = await _db.Transactions
            .Where(t => t.HoldingId == holdingId && t.PurchaseDate <= threshold && t.DeemedDisposalDue)
            .OrderBy(t => t.PurchaseDate)
            .ToListAsync(ct);

        if (transactions.Count == 0) return;

        var holding = await _db.Holdings
            .FirstOrDefaultAsync(h => h.Id == holdingId && h.UserId == userId, ct);
        if (holding == null) return;

        var ticker = holding.Ticker;

        // Preload all existing TaxEvent rows for this holding (deemed disposal type)
        var existingEvents = await _db.TaxEvents
            .Where(te => te.HoldingId == holdingId && te.EventType == TaxEventType.DeemedDisposal)
            .ToListAsync(ct);

        // Preload all sell lot allocations for this holding to compute remaining qty at each anniversary
        var allAllocations = await _db.SellLotAllocations
            .Include(sla => sla.SellRecord)
            .Where(sla => _db.Transactions
                .Where(t => t.HoldingId == holdingId)
                .Select(t => t.Id)
                .Contains(sla.BuyTransactionId))
            .ToListAsync(ct);

        var utcNow = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);
        var newEvents = new List<TaxEvent>();

        foreach (var txn in transactions)
        {
            // Enumerate all 8-year anniversaries up to and including today
            for (int years = 8; ; years += 8)
            {
                DateOnly anniversary = SafeAddYears(txn.PurchaseDate, years);
                if (anniversary > today) break;

                // Skip if this event already exists
                bool alreadyExists = existingEvents.Any(e =>
                    e.BuyTransactionId == txn.Id && e.EventDate == anniversary);
                if (alreadyExists) continue;

                // Remaining quantity at this anniversary: original qty minus units sold BEFORE this anniversary
                var soldBefore = allAllocations
                    .Where(sla => sla.BuyTransactionId == txn.Id
                               && sla.SellRecord != null
                               && sla.SellRecord.SellDate < anniversary)
                    .Sum(sla => sla.QuantityConsumed);

                var remainingQty = txn.Quantity - soldBefore;
                if (remainingQty <= 0m) continue;

                // Cost basis: price at the most recent prior deemed-disposal event for this lot,
                // or the original purchase price if this is the first anniversary
                decimal costBasis = GetCostBasisForAnniversary(txn, anniversary, existingEvents, newEvents);

                // Historical price at anniversary
                var priceAtAnniversary = await GetPriceOnOrBeforeAsync(ticker, anniversary, ct);
                if (priceAtAnniversary == null)
                {
                    _logger.LogWarning(
                        "No historical price found for {Ticker} at {Date}. Skipping deemed-disposal event.",
                        ticker, anniversary);
                    continue;
                }

                var taxableGain = (priceAtAnniversary.Value - costBasis) * remainingQty;
                var taxAmount = Math.Max(0m, taxableGain) * taxRate / 100m;

                var taxEvent = new TaxEvent
                {
                    UserId = userId,
                    HoldingId = holdingId,
                    BuyTransactionId = txn.Id,
                    SellRecordId = null,
                    EventType = TaxEventType.DeemedDisposal,
                    EventDate = anniversary,
                    QuantityAtEvent = remainingQty,
                    CostBasisPerUnit = costBasis,
                    PricePerUnitAtEvent = priceAtAnniversary.Value,
                    TaxableGain = taxableGain,
                    TaxAmount = taxAmount,
                    TaxRateUsed = taxRate,
                    TaxSubType = "DeemedDisposal",
                    Status = TaxEventStatus.Pending,
                    CreatedAt = utcNow
                };

                newEvents.Add(taxEvent);
                // Also add to in-memory list so subsequent anniversaries of the same lot pick up the right basis
                existingEvents.Add(taxEvent);
            }
        }

        if (newEvents.Count > 0)
        {
            _db.TaxEvents.AddRange(newEvents);
            try
            {
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "Created {Count} deemed-disposal tax event(s) for holding {HoldingId}.",
                    newEvents.Count, holdingId);
            }
            catch (Exception ex)
            {
                // Swallow — duplicate key on race condition is harmless
                _logger.LogWarning(ex,
                    "Could not save deemed-disposal events for holding {HoldingId} (may be duplicate).",
                    holdingId);
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static decimal GetCostBasisForAnniversary(
        Transaction txn,
        DateOnly anniversary,
        List<TaxEvent> existingEvents,
        List<TaxEvent> newEvents)
    {
        // Most recent deemed-disposal event for this lot with event_date < current anniversary
        var prior = existingEvents
            .Concat(newEvents)
            .Where(e => e.BuyTransactionId == txn.Id
                     && e.EventType == TaxEventType.DeemedDisposal
                     && e.EventDate < anniversary)
            .OrderByDescending(e => e.EventDate)
            .FirstOrDefault();

        return prior?.PricePerUnitAtEvent ?? txn.PurchasePrice;
    }

    private static DateOnly SafeAddYears(DateOnly date, int years)
    {
        try
        {
            return new DateOnly(date.Year + years, date.Month, date.Day);
        }
        catch
        {
            // Feb 29 on non-leap year → Feb 28
            var y = date.Year + years;
            var day = Math.Min(date.Day, DateTime.DaysInMonth(y, date.Month));
            return new DateOnly(y, date.Month, day);
        }
    }

    private async Task<decimal?> GetPriceOnOrBeforeAsync(string ticker, DateOnly date, CancellationToken ct)
    {
        var cutoff = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var snap = await _db.PriceSnapshots
            .Where(ps => ps.Ticker == ticker && ps.SnapshotDate <= cutoff)
            .OrderByDescending(ps => ps.SnapshotDate)
            .FirstOrDefaultAsync(ct);

        return snap?.Price;
    }
}

