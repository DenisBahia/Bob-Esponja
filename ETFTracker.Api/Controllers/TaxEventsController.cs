using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ETFTracker.Api.Data;
using ETFTracker.Api.Dtos;
using ETFTracker.Api.Models;
using ETFTracker.Api.Services;

namespace ETFTracker.Api.Controllers;

/// <summary>
/// Tax event ledger — deemed disposals and sell events.
/// </summary>
[Authorize]
[ApiController]
[Route("api/tax-events")]
public class TaxEventsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISharingContextService _sharingContext;
    private readonly ILogger<TaxEventsController> _logger;

    public TaxEventsController(AppDbContext db, ISharingContextService sharingContext,
        ILogger<TaxEventsController> logger)
    {
        _db = db;
        _sharingContext = sharingContext;
        _logger = logger;
    }

    private int UserId => _sharingContext.GetEffectiveUserId();

    /// <summary>Get all tax events for the authenticated user, optionally filtered by holding.</summary>
    [HttpGet]
    public async Task<ActionResult<TaxSummaryDto>> GetTaxEvents(
        [FromQuery] int? holdingId, CancellationToken ct = default)
    {
        try
        {
            var query = _db.TaxEvents
                .Where(te => te.UserId == UserId);

            if (holdingId.HasValue)
                query = query.Where(te => te.HoldingId == holdingId.Value);

            var events = await query
                .Include(te => te.Holding)
                .OrderByDescending(te => te.EventDate)
                .ToListAsync(ct);

            var dtos = events.Select(MapToDto).ToList();

            // Next deemed disposal date across all events
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            DateOnly? nextDd = null;
            if (!holdingId.HasValue)
            {
                // Compute globally from all transactions that still have remaining (unsold) quantity
                var allTxns = await _db.Transactions
                    .Where(t => _db.Holdings
                        .Where(h => h.UserId == UserId)
                        .Select(h => h.Id)
                        .Contains(t.HoldingId))
                    .ToListAsync(ct);

                var txnIds = allTxns.Select(t => t.Id).ToList();
                var consumedByTxn = await _db.SellLotAllocations
                    .Where(sla => txnIds.Contains(sla.BuyTransactionId))
                    .GroupBy(sla => sla.BuyTransactionId)
                    .Select(g => new { TxnId = g.Key, Consumed = g.Sum(sla => sla.QuantityConsumed) })
                    .ToDictionaryAsync(x => x.TxnId, x => x.Consumed, ct);

                foreach (var txn in allTxns)
                {
                    // Skip fully sold lots
                    var consumed = consumedByTxn.TryGetValue(txn.Id, out var c) ? c : 0m;
                    if (txn.Quantity - consumed <= 0m) continue;

                    for (int y = 8; ; y += 8)
                    {
                        DateOnly ann;
                        try { ann = new DateOnly(txn.PurchaseDate.Year + y, txn.PurchaseDate.Month, txn.PurchaseDate.Day); }
                        catch { ann = new DateOnly(txn.PurchaseDate.Year + y, txn.PurchaseDate.Month,
                            Math.Min(txn.PurchaseDate.Day, DateTime.DaysInMonth(txn.PurchaseDate.Year + y, txn.PurchaseDate.Month))); }
                        if (ann > today) { if (!nextDd.HasValue || ann < nextDd.Value) nextDd = ann; break; }
                    }
                }
            }

            // ── Tax-free allowance ────────────────────────────────────────────────
            var projSettings = await _db.ProjectionSettings
                .FirstOrDefaultAsync(ps => ps.UserId == UserId, ct);

            var annualAllowance = (projSettings is { IsIrishInvestor: false } && projSettings.TaxFreeAllowancePerYear > 0)
                ? projSettings.TaxFreeAllowancePerYear
                : 0m;

            var allowanceByYear = new List<TaxYearAllowanceSummaryDto>();
            decimal totalPendingAfterAllowance;

            if (annualAllowance > 0)
            {
                var pendingByYear = dtos
                    .Where(e => e.Status == "Pending")
                    .GroupBy(e => e.EventDate.Year)
                    .OrderBy(g => g.Key);

                foreach (var yearGroup in pendingByYear)
                {
                    var totalGain = yearGroup.Sum(e => e.TaxableGain);
                    var taxBefore = yearGroup.Sum(e => e.TaxAmount);
                    // Allowance is deducted directly from the tax due (not from gain)
                    var applied   = Math.Min(annualAllowance, taxBefore);
                    var taxAfter  = Math.Round(Math.Max(0m, taxBefore - applied), 2);

                    allowanceByYear.Add(new TaxYearAllowanceSummaryDto
                    {
                        Year                = yearGroup.Key,
                        TotalTaxableGain    = totalGain,
                        TaxBeforeAllowance  = taxBefore,
                        AllowanceApplied    = applied,
                        TaxAfterAllowance   = taxAfter,
                    });
                }

                totalPendingAfterAllowance = allowanceByYear.Sum(y => y.TaxAfterAllowance);
            }
            else
            {
                totalPendingAfterAllowance = dtos.Where(e => e.Status == "Pending").Sum(e => e.TaxAmount);
            }

            return Ok(new TaxSummaryDto
            {
                TotalPaid = dtos.Where(e => e.Status == "Paid").Sum(e => e.TaxAmount),
                TotalPending = dtos.Where(e => e.Status == "Pending").Sum(e => e.TaxAmount),
                NextDeemedDisposalDate = nextDd,
                Events = dtos,
                AnnualTaxFreeAllowance = annualAllowance,
                AllowanceByYear = allowanceByYear,
                TotalPendingAfterAllowance = totalPendingAfterAllowance,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tax events");
            return StatusCode(500, new { message = "Error retrieving tax events" });
        }
    }

    /// <summary>Mark a single tax event as paid.</summary>
    [HttpPut("{id}/mark-paid")]
    public async Task<ActionResult<TaxEventDto>> MarkPaid(int id,
        [FromBody] MarkTaxEventPaidDto? dto, CancellationToken ct = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "Read-only profile." });

            var taxEvent = await _db.TaxEvents
                .FirstOrDefaultAsync(te => te.Id == id && te.UserId == UserId, ct);

            if (taxEvent == null)
                return NotFound(new { message = "Tax event not found." });

            taxEvent.Status = TaxEventStatus.Paid;
            taxEvent.PaidAt = dto?.PaidAt ?? new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);
            await _db.SaveChangesAsync(ct);

            await _db.Entry(taxEvent).Reference(te => te.Holding).LoadAsync(ct);
            return Ok(MapToDto(taxEvent));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking tax event {Id} as paid", id);
            return StatusCode(500, new { message = "Error updating tax event" });
        }
    }

    /// <summary>Bulk-mark all pending tax events as paid (optional year filter).</summary>
    [HttpPut("mark-all-paid")]
    public async Task<ActionResult<object>> MarkAllPaid(
        [FromQuery] int? year, CancellationToken ct = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "Read-only profile." });

            var query = _db.TaxEvents
                .Where(te => te.UserId == UserId && te.Status == TaxEventStatus.Pending);

            if (year.HasValue)
                query = query.Where(te => te.EventDate.Year == year.Value);

            var events = await query.ToListAsync(ct);
            var paidAt = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);

            foreach (var e in events)
            {
                e.Status = TaxEventStatus.Paid;
                e.PaidAt = paidAt;
            }

            await _db.SaveChangesAsync(ct);
            return Ok(new { marked = events.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk-marking tax events as paid");
            return StatusCode(500, new { message = "Error updating tax events" });
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static TaxEventDto MapToDto(TaxEvent te) => new()
    {
        Id = te.Id,
        HoldingId = te.HoldingId,
        Ticker = te.Holding?.Ticker ?? string.Empty,
        EtfName = te.Holding?.EtfName,
        BuyTransactionId = te.BuyTransactionId,
        SellRecordId = te.SellRecordId,
        EventType = te.EventType.ToString(),
        EventDate = te.EventDate,
        QuantityAtEvent = te.QuantityAtEvent,
        CostBasisPerUnit = te.CostBasisPerUnit,
        PricePerUnitAtEvent = te.PricePerUnitAtEvent,
        TaxableGain = te.TaxableGain,
        TaxAmount = te.TaxAmount,
        TaxRateUsed = te.TaxRateUsed,
        Status = te.Status.ToString(),
        PaidAt = te.PaidAt,
        CreatedAt = te.CreatedAt
    };
}

