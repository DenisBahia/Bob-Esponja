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
            var projSettings = await _db.ProjectionSettings
                .FirstOrDefaultAsync(ps => ps.UserId == UserId, ct);
            var isIrishInvestor = projSettings?.IsIrishInvestor ?? false;
            var annualAllowance = (!isIrishInvestor && (projSettings?.TaxFreeAllowancePerYear ?? 0) > 0)
                ? projSettings!.TaxFreeAllowancePerYear
                : 0m;

            var query = _db.TaxEvents
                .Where(te => te.UserId == UserId);
            if (holdingId.HasValue)
                query = query.Where(te => te.HoldingId == holdingId.Value);

            var events = await query
                .Include(te => te.Holding)
                .OrderByDescending(te => te.EventDate)
                .ToListAsync(ct);

            var dtos = events.Select(MapToDto).ToList();

            // NextDeemedDisposalDate — only for Irish investors
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            DateOnly? nextDd = null;
            if (isIrishInvestor && !holdingId.HasValue)
            {
                var allTxns = await _db.Transactions
                    .Where(t => t.DeemedDisposalDue && _db.Holdings
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

            // ── CGT year summaries (non-Irish + Irish CGT lots) ───────────────
            var cgtSells = await _db.SellRecords
                .Where(sr => _db.Holdings.Where(h => h.UserId == UserId).Select(h => h.Id).Contains(sr.HoldingId)
                          && sr.TaxType == "CGT")
                .ToListAsync(ct);

            var cgtByYear = cgtSells
                .GroupBy(sr => sr.SellDate.Year)
                .Select(g => new TaxYearSummaryDto
                {
                    Year = g.Key,
                    TotalProfits = g.Where(r => r.TotalProfit > 0).Sum(r => r.TotalProfit),
                    TotalLosses = g.Where(r => r.TotalProfit < 0).Sum(r => r.TotalProfit),
                    NetGain = g.Sum(r => r.TotalProfit),
                    TaxFreeAllowance = annualAllowance,
                    TaxableGain = Math.Max(0, g.Sum(r => r.TotalProfit) - annualAllowance),
                    TaxDue = Math.Max(0, g.Sum(r => r.TotalProfit) - annualAllowance) * (projSettings?.CgtPercent ?? 33m) / 100m,
                    Status = "Pending"
                })
                .OrderBy(y => y.Year)
                .ToList();

            // ── Exit Tax pots (Irish only, per asset per year) ────────────────
            var exitTaxPots = new List<ExitTaxPotDto>();
            if (isIrishInvestor)
            {
                var exitSells = await _db.SellRecords
                    .Include(sr => sr.Holding)
                    .Where(sr => _db.Holdings.Where(h => h.UserId == UserId).Select(h => h.Id).Contains(sr.HoldingId)
                              && sr.TaxType == "ExitTax")
                    .ToListAsync(ct);

                foreach (var group in exitSells.GroupBy(sr => new { sr.HoldingId, Year = sr.SellDate.Year }))
                {
                    var profits = group.Where(r => r.TotalProfit > 0).Sum(r => r.TotalProfit);
                    var losses = group.Where(r => r.TotalProfit < 0).Sum(r => r.TotalProfit);
                    var netGain = profits + losses;

                    // Deemed disposal credit: sum of deemed disposal tax events for this holding before end of year
                    var yearEnd = new DateOnly(group.Key.Year, 12, 31);
                    var ddCredit = await _db.TaxEvents
                        .Where(te => te.HoldingId == group.Key.HoldingId
                                  && te.EventType == TaxEventType.DeemedDisposal
                                  && te.EventDate <= yearEnd)
                        .SumAsync(te => te.TaxAmount, ct);

                    var taxable = Math.Max(0m, netGain - ddCredit);
                    exitTaxPots.Add(new ExitTaxPotDto
                    {
                        HoldingId = group.Key.HoldingId,
                        Ticker = group.First().Holding?.Ticker ?? string.Empty,
                        Year = group.Key.Year,
                        TotalProfits = profits,
                        TotalLosses = losses,
                        DeemedDisposalCreditUsed = ddCredit,
                        NetTaxableGain = taxable,
                        TaxDue = taxable * (projSettings?.ExitTaxPercent ?? 41m) / 100m,
                        Status = "Pending"
                    });
                }
                exitTaxPots = exitTaxPots.OrderBy(p => p.Year).ThenBy(p => p.Ticker).ToList();
            }

            // ── Legacy allowance calc (kept for UI backward compat) ───────────
            var allowanceByYear = new List<TaxYearAllowanceSummaryDto>();
            decimal totalPendingAfterAllowance;
            if (annualAllowance > 0)
            {
                var pendingByYear = dtos.Where(e => e.Status == "Pending")
                    .GroupBy(e => e.EventDate.Year).OrderBy(g => g.Key);
                foreach (var yg in pendingByYear)
                {
                    var totalGain = yg.Sum(e => e.TaxableGain);
                    var taxBefore = yg.Sum(e => e.TaxAmount);
                    var applied = Math.Min(annualAllowance, taxBefore);
                    var taxAfter = Math.Round(Math.Max(0m, taxBefore - applied), 2);
                    allowanceByYear.Add(new TaxYearAllowanceSummaryDto
                    {
                        Year = yg.Key,
                        TotalTaxableGain = totalGain,
                        TaxBeforeAllowance = taxBefore,
                        AllowanceApplied = applied,
                        TaxAfterAllowance = taxAfter
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
                IsIrishInvestor = isIrishInvestor,
                NextDeemedDisposalDate = nextDd,
                Events = dtos,
                CgtByYear = cgtByYear,
                ExitTaxPots = exitTaxPots,
                AnnualTaxFreeAllowance = annualAllowance,
                AllowanceByYear = allowanceByYear,
                TotalPendingAfterAllowance = totalPendingAfterAllowance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tax events");
            return StatusCode(500, new { message = "Error retrieving tax events" });
        }
    }

    /// <summary>Recalculate year-end tax for a given year and upsert into annual_tax_summary.</summary>
    [HttpPost("recalculate-year")]
    public async Task<ActionResult<RecalculateTaxYearResultDto>> RecalculateTaxYear(
        [FromQuery] int year, CancellationToken ct = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "Read-only profile." });

            var projSettings = await _db.ProjectionSettings
                .FirstOrDefaultAsync(ps => ps.UserId == UserId, ct);
            var isIrishInvestor = projSettings?.IsIrishInvestor ?? false;
            var cgtRate = projSettings?.CgtPercent ?? 33m;
            var exitTaxRate = projSettings?.ExitTaxPercent ?? 41m;
            var annualAllowance = (!isIrishInvestor && (projSettings?.TaxFreeAllowancePerYear ?? 0) > 0)
                ? projSettings!.TaxFreeAllowancePerYear
                : 0m;

            var userHoldingIds = await _db.Holdings
                .Where(h => h.UserId == UserId)
                .Select(h => h.Id)
                .ToListAsync(ct);

            var utcNow = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);

            // ── CGT ────────────────────────────────────────────────────────────
            var cgtSells = await _db.SellRecords
                .Where(sr => userHoldingIds.Contains(sr.HoldingId)
                          && sr.SellDate.Year == year
                          && sr.TaxType == "CGT")
                .ToListAsync(ct);

            var cgtProfits = cgtSells.Where(r => r.TotalProfit > 0).Sum(r => r.TotalProfit);
            var cgtLosses = cgtSells.Where(r => r.TotalProfit < 0).Sum(r => r.TotalProfit);
            var cgtNet = cgtProfits + cgtLosses;
            var cgtTaxableGain = Math.Max(0m, cgtNet - annualAllowance);
            var cgtTaxDue = cgtTaxableGain * cgtRate / 100m;

            // Upsert CGT annual summary
            await UpsertAnnualSummaryAsync(UserId, year, "CGT", null,
                cgtProfits, cgtLosses, cgtNet, annualAllowance, 0m, cgtTaxableGain, cgtTaxDue, cgtRate, utcNow, ct);

            // ── Exit Tax (Irish only) ──────────────────────────────────────────
            var exitTaxPots = new List<ExitTaxPotDto>();
            if (isIrishInvestor)
            {
                var exitSells = await _db.SellRecords
                    .Include(sr => sr.Holding)
                    .Where(sr => userHoldingIds.Contains(sr.HoldingId)
                              && sr.SellDate.Year == year
                              && sr.TaxType == "ExitTax")
                    .ToListAsync(ct);

                foreach (var group in exitSells.GroupBy(sr => sr.HoldingId))
                {
                    var profits = group.Where(r => r.TotalProfit > 0).Sum(r => r.TotalProfit);
                    var losses = group.Where(r => r.TotalProfit < 0).Sum(r => r.TotalProfit);
                    var netGain = profits + losses;
                    var yearEnd = new DateOnly(year, 12, 31);
                    var ddCredit = await _db.TaxEvents
                        .Where(te => te.HoldingId == group.Key
                                  && te.EventType == TaxEventType.DeemedDisposal
                                  && te.EventDate <= yearEnd)
                        .SumAsync(te => te.TaxAmount, ct);

                    var taxable = Math.Max(0m, netGain - ddCredit);
                    var taxDue = taxable * exitTaxRate / 100m;

                    await UpsertAnnualSummaryAsync(UserId, year, "ExitTax", group.Key,
                        profits, losses, netGain, 0m, ddCredit, taxable, taxDue, exitTaxRate, utcNow, ct);

                    exitTaxPots.Add(new ExitTaxPotDto
                    {
                        HoldingId = group.Key,
                        Ticker = group.First().Holding?.Ticker ?? string.Empty,
                        Year = year,
                        TotalProfits = profits,
                        TotalLosses = losses,
                        DeemedDisposalCreditUsed = ddCredit,
                        NetTaxableGain = taxable,
                        TaxDue = taxDue,
                        Status = "Pending"
                    });
                }
            }

            return Ok(new RecalculateTaxYearResultDto
            {
                Year = year,
                CgtTaxDue = cgtTaxDue,
                ExitTaxPots = exitTaxPots,
                TotalTaxDue = cgtTaxDue + exitTaxPots.Sum(p => p.TaxDue)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating tax for year {Year}", year);
            return StatusCode(500, new { message = "Error recalculating tax" });
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
            foreach (var e in events) { e.Status = TaxEventStatus.Paid; e.PaidAt = paidAt; }
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

    private async Task UpsertAnnualSummaryAsync(
        int userId, int year, string taxType, int? holdingId,
        decimal profits, decimal losses, decimal netGain,
        decimal allowance, decimal ddCredit, decimal taxableGain, decimal taxDue, decimal taxRate,
        DateTime utcNow, CancellationToken ct)
    {
        var existing = await _db.AnnualTaxSummaries
            .FirstOrDefaultAsync(a => a.UserId == userId && a.TaxYear == year
                                   && a.TaxType == taxType && a.HoldingId == holdingId, ct);
        if (existing == null)
        {
            _db.AnnualTaxSummaries.Add(new AnnualTaxSummary
            {
                UserId = userId,
                TaxYear = year,
                TaxType = taxType,
                HoldingId = holdingId,
                TotalProfits = profits,
                TotalLosses = losses,
                NetGain = netGain,
                AllowanceApplied = allowance,
                DeemedDisposalCredit = ddCredit,
                TaxableGain = taxableGain,
                TaxDue = taxDue,
                TaxRateUsed = taxRate,
                Status = "Pending",
                RecalculatedAt = utcNow
            });
        }
        else
        {
            existing.TotalProfits = profits;
            existing.TotalLosses = losses;
            existing.NetGain = netGain;
            existing.AllowanceApplied = allowance;
            existing.DeemedDisposalCredit = ddCredit;
            existing.TaxableGain = taxableGain;
            existing.TaxDue = taxDue;
            existing.TaxRateUsed = taxRate;
            existing.RecalculatedAt = utcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    private static TaxEventDto MapToDto(TaxEvent te) => new()
    {
        Id = te.Id,
        HoldingId = te.HoldingId,
        Ticker = te.Holding?.Ticker ?? string.Empty,
        EtfName = te.Holding?.EtfName,
        BuyTransactionId = te.BuyTransactionId,
        SellRecordId = te.SellRecordId,
        EventType = te.EventType.ToString(),
        TaxSubType = te.TaxSubType,
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

