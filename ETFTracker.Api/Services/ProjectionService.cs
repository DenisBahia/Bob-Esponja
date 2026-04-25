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
        CgtPercent               = 33m,
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
                CgtPercent               = dbSettings.CgtPercent,
                StartAmount              = dbSettings.StartAmount,
            }
            : new ProjectionSettingsDto
            {
                YearlyReturnPercent      = DefaultSettings.YearlyReturnPercent,
                MonthlyBuyAmount         = DefaultSettings.MonthlyBuyAmount,
                AnnualBuyIncreasePercent = DefaultSettings.AnnualBuyIncreasePercent,
                ProjectionYears          = DefaultSettings.ProjectionYears,
                InflationPercent         = DefaultSettings.InflationPercent,
                CgtPercent               = DefaultSettings.CgtPercent,
                StartAmount              = null,
            };

        var dataPoints = await ComputeDataPointsAsync(userId, settings, ct);
        return new ProjectionResultDto { Settings = settings, DataPoints = dataPoints };
    }

    /// <summary>
    /// Calculates a projection for the given settings WITHOUT saving anything to the database.
    /// Used to preview a specific scenario (e.g. loading a saved version's settings).
    /// </summary>
    public async Task<ProjectionResultDto> CalculateAsync(int userId, ProjectionSettingsDto settings, CancellationToken ct = default)
    {
        var dataPoints = await ComputeDataPointsAsync(userId, settings, ct);
        return new ProjectionResultDto { Settings = settings, DataPoints = dataPoints };
    }

    /// <summary>
    /// Core calculation: builds projection data points for the given settings and current portfolio.
    /// Tax is applied once in the final year only, over the total profit (endValue − totalInvested).
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

        // ── Gross projection arrays ───────────────────────────────────────────
        var endOfYear      = new decimal[settings.ProjectionYears + 1];
        var initialBalance = new decimal[settings.ProjectionYears + 1];
        var totalBuys      = new decimal[settings.ProjectionYears + 1];
        var yearProfit     = new decimal[settings.ProjectionYears + 1];

        var partialGrowthFactor = (decimal)Math.Pow(
            (double)(1 + settings.YearlyReturnPercent / 100m),
            monthsRemaining / 12.0);

        initialBalance[0] = currentTotal;
        totalBuys[0]      = monthsRemaining * settings.MonthlyBuyAmount;
        endOfYear[0]      = (currentTotal + totalBuys[0]) * partialGrowthFactor;
        yearProfit[0]     = endOfYear[0] - currentTotal - totalBuys[0];

        for (int i = 1; i <= settings.ProjectionYears; i++)
        {
            var annualIncreaseFactor = (decimal)Math.Pow((double)(1 + settings.AnnualBuyIncreasePercent / 100m), i);
            var contributions = 12m * settings.MonthlyBuyAmount * annualIncreaseFactor;
            var growthFactor  = 1m + settings.YearlyReturnPercent / 100m;

            initialBalance[i] = endOfYear[i - 1];
            totalBuys[i]      = contributions;
            endOfYear[i]      = (endOfYear[i - 1] + contributions) * growthFactor;
            yearProfit[i]     = endOfYear[i] - endOfYear[i - 1] - contributions;
        }

        // ── Simplified universal tax engine ───────────────────────────────────
        // Tax is calculated once, at the final year only.
        // totalProfit = endValue − (startingBalance + all contributions across all years)
        decimal totalAmountInvested = currentTotal;
        for (int i = 0; i <= settings.ProjectionYears; i++)
            totalAmountInvested += totalBuys[i];

        var totalProfit = Math.Max(0m, endOfYear[settings.ProjectionYears] - totalAmountInvested);
        var taxAtEnd    = Math.Round(totalProfit * settings.CgtPercent / 100m, 2);

        // ── Build data points ─────────────────────────────────────────────────
        var dataPoints = new List<ProjectionDataPointDto>();

        for (int i = 0; i <= settings.ProjectionYears; i++)
        {
            var year            = currentYear + i;
            var inflationFactor = (decimal)Math.Pow((double)(1 + settings.InflationPercent / 100m), i);
            var isLastYear      = (i == settings.ProjectionYears);
            var taxPaid         = isLastYear ? taxAtEnd : 0m;
            var afterTax        = Math.Max(0m, endOfYear[i] - taxPaid);

            dataPoints.Add(new ProjectionDataPointDto
            {
                Year                             = year,
                InitialBalance                   = Math.Round(initialBalance[i], 2),
                TotalBuys                        = Math.Round(totalBuys[i], 2),
                YearProfit                       = Math.Round(yearProfit[i], 2),
                TotalAmount                      = Math.Round(endOfYear[i], 2),
                InflationCorrectedAmount         = Math.Round(endOfYear[i] / inflationFactor, 2),
                TaxPaid                          = taxPaid,
                AfterTaxTotalAmount              = Math.Round(afterTax, 2),
                AfterTaxInflationCorrectedAmount = Math.Round(afterTax / inflationFactor, 2),
            });
        }

        return dataPoints;
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
                CgtPercent               = dto.CgtPercent,
                StartAmount              = dto.StartAmount,
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
            existing.CgtPercent               = dto.CgtPercent;
            existing.StartAmount              = dto.StartAmount;
            existing.UpdatedAt                = utcNow;
        }

        await _context.SaveChangesAsync(ct);
        return dto;
    }

    public async Task<ProjectionVersionSummaryDto> SaveVersionAsync(
        int userId, SaveVersionRequestDto dto, CancellationToken ct = default)
    {
        var settings = dto.Settings;
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
