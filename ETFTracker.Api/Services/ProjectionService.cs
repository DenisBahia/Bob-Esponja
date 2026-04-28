using Microsoft.EntityFrameworkCore;
using ETFTracker.Api.Data;
using ETFTracker.Api.Dtos;
using ETFTracker.Api.Models;

namespace ETFTracker.Api.Services;

public interface IProjectionService
{
    Task<ProjectionResultDto> GetProjectionAsync(int userId, CancellationToken ct = default);
    Task<ProjectionResultDto> CalculateAsync(int userId, ProjectionSettingsDto settings, CancellationToken ct = default);
    Task<ProjectionSettingsDto> SaveSettingsAsync(int userId, ProjectionSettingsDto dto, CancellationToken ct = default);
    Task<ProjectionVersionSummaryDto> SaveVersionAsync(int userId, SaveVersionRequestDto dto, CancellationToken ct = default);
    Task<List<ProjectionVersionSummaryDto>> GetVersionsAsync(int userId, CancellationToken ct = default);
    Task<ProjectionVersionDetailDto?> GetVersionDetailAsync(int userId, int versionId, CancellationToken ct = default);
    Task<bool> DeleteVersionAsync(int userId, int versionId, CancellationToken ct = default);
    Task<ProjectionVersionSummaryDto?> SetDefaultVersionAsync(int userId, int versionId, CancellationToken ct = default);
}

public class ProjectionService : IProjectionService
{
    private readonly AppDbContext _context;
    private readonly IPriceService _priceService;
    private readonly ILogger<ProjectionService> _logger;

    private static readonly ProjectionSettingsDto DefaultSettings = new()
    {
        YearlyReturnPercent      = 7m,
        MonthlyBuyAmount         = 500m,
        AnnualBuyIncreasePercent = 3m,
        ProjectionYears          = 10,
        InflationPercent         = 2m,
        ApplyDeemedDisposal      = false,
        DeemedDisposalPercent    = 0m,
    };

    public ProjectionService(AppDbContext context, IPriceService priceService, ILogger<ProjectionService> logger)
    {
        _context = context;
        _priceService = priceService;
        _logger = logger;
    }

    public async Task<ProjectionResultDto> GetProjectionAsync(int userId, CancellationToken ct = default)
    {
        _logger.LogDebug("Calculating projection for user {UserId}", userId);

        var dbSettings = await _context.ProjectionSettings
            .FirstOrDefaultAsync(ps => ps.UserId == userId, ct);

        var settings = dbSettings != null
            ? new ProjectionSettingsDto
            {
                YearlyReturnPercent      = dbSettings.YearlyReturnPercent,
                MonthlyBuyAmount         = dbSettings.MonthlyBuyAmount,
                AnnualBuyIncreasePercent = dbSettings.AnnualBuyIncreasePercent,
                ProjectionYears          = dbSettings.ProjectionYears,
                InflationPercent         = dbSettings.InflationPercent,
                StartAmount              = dbSettings.StartAmount,
                ApplyDeemedDisposal      = dbSettings.ApplyDeemedDisposal,
            }
            : new ProjectionSettingsDto
            {
                YearlyReturnPercent      = DefaultSettings.YearlyReturnPercent,
                MonthlyBuyAmount         = DefaultSettings.MonthlyBuyAmount,
                AnnualBuyIncreasePercent = DefaultSettings.AnnualBuyIncreasePercent,
                ProjectionYears          = DefaultSettings.ProjectionYears,
                InflationPercent         = DefaultSettings.InflationPercent,
                StartAmount              = null,
                ApplyDeemedDisposal      = false,
            };

        // Always resolve tax rates from user settings
        settings.CgtPercent           = await GetTaxRateAsync(userId, settings.ApplyDeemedDisposal, ct);
        if (settings.ApplyDeemedDisposal)
            settings.DeemedDisposalPercent = await GetDeemedDisposalPercentAsync(userId, ct);

        var dataPoints = await ComputeDataPointsAsync(userId, settings, ct);
        return new ProjectionResultDto { Settings = settings, DataPoints = dataPoints };
    }

