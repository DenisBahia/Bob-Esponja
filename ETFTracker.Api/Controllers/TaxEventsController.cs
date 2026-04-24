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
            var projSettings = await _db.UserSettings
                .FirstOrDefaultAsync(ps => ps.UserId == UserId, ct);
            var isIrishInvestor = projSettings?.IsIrishInvestor ?? false;
            var annualAllowance = ((projSettings?.TaxFreeAllowancePerYear ?? 0) > 0)
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

            // For CGT sell events the DB stores TaxAmount=0 (year-end netting required).
            // Set a provisional amount so the UI shows a meaningful estimate.
            var cgtRateForProvisional = projSettings?.CgtPercent ?? 33m;
            foreach (var dto in dtos.Where(d => d.EventType == "Sell" && d.TaxSubType == "CGT" && d.TaxAmount == 0m && d.TaxableGain > 0))
                dto.TaxAmount = Math.Round(dto.TaxableGain * dto.TaxRateUsed / 100m, 2);

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

            // Load AnnualTaxSummary statuses so we can reflect "Paid" on the year rows
            var annualSummaries = await _db.AnnualTaxSummaries
                .Where(a => a.UserId == UserId && a.TaxType == "CGT" && a.HoldingId == null)
                .ToListAsync(ct);
            var annualSummaryStatuses = annualSummaries.ToDictionary(a => a.TaxYear, a => a.Status);

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
                    Status = annualSummaryStatuses.TryGetValue(g.Key, out var s) ? s : "Pending"
                })
                .OrderBy(y => y.Year)
                .ToList();

            // ── Exit Tax pots (Irish only, per asset per year) ────────────────
            var exitTaxPots = new List<ExitTaxPotDto>();
            var exitAnnualSummaryList = new List<AnnualTaxSummary>();
            if (isIrishInvestor)
            {
                var exitSells = await _db.SellRecords
                    .Include(sr => sr.Holding)
                    .Where(sr => _db.Holdings.Where(h => h.UserId == UserId).Select(h => h.Id).Contains(sr.HoldingId)
                              && sr.TaxType == "ExitTax")
                    .ToListAsync(ct);

                exitAnnualSummaryList = await _db.AnnualTaxSummaries
                    .Where(a => a.UserId == UserId && a.TaxType == "ExitTax")
                    .ToListAsync(ct);
                var exitAnnualStatuses = exitAnnualSummaryList
                    .ToDictionary(a => (a.HoldingId, a.TaxYear), a => a.Status);

                foreach (var group in exitSells.GroupBy(sr => new { sr.HoldingId, Year = sr.SellDate.Year }))
                {
                    var profits = group.Where(r => r.TotalProfit > 0).Sum(r => r.TotalProfit);
                    var losses = group.Where(r => r.TotalProfit < 0).Sum(r => r.TotalProfit);
                    var netGain = profits + losses;

                    // Deemed disposal credit: taxable gain from deemed disposal events up to and including sell year,
                    // scaled proportionally to the quantity sold vs total buy quantity for this holding.
                    var totalDdCreditRaw = await _db.TaxEvents
                        .Where(te => te.HoldingId == group.Key.HoldingId
                                  && te.EventType == TaxEventType.DeemedDisposal
                                  && te.EventDate.Year <= group.Key.Year)
                        .SumAsync(te => te.TaxableGain, ct);

                    var totalBuyQty = await _db.Transactions
                        .Where(t => t.HoldingId == group.Key.HoldingId)
                        .SumAsync(t => t.Quantity, ct);

                    var qtySold = group.Sum(r => r.Quantity);
                    var ratio = (totalBuyQty > 0m) ? Math.Min(1m, qtySold / totalBuyQty) : 0m;
                    var ddCredit = Math.Round(totalDdCreditRaw * ratio, 2);

                    var taxable = Math.Max(0m, netGain - ddCredit);
                    var potStatus = exitAnnualStatuses.TryGetValue((group.Key.HoldingId, group.Key.Year), out var ps) ? ps : "Pending";
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
                        Status = potStatus
                    });
                }
                exitTaxPots = exitTaxPots.OrderBy(p => p.Year).ThenBy(p => p.Ticker).ToList();
            }

            // ── Allowance breakdown — sourced from CgtByYear for accuracy ────────
            // (Individual TaxEvents store TaxAmount=0 for CGT; year-level is correct.)
            var allowanceByYear = new List<TaxYearAllowanceSummaryDto>();
            decimal totalPendingAfterAllowance;
            if (annualAllowance > 0)
            {
                var cgtRate = projSettings?.CgtPercent ?? 33m;
                foreach (var yr in cgtByYear)
                {
                    var taxBefore = Math.Round(Math.Max(0m, yr.NetGain) * cgtRate / 100m, 2);
                    var allowanceSaving = Math.Round(Math.Min(annualAllowance, Math.Max(0m, yr.NetGain)) * cgtRate / 100m, 2);
                    allowanceByYear.Add(new TaxYearAllowanceSummaryDto
                    {
                        Year = yr.Year,
                        TotalTaxableGain = yr.NetGain,
                        TaxBeforeAllowance = taxBefore,
                        AllowanceApplied = allowanceSaving,
                        TaxAfterAllowance = yr.TaxDue
                    });
                }
                // After-allowance total = CGT pending (from cgtByYear) + non-CGT pending events
                // For Irish: ExitTax pending uses exitTaxPots statuses, NOT raw TaxEvents
                var nonCgtPending = isIrishInvestor
                    ? dtos.Where(e => e.Status == "Pending" && e.TaxSubType != "CGT" && e.TaxSubType != "ExitTax").Sum(e => e.TaxAmount)
                      + exitTaxPots.Where(p => p.Status == "Pending").Sum(p => p.TaxDue)
                    : dtos.Where(e => e.Status == "Pending" && e.TaxSubType != "CGT").Sum(e => e.TaxAmount);
                totalPendingAfterAllowance = cgtByYear.Where(y => y.Status == "Pending").Sum(y => y.TaxDue) + nonCgtPending;
            }
            else
            {
                // For Irish: ExitTax pending uses exitTaxPots statuses
                totalPendingAfterAllowance = isIrishInvestor
                    ? cgtByYear.Where(y => y.Status == "Pending").Sum(y => y.TaxDue)
                      + dtos.Where(e => e.Status == "Pending" && e.TaxSubType != "CGT" && e.TaxSubType != "ExitTax").Sum(e => e.TaxAmount)
                      + exitTaxPots.Where(p => p.Status == "Pending").Sum(p => p.TaxDue)
                    : dtos.Where(e => e.Status == "Pending").Sum(e => e.TaxAmount);
            }

            // TotalPending: use CgtByYear for CGT portion (accurate year-level calc) + non-CGT events
            // For Irish: ExitTax pending uses exitTaxPots statuses (AnnualTaxSummary is source of truth)
            var nonCgtPendingTotal = isIrishInvestor
                ? dtos.Where(e => e.Status == "Pending" && e.TaxSubType != "CGT" && e.TaxSubType != "ExitTax").Sum(e => e.TaxAmount)
                  + exitTaxPots.Where(p => p.Status == "Pending").Sum(p => p.TaxDue)
                : dtos.Where(e => e.Status == "Pending" && e.TaxSubType != "CGT").Sum(e => e.TaxAmount);
            var cgtPendingTotal = cgtByYear.Where(y => y.Status == "Pending").Sum(y => y.TaxDue);

            // ExitTax paid: use exitTaxPots live TaxDue for paid pots (accurate even on first mark-paid)
            var exitTaxPaidTotal = isIrishInvestor
                ? exitTaxPots.Where(p => p.Status == "Paid").Sum(p => p.TaxDue)
                : 0m;

            return Ok(new TaxSummaryDto
            {
                // TotalPaid = non-CGT/non-ExitTax paid events + CGT annual paid snapshots + ExitTax annual paid snapshots
                TotalPaid = dtos.Where(e => e.Status == "Paid" && e.TaxSubType != "CGT" && e.TaxSubType != "ExitTax").Sum(e => e.TaxAmount)
                          + annualSummaries.Where(a => a.Status == "Paid").Sum(a => a.PaidTaxAmount)
                          + exitTaxPaidTotal,
                TotalPending = cgtPendingTotal + nonCgtPendingTotal,
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

            var projSettings = await _db.UserSettings
                .FirstOrDefaultAsync(ps => ps.UserId == UserId, ct);
            var isIrishInvestor = projSettings?.IsIrishInvestor ?? false;
            var cgtRate = projSettings?.CgtPercent ?? 33m;
            var exitTaxRate = projSettings?.ExitTaxPercent ?? 41m;
            var annualAllowance = ((projSettings?.TaxFreeAllowancePerYear ?? 0) > 0)
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

                    // Deemed disposal credit: taxable gain from DD events up to and including sell year, scaled to qty sold / total buy qty.
                    var totalDdCreditRaw = await _db.TaxEvents
                        .Where(te => te.HoldingId == group.Key
                                  && te.EventType == TaxEventType.DeemedDisposal
                                  && te.EventDate.Year <= year)
                        .SumAsync(te => te.TaxableGain, ct);

                    var totalBuyQty = await _db.Transactions
                        .Where(t => t.HoldingId == group.Key)
                        .SumAsync(t => t.Quantity, ct);

                    var qtySold = group.Sum(r => r.Quantity);
                    var ratio = (totalBuyQty > 0m) ? Math.Min(1m, qtySold / totalBuyQty) : 0m;
                    var ddCredit = Math.Round(totalDdCreditRaw * ratio, 2);


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

    /// <summary>Mark an entire CGT year as paid via the AnnualTaxSummary record.
    /// Creates the record if it does not exist yet.</summary>
    [HttpPut("mark-year-paid/{year:int}")]
    public async Task<ActionResult<object>> MarkYearPaid(int year, CancellationToken ct = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "Read-only profile." });

            var existing = await _db.AnnualTaxSummaries
                .FirstOrDefaultAsync(a => a.UserId == UserId && a.TaxYear == year
                                       && a.TaxType == "CGT" && a.HoldingId == null, ct);

            if (existing != null)
            {
                existing.PaidTaxAmount = existing.TaxDue;
                existing.Status = "Paid";
                existing.RecalculatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.AnnualTaxSummaries.Add(new AnnualTaxSummary
                {
                    UserId = UserId,
                    TaxYear = year,
                    TaxType = "CGT",
                    HoldingId = null,
                    Status = "Paid",
                    PaidTaxAmount = 0m,
                    RecalculatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(ct);
            return Ok(new { year, status = "Paid" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking year {Year} as paid", year);
            return StatusCode(500, new { message = "Error updating year status" });
        }
    }

    [HttpPut("mark-exit-tax-year-paid/{year:int}")]
    public async Task<ActionResult<object>> MarkExitTaxYearPaid(int year, CancellationToken ct = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "Read-only profile." });

            var holdingIds = await _db.Holdings
                .Where(h => h.UserId == UserId).Select(h => h.Id).ToListAsync(ct);

            var existingPots = await _db.AnnualTaxSummaries
                .Where(a => a.UserId == UserId && a.TaxType == "ExitTax" && a.TaxYear == year)
                .ToListAsync(ct);

            foreach (var pot in existingPots)
            {
                pot.PaidTaxAmount = pot.TaxDue;
                pot.Status = "Paid";
                pot.RecalculatedAt = DateTime.UtcNow;
            }

            var coveredHoldings = existingPots.Select(p => p.HoldingId).ToHashSet();
            var holdingsWithSells = await _db.SellRecords
                .Where(sr => holdingIds.Contains(sr.HoldingId) && sr.TaxType == "ExitTax" && sr.SellDate.Year == year)
                .Select(sr => sr.HoldingId).Distinct().ToListAsync(ct);

            foreach (var hId in holdingsWithSells.Where(h => !coveredHoldings.Contains(h)))
            {
                _db.AnnualTaxSummaries.Add(new AnnualTaxSummary
                {
                    UserId = UserId, TaxYear = year, TaxType = "ExitTax",
                    HoldingId = hId, Status = "Paid", PaidTaxAmount = 0m,
                    RecalculatedAt = DateTime.UtcNow
                });
            }

            // Also mark any Deemed Disposal TaxEvents for this year as Paid
            var deemedDisposalEvents = await _db.TaxEvents
                .Where(te => holdingIds.Contains(te.HoldingId)
                          && te.EventType == TaxEventType.DeemedDisposal
                          && te.EventDate.Year == year
                          && te.Status == TaxEventStatus.Pending)
                .ToListAsync(ct);

            foreach (var te in deemedDisposalEvents)
            {
                te.Status = TaxEventStatus.Paid;
                te.PaidAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            return Ok(new { year, status = "Paid" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking exit tax year {Year} as paid", year);
            return StatusCode(500, new { message = "Error updating exit tax year status" });
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
            // Reset to Pending only when the TaxDue meaningfully changed from a previously
            // calculated value. If existing.TaxDue is 0 it means the record was created by
            // "Mark Paid" before any recalculation had run (placeholder) — just update the
            // numbers and keep the Paid status; don't punish the user with a spurious reset.
            var taxDueChanged = Math.Round(existing.TaxDue, 2) != Math.Round(taxDue, 2);
            var wasRealValue  = existing.TaxDue != 0m;
            if (taxDueChanged && wasRealValue)
            {
                existing.Status = "Pending";
                existing.PaidTaxAmount = 0m;
            }
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