    /// <summary>
    /// Calculates a projection for the given settings WITHOUT saving anything to the database.
    /// Used to preview a specific scenario (e.g. loading a saved version's settings).
    /// </summary>
    public async Task<ProjectionResultDto> CalculateAsync(int userId, ProjectionSettingsDto settings, CancellationToken ct = default)
    {
        // Always override tax rates from user settings — ignore whatever the client sent
        settings.CgtPercent           = await GetTaxRateAsync(userId, settings.ApplyDeemedDisposal, ct);
        if (settings.ApplyDeemedDisposal)
            settings.DeemedDisposalPercent = await GetDeemedDisposalPercentAsync(userId, ct);

        var dataPoints = await ComputeDataPointsAsync(userId, settings, ct);
        return new ProjectionResultDto { Settings = settings, DataPoints = dataPoints };
    }

    /// <summary>
    /// Core calculation: builds projection data points for the given settings and current portfolio.
    /// When ApplyDeemedDisposal is false: tax is applied once in the final year only (original behaviour).
    /// When ApplyDeemedDisposal is true: 8-year DD events fire per contribution cohort; final-year exit tax
    /// is calculated on (totalProfit − totalDDPaid) to avoid double taxation.
    /// </summary>
    private async Task<List<ProjectionDataPointDto>> ComputeDataPointsAsync(
        int userId, ProjectionSettingsDto settings, CancellationToken ct)
    {
        // ── Current portfolio value ───────────────────────────────────────────
        var holdings = await _context.Holdings
            .Where(h => h.UserId == userId)
            .ToListAsync(ct);

        decimal currentTotal = 0m;
        foreach (var holding in holdings)
        {
            var price = await _priceService.GetPriceAsync(holding.Ticker, ct);
            currentTotal += holding.Quantity * (price ?? 0m);
        }

        if (settings.StartAmount.HasValue)
            currentTotal = settings.StartAmount.Value;

        // ── Months remaining in current year ──────────────────────────────────
        var today = DateTime.UtcNow;
        var currentYear = today.Year;
        var currentMonth = today.Month;

        var hasBuyThisMonth = await _context.Transactions
            .AnyAsync(t => t.Holding!.UserId == userId
                        && t.PurchaseDate.Year  == currentYear
                        && t.PurchaseDate.Month == currentMonth, ct);

        var monthsRemaining = hasBuyThisMonth ? 12 - currentMonth : 12 - currentMonth + 1;

        int N = settings.ProjectionYears;

        // ── Gross projection arrays ───────────────────────────────────────────
        var endOfYear      = new decimal[N + 1];
        var initialBalance = new decimal[N + 1];
        var totalBuys      = new decimal[N + 1];
        var yearProfit     = new decimal[N + 1];

        var partialGrowthFactor = (decimal)Math.Pow(
            (double)(1 + settings.YearlyReturnPercent / 100m),
            monthsRemaining / 12.0);

        initialBalance[0] = currentTotal;
        totalBuys[0]      = monthsRemaining * settings.MonthlyBuyAmount;
        endOfYear[0]      = (currentTotal + totalBuys[0]) * partialGrowthFactor;
        yearProfit[0]     = endOfYear[0] - currentTotal - totalBuys[0];

        for (int i = 1; i <= N; i++)
        {
            var annualIncreaseFactor = (decimal)Math.Pow((double)(1 + settings.AnnualBuyIncreasePercent / 100m), i);
            var contributions = 12m * settings.MonthlyBuyAmount * annualIncreaseFactor;
            var growthFactor  = 1m + settings.YearlyReturnPercent / 100m;

            initialBalance[i] = endOfYear[i - 1];
            totalBuys[i]      = contributions;
            endOfYear[i]      = (endOfYear[i - 1] + contributions) * growthFactor;
            yearProfit[i]     = endOfYear[i] - endOfYear[i - 1] - contributions;
        }

        // ── Total invested (same for both tax modes) ──────────────────────────
        decimal totalAmountInvested = currentTotal;
        for (int i = 0; i <= N; i++)
            totalAmountInvested += totalBuys[i];

        // ── Deemed Disposal engine (Irish investors only) ─────────────────────
        // Each cohort c (0 = existing portfolio, 1..N = annual contributions) grows
        // independently. A DD event fires for cohort c at year y when (y-c) % 8 == 0.
        // The cost basis is stepped up after each DD event to prevent double taxation.
        var ddPaidPerYear = new decimal[N + 1];
        decimal totalDDPaid = 0m;

        if (settings.ApplyDeemedDisposal && settings.DeemedDisposalPercent > 0m)
        {
            decimal r = settings.YearlyReturnPercent / 100m;

            // cohortOriginalAmount[c]: original contribution amount of cohort c
            // cohortBasis[c]: current cost basis (stepped up after DD events)
            var cohortOriginalAmount = new decimal[N + 1];
            var cohortBasis          = new decimal[N + 1];

            cohortOriginalAmount[0] = currentTotal;
            cohortBasis[0]          = currentTotal;

            for (int i = 1; i <= N; i++)
            {
                var annualIncreaseFactor = (decimal)Math.Pow((double)(1 + settings.AnnualBuyIncreasePercent / 100m), i);
                cohortOriginalAmount[i] = 12m * settings.MonthlyBuyAmount * annualIncreaseFactor;
                cohortBasis[i]          = cohortOriginalAmount[i];
            }

            // Helper: value of cohort c at end of year y (simple compound growth)
            decimal CohortValue(int c, int y) =>
                cohortOriginalAmount[c] * (decimal)Math.Pow((double)(1m + r), y - c);

            for (int y = 1; y <= N; y++)
            {
                for (int c = 0; c <= y; c++)
                {
                    int age = y - c;
                    if (age > 0 && age % 8 == 0)
                    {
                        var value   = CohortValue(c, y);
                        var profit  = Math.Max(0m, value - cohortBasis[c]);
                        var ddTax   = Math.Round(profit * settings.DeemedDisposalPercent / 100m, 2);

                        ddPaidPerYear[y] += ddTax;
                        totalDDPaid      += ddTax;

                        // Step up cost basis so future events don't re-tax the same profit
                        cohortBasis[c] = value;
                    }
                }
            }
        }

        // ── SIA engine (Irish investors only — Gov. plan from 2027+) ─────────────
        // Simple flat annual charge: siaAnnualPercent × end-of-year portfolio value.
        // Computed as a parallel scenario — does not affect the existing DD/exit-tax columns.
        var siaPercent = 0m;
        {
            var usSia = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId, ct);
            if (usSia?.IsIrishInvestor == true && usSia.SiaAnnualPercent > 0m)
                siaPercent = usSia.SiaAnnualPercent;
        }

        var siaTaxPerYear = new decimal[N + 1];
        if (siaPercent > 0m)
        {
            for (int i = 0; i <= N; i++)
                siaTaxPerYear[i] = Math.Round(endOfYear[i] * siaPercent / 100m, 2);
        }

        // ── Final-year exit/CGT tax ───────────────────────────────────────────
        decimal taxAtEnd;
        if (settings.ApplyDeemedDisposal)
        {
            // Irish Revenue rule: exit tax is calculated on the FULL gain at exit,
            // then DD taxes already paid are deducted as a credit (not from the profit base).
            // Formula: max(0, (totalProfit × exitTaxRate) − totalDDPaid)
            var grossTotalProfit = Math.Max(0m, endOfYear[N] - totalAmountInvested);
            var grossExitTax     = Math.Round(grossTotalProfit * settings.CgtPercent / 100m, 2);
            taxAtEnd             = Math.Max(0m, grossExitTax - totalDDPaid);
        }
        else
        {
            var totalProfit = Math.Max(0m, endOfYear[N] - totalAmountInvested);
            taxAtEnd        = Math.Round(totalProfit * settings.CgtPercent / 100m, 2);
        }

        // ── Build data points ─────────────────────────────────────────────────
        var dataPoints = new List<ProjectionDataPointDto>();
        decimal cumulativeDDPaid = 0m;
        decimal cumulativeSiaPaid = 0m;

        for (int i = 0; i <= N; i++)
        {
            var year              = currentYear + i;
            var inflationFactor   = (decimal)Math.Pow((double)(1 + settings.InflationPercent / 100m), i);
            var isLastYear        = (i == N);
            var ddThisYear        = ddPaidPerYear[i];
            var taxPaid           = isLastYear ? taxAtEnd : 0m;

            cumulativeDDPaid  += ddThisYear;
            cumulativeSiaPaid += siaTaxPerYear[i];

            // After-tax balance = gross EoY value minus ALL taxes paid up to and including this year.
            // This reflects the real net position: every euro of DD paid in prior years
            // has already left the portfolio and cannot compound further.
            var afterTax    = Math.Max(0m, endOfYear[i] - cumulativeDDPaid - taxPaid);
            var afterSia    = Math.Max(0m, endOfYear[i] - cumulativeSiaPaid);

            dataPoints.Add(new ProjectionDataPointDto
            {
                Year                             = year,
                InitialBalance                   = Math.Round(initialBalance[i], 2),
                TotalBuys                        = Math.Round(totalBuys[i], 2),
                YearProfit                       = Math.Round(yearProfit[i], 2),
                TotalAmount                      = Math.Round(endOfYear[i], 2),
                InflationCorrectedAmount         = Math.Round(endOfYear[i] / inflationFactor, 2),
                DeemedDisposalPaid               = Math.Round(ddThisYear, 2),
                TaxPaid                          = Math.Round(taxPaid, 2),
                AfterTaxTotalAmount              = Math.Round(afterTax, 2),
                AfterTaxInflationCorrectedAmount = Math.Round(afterTax / inflationFactor, 2),
                SiaTaxDue                        = Math.Round(siaTaxPerYear[i], 2),
                AfterSiaTotalAmount              = Math.Round(afterSia, 2),
            });
        }

        return dataPoints;
    }

    /// <summary>
    /// Resolves the final-exit tax rate from UserSettings.
    /// Irish investor with DD on → ExitTaxPercent.
    /// Irish investor with DD off → ExitTaxPercent.
    /// Non-Irish investor → CgtPercent.
    /// </summary>
    private async Task<decimal> GetTaxRateAsync(int userId, bool applyDeemedDisposal, CancellationToken ct)
    {
        var us = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (us == null)
        {
            _logger.LogWarning(
                "No UserSettings found for user {UserId} — tax rate cannot be resolved; defaulting to 0%.",
                userId);
            return 0m;
        }
        return us.IsIrishInvestor ? us.ExitTaxPercent : us.CgtPercent;
    }

    /// <summary>Reads the user's Deemed Disposal % from UserSettings (Irish regime only).</summary>
    private async Task<decimal> GetDeemedDisposalPercentAsync(int userId, CancellationToken ct)
    {
        var us = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (us == null)
        {
            _logger.LogWarning(
                "No UserSettings found for user {UserId} — DD % cannot be resolved; DD events will be skipped (0%).",
                userId);
            return 0m;
        }
        return us.DeemedDisposalPercent;
    }

    public async Task<ProjectionSettingsDto> SaveSettingsAsync(int userId, ProjectionSettingsDto dto, CancellationToken ct = default)
    {
        var utcNow = DateTime.UtcNow;

        var existing = await _context.ProjectionSettings
            .FirstOrDefaultAsync(ps => ps.UserId == userId, ct);

        if (existing == null)
        {
            var newSettings = new ProjectionSettings
            {
                UserId                   = userId,
                YearlyReturnPercent      = dto.YearlyReturnPercent,
                MonthlyBuyAmount         = dto.MonthlyBuyAmount,
                AnnualBuyIncreasePercent = dto.AnnualBuyIncreasePercent,
                ProjectionYears          = dto.ProjectionYears,
                InflationPercent         = dto.InflationPercent,
                StartAmount              = dto.StartAmount,
                ApplyDeemedDisposal      = dto.ApplyDeemedDisposal,
                CreatedAt                = utcNow,
                UpdatedAt                = utcNow,
            };
            _context.ProjectionSettings.Add(newSettings);
        }
        else
        {
            existing.YearlyReturnPercent      = dto.YearlyReturnPercent;
            existing.MonthlyBuyAmount         = dto.MonthlyBuyAmount;
            existing.AnnualBuyIncreasePercent = dto.AnnualBuyIncreasePercent;
            existing.ProjectionYears          = dto.ProjectionYears;
            existing.InflationPercent         = dto.InflationPercent;
            existing.StartAmount              = dto.StartAmount;
            existing.ApplyDeemedDisposal      = dto.ApplyDeemedDisposal;
            existing.UpdatedAt                = utcNow;
        }

        await _context.SaveChangesAsync(ct);
        return dto;
    }

    public async Task<ProjectionVersionSummaryDto> SaveVersionAsync(
        int userId, SaveVersionRequestDto dto, CancellationToken ct = default)
    {
        var settings = dto.Settings;
        // Resolve tax rates from user settings before computing
        settings.CgtPercent = await GetTaxRateAsync(userId, settings.ApplyDeemedDisposal, ct);
        if (settings.ApplyDeemedDisposal)
            settings.DeemedDisposalPercent = await GetDeemedDisposalPercentAsync(userId, ct);

        var dataPoints = await ComputeDataPointsAsync(userId, settings, ct);
        var json = System.Text.Json.JsonSerializer.Serialize(dataPoints);

        var entity = new ProjectionVersion
        {
            UserId                   = userId,
            VersionName              = string.IsNullOrWhiteSpace(dto.VersionName) ? "Unnamed" : dto.VersionName.Trim(),
            IsDefault                = false,
            SavedAt                  = DateTime.UtcNow,
            YearlyReturnPercent      = settings.YearlyReturnPercent,
            MonthlyBuyAmount         = settings.MonthlyBuyAmount,
            AnnualBuyIncreasePercent = settings.AnnualBuyIncreasePercent,
            ProjectionYears          = settings.ProjectionYears,
            InflationPercent         = settings.InflationPercent,
            CgtPercent               = settings.CgtPercent,
            StartAmount              = settings.StartAmount,
            ApplyDeemedDisposal      = settings.ApplyDeemedDisposal,
            DeemedDisposalPercent    = settings.DeemedDisposalPercent,
            DataPointsJson           = json,
        };
        _context.ProjectionVersions.Add(entity);
        await _context.SaveChangesAsync(ct);

        return new ProjectionVersionSummaryDto
        {
            Id          = entity.Id,
            VersionName = entity.VersionName,
            IsDefault   = entity.IsDefault,
            SavedAt     = entity.SavedAt,
            Settings    = settings,
            DataPoints  = dataPoints,
        };
    }

    public async Task<List<ProjectionVersionSummaryDto>> GetVersionsAsync(
        int userId, CancellationToken ct = default)
    {
        var entities = await _context.ProjectionVersions
            .Where(pv => pv.UserId == userId)
            .OrderBy(pv => pv.SavedAt)
            .ToListAsync(ct);

        return entities.Select(pv => new ProjectionVersionSummaryDto
        {
            Id          = pv.Id,
            VersionName = pv.VersionName,
            IsDefault   = pv.IsDefault,
            SavedAt     = pv.SavedAt,
            Settings    = new ProjectionSettingsDto
            {
                YearlyReturnPercent      = pv.YearlyReturnPercent,
                MonthlyBuyAmount         = pv.MonthlyBuyAmount,
                AnnualBuyIncreasePercent = pv.AnnualBuyIncreasePercent,
                ProjectionYears          = pv.ProjectionYears,
                InflationPercent         = pv.InflationPercent,
                CgtPercent               = pv.CgtPercent,
                StartAmount              = pv.StartAmount,
                ApplyDeemedDisposal      = pv.ApplyDeemedDisposal,
                DeemedDisposalPercent    = pv.DeemedDisposalPercent,
            },
            DataPoints = System.Text.Json.JsonSerializer.Deserialize<List<ProjectionDataPointDto>>(pv.DataPointsJson)
                         ?? new List<ProjectionDataPointDto>(),
        }).ToList();
    }

    public async Task<ProjectionVersionDetailDto?> GetVersionDetailAsync(
        int userId, int versionId, CancellationToken ct = default)
    {
        var entity = await _context.ProjectionVersions
            .FirstOrDefaultAsync(pv => pv.Id == versionId && pv.UserId == userId, ct);

        if (entity == null) return null;

        var dataPoints = System.Text.Json.JsonSerializer.Deserialize<List<ProjectionDataPointDto>>(entity.DataPointsJson)
                         ?? new List<ProjectionDataPointDto>();

        return new ProjectionVersionDetailDto
        {
            Id          = entity.Id,
            VersionName = entity.VersionName,
            IsDefault   = entity.IsDefault,
            SavedAt     = entity.SavedAt,
            Settings    = new ProjectionSettingsDto
            {
                YearlyReturnPercent      = entity.YearlyReturnPercent,
                MonthlyBuyAmount         = entity.MonthlyBuyAmount,
                AnnualBuyIncreasePercent = entity.AnnualBuyIncreasePercent,
                ProjectionYears          = entity.ProjectionYears,
                InflationPercent         = entity.InflationPercent,
                CgtPercent               = entity.CgtPercent,
                StartAmount              = entity.StartAmount,
                ApplyDeemedDisposal      = entity.ApplyDeemedDisposal,
                DeemedDisposalPercent    = entity.DeemedDisposalPercent,
            },
            DataPoints = dataPoints,
        };
    }

    public async Task<ProjectionVersionSummaryDto?> SetDefaultVersionAsync(
        int userId, int versionId, CancellationToken ct = default)
    {
        var target = await _context.ProjectionVersions
            .FirstOrDefaultAsync(pv => pv.Id == versionId && pv.UserId == userId, ct);

        if (target == null) return null;

        var currentDefaults = await _context.ProjectionVersions
            .Where(pv => pv.UserId == userId && pv.IsDefault)
            .ToListAsync(ct);
        foreach (var v in currentDefaults)
            v.IsDefault = false;

        target.IsDefault = true;
        await _context.SaveChangesAsync(ct);

        return new ProjectionVersionSummaryDto
        {
            Id          = target.Id,
            VersionName = target.VersionName,
            IsDefault   = target.IsDefault,
            SavedAt     = target.SavedAt,
            Settings    = new ProjectionSettingsDto
            {
                YearlyReturnPercent      = target.YearlyReturnPercent,
                MonthlyBuyAmount         = target.MonthlyBuyAmount,
                AnnualBuyIncreasePercent = target.AnnualBuyIncreasePercent,
                ProjectionYears          = target.ProjectionYears,
                InflationPercent         = target.InflationPercent,
                CgtPercent               = target.CgtPercent,
                StartAmount              = target.StartAmount,
                ApplyDeemedDisposal      = target.ApplyDeemedDisposal,
                DeemedDisposalPercent    = target.DeemedDisposalPercent,
            }
        };
    }

    public async Task<bool> DeleteVersionAsync(int userId, int versionId, CancellationToken ct = default)
    {
        var entity = await _context.ProjectionVersions
            .FirstOrDefaultAsync(pv => pv.Id == versionId && pv.UserId == userId, ct);

        if (entity == null) return false;

        _context.ProjectionVersions.Remove(entity);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
