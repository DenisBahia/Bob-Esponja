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
        YearlyReturnPercent = 7m,
        MonthlyBuyAmount = 500m,
        AnnualBuyIncreasePercent = 3m,
        ProjectionYears = 10,
        InflationPercent = 2m,
        CgtPercent = 33m,
        ExitTaxPercent = 41m,
        ExcludePreExistingFromTax = false,
        SiaAnnualPercent = 0m,
        IsIrishInvestor = true,
        DeemedDisposalPercent = 41m,
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

        // Load settings (or use defaults)
        var dbSettings = await _context.ProjectionSettings
            .FirstOrDefaultAsync(ps => ps.UserId == userId, ct);

        var settings = dbSettings != null
            ? new ProjectionSettingsDto
            {
                YearlyReturnPercent = dbSettings.YearlyReturnPercent,
                MonthlyBuyAmount = dbSettings.MonthlyBuyAmount,
                AnnualBuyIncreasePercent = dbSettings.AnnualBuyIncreasePercent,
                ProjectionYears = dbSettings.ProjectionYears,
                InflationPercent = dbSettings.InflationPercent,
                CgtPercent = dbSettings.CgtPercent,
                ExitTaxPercent = dbSettings.ExitTaxPercent,
                ExcludePreExistingFromTax = dbSettings.ExcludePreExistingFromTax,
                SiaAnnualPercent = dbSettings.SiaAnnualPercent,
                StartAmount = dbSettings.StartAmount,
                IsIrishInvestor = dbSettings.IsIrishInvestor,
                TaxFreeAllowancePerYear = dbSettings.TaxFreeAllowancePerYear,
                DeemedDisposalPercent = dbSettings.DeemedDisposalPercent,
                DeemedDisposalEnabled = dbSettings.DeemedDisposalEnabled,
            }
            : new ProjectionSettingsDto
            {
                YearlyReturnPercent = DefaultSettings.YearlyReturnPercent,
                MonthlyBuyAmount = DefaultSettings.MonthlyBuyAmount,
                AnnualBuyIncreasePercent = DefaultSettings.AnnualBuyIncreasePercent,
                ProjectionYears = DefaultSettings.ProjectionYears,
                InflationPercent = DefaultSettings.InflationPercent,
                CgtPercent = DefaultSettings.CgtPercent,
                ExitTaxPercent = DefaultSettings.ExitTaxPercent,
                ExcludePreExistingFromTax = DefaultSettings.ExcludePreExistingFromTax,
                SiaAnnualPercent = DefaultSettings.SiaAnnualPercent,
                DeemedDisposalPercent = DefaultSettings.DeemedDisposalPercent,
                StartAmount = null,
            };

        var dataPoints = await ComputeDataPointsAsync(userId, settings, ct);

        return new ProjectionResultDto
        {
            Settings = settings,
            DataPoints = dataPoints,
        };
    }

    /// <summary>
    /// Calculates a projection for the given settings WITHOUT saving anything to the database.
    /// Used to preview a specific scenario (e.g. loading a saved version's settings).
    /// </summary>
    public async Task<ProjectionResultDto> CalculateAsync(int userId, ProjectionSettingsDto settings, CancellationToken ct = default)
    {
        var dataPoints = await ComputeDataPointsAsync(userId, settings, ct);
        return new ProjectionResultDto
        {
            Settings = settings,
            DataPoints = dataPoints,
        };
    }

    /// <summary>
    /// Core calculation: builds projection data points for the given settings and current portfolio.
    /// Extracted so it can be reused by both GetProjectionAsync and SaveVersionAsync.
    /// </summary>
    private async Task<List<ProjectionDataPointDto>> ComputeDataPointsAsync(
        int userId, ProjectionSettingsDto settings, CancellationToken ct)
    {
        // Calculate current total portfolio value & cache prices by ticker
        var holdings = await _context.Holdings
            .Where(h => h.UserId == userId)
            .ToListAsync(ct);

        var priceByTicker = new Dictionary<string, decimal>();
        decimal currentTotal = 0m;
        foreach (var holding in holdings)
        {
            var price = await _priceService.GetPriceAsync(holding.Ticker, ct);
            priceByTicker[holding.Ticker] = price ?? 0m;
            currentTotal += holding.Quantity * (price ?? 0m);
        }

        // Allow the user to override the starting portfolio value
        if (settings.StartAmount.HasValue && settings.StartAmount.Value > 0m)
            currentTotal = settings.StartAmount.Value;

        // Determine months remaining in the current year
        var today = DateTime.UtcNow;
        var currentYear = today.Year;
        var currentMonth = today.Month;

        var hasBuyThisMonth = await _context.Transactions
            .AnyAsync(t => t.Holding!.UserId == userId
                        && t.PurchaseDate.Year == currentYear
                        && t.PurchaseDate.Month == currentMonth, ct);

        var monthsRemaining = hasBuyThisMonth
            ? 12 - currentMonth
            : 12 - currentMonth + 1;

        // ── Gross projection arrays ───────────────────────────────────────────
        var endOfYear = new decimal[settings.ProjectionYears + 1];
        var initialBalance = new decimal[settings.ProjectionYears + 1];
        var totalBuys = new decimal[settings.ProjectionYears + 1];
        var yearProfit = new decimal[settings.ProjectionYears + 1];

        var partialGrowthFactor = (decimal)Math.Pow(
            (double)(1 + settings.YearlyReturnPercent / 100m),
            monthsRemaining / 12.0);

        initialBalance[0] = currentTotal;
        totalBuys[0] = monthsRemaining * settings.MonthlyBuyAmount;
        endOfYear[0] = (currentTotal + totalBuys[0]) * partialGrowthFactor;
        yearProfit[0] = endOfYear[0] - currentTotal - totalBuys[0];

        for (int i = 1; i <= settings.ProjectionYears; i++)
        {
            var annualIncreaseFactor = (decimal)Math.Pow((double)(1 + settings.AnnualBuyIncreasePercent / 100m), i);
            var monthlyBuyThisYear = settings.MonthlyBuyAmount * annualIncreaseFactor;
            var contributions = 12m * monthlyBuyThisYear;
            var growthFactor = 1m + settings.YearlyReturnPercent / 100m;

            initialBalance[i] = endOfYear[i - 1];
            totalBuys[i] = contributions;
            endOfYear[i] = (endOfYear[i - 1] + contributions) * growthFactor;
            yearProfit[i] = endOfYear[i] - endOfYear[i - 1] - contributions;
        }

        // ── Tax engine ────────────────────────────────────────────────────────
        int projectionEndYear = currentYear + settings.ProjectionYears;
        var growthRate = settings.YearlyReturnPercent / 100m;

        var allTransactions = await _context.Transactions
            .Include(t => t.Holding)
            .Where(t => t.Holding!.UserId == userId)
            .ToListAsync(ct);

        var buyLots = new List<(int BuyYear, decimal CostBasis, bool IsReal, decimal CurrentValue, bool DeemedDisposalDue)>();
        var taxCutoffDate = new DateOnly(2026, 1, 1);

        foreach (var txn in allTransactions)
        {
            if (settings.ExcludePreExistingFromTax && txn.PurchaseDate < taxCutoffDate)
                continue;

            var ticker = txn.Holding!.Ticker;
            var price = priceByTicker.GetValueOrDefault(ticker, 0m);
            buyLots.Add((
                BuyYear: txn.PurchaseDate.Year,
                CostBasis: txn.Quantity * txn.PurchasePrice,
                IsReal: true,
                CurrentValue: txn.Quantity * price,
                DeemedDisposalDue: txn.DeemedDisposalDue
            ));
        }

        if (monthsRemaining > 0 && settings.MonthlyBuyAmount > 0)
        {
            // Future projected buys: use DeemedDisposalEnabled to decide
            buyLots.Add((
                BuyYear: currentYear,
                CostBasis: monthsRemaining * settings.MonthlyBuyAmount,
                IsReal: false,
                CurrentValue: 0m,
                DeemedDisposalDue: settings.DeemedDisposalEnabled
            ));
        }

        for (int i = 1; i <= settings.ProjectionYears; i++)
        {
            var annualIncreaseFactor = (decimal)Math.Pow(
                (double)(1 + settings.AnnualBuyIncreasePercent / 100m), i);
            var contributions = 12m * settings.MonthlyBuyAmount * annualIncreaseFactor;
            if (contributions > 0)
            {
                buyLots.Add((
                    BuyYear: currentYear + i,
                    CostBasis: contributions,
                    IsReal: false,
                    CurrentValue: 0m,
                    DeemedDisposalDue: settings.DeemedDisposalEnabled
                ));
            }
        }

        var deemedDisposalByYear = new Dictionary<int, decimal>();
        decimal exitTaxTotal = 0m;

        foreach (var lot in buyLots)
        {
            decimal cumulativeDdPaid = 0m;

            decimal ProjectedValue(int year)
            {
                if (lot.IsReal)
                    return lot.CurrentValue * (decimal)Math.Pow((double)(1 + growthRate), year - currentYear);
                else
                    return lot.CostBasis * (decimal)Math.Pow((double)(1 + growthRate), year - lot.BuyYear);
            }

            // Deemed disposal loop: only if enabled AND the lot has DeemedDisposalDue
            if (settings.DeemedDisposalEnabled && lot.DeemedDisposalDue)
            {
                for (int ddYear = lot.BuyYear + 8; ddYear < projectionEndYear; ddYear += 8)
                {
                    var profit = ProjectedValue(ddYear) - lot.CostBasis;
                    if (profit > 0)
                    {
                        var ddAmount = Math.Max(0m, Math.Round(profit * settings.DeemedDisposalPercent / 100m - cumulativeDdPaid, 2));
                        if (ddAmount > 0)
                        {
                            cumulativeDdPaid += ddAmount;
                            if (ddYear >= currentYear)
                            {
                                deemedDisposalByYear.TryGetValue(ddYear, out var existing);
                                deemedDisposalByYear[ddYear] = existing + ddAmount;
                            }
                        }
                    }
                }
            }

            // Final-year sell: ExitTax for DeemedDisposalDue lots, CGT for others
            if (projectionEndYear >= lot.BuyYear)
            {
                var profit = ProjectedValue(projectionEndYear) - lot.CostBasis;
                if (profit > 0)
                {
                    var sellTaxRate = lot.DeemedDisposalDue ? settings.ExitTaxPercent : settings.CgtPercent;
                    var sellTax = Math.Max(0m, Math.Round(profit * sellTaxRate / 100m - cumulativeDdPaid, 2));
                    exitTaxTotal += sellTax;
                }
            }
        }

        // ── Build data points ─────────────────────────────────────────────────
        var dataPoints = new List<ProjectionDataPointDto>();
        var cumulativeTax = 0m;
        var cumulativeSia = 0m;

        // Annual tax-free allowance: only for non-Irish investors and when > 0
        var annualAllowance = (!settings.IsIrishInvestor && settings.TaxFreeAllowancePerYear > 0)
            ? settings.TaxFreeAllowancePerYear
            : 0m;

        for (int i = 0; i <= settings.ProjectionYears; i++)
        {
            var year = currentYear + i;
            var inflationFactor = (decimal)Math.Pow((double)(1 + settings.InflationPercent / 100m), i);

            var cgtRaw      = deemedDisposalByYear.TryGetValue(year, out var cgt) ? cgt : 0m;
            var exitTaxRaw  = (year == projectionEndYear) ? exitTaxTotal : 0m;

            // Apply allowance: deduct from total tax due this year, floored at 0
            var taxDueThisYear   = cgtRaw + exitTaxRaw;
            var allowanceUsed    = Math.Min(annualAllowance, taxDueThisYear);
            var taxAfterAllowance = Math.Max(0m, taxDueThisYear - allowanceUsed);

            // Distribute the reduction proportionally between CGT and exit-tax for display
            decimal cgtPaid, exitTaxPaid;
            if (taxDueThisYear > 0)
            {
                cgtPaid     = Math.Round(cgtRaw     * taxAfterAllowance / taxDueThisYear, 2);
                exitTaxPaid = Math.Round(exitTaxRaw * taxAfterAllowance / taxDueThisYear, 2);
            }
            else
            {
                cgtPaid     = 0m;
                exitTaxPaid = 0m;
            }

            cumulativeTax += cgtPaid + exitTaxPaid;

            // SIA: annual tax on the entire end-of-year portfolio balance (allowance does not apply to SIA)
            var siaTax = Math.Round(endOfYear[i] * settings.SiaAnnualPercent / 100m, 2);
            cumulativeSia += siaTax;

            dataPoints.Add(new ProjectionDataPointDto
            {
                Year = year,
                InitialBalance = Math.Round(initialBalance[i], 2),
                TotalBuys = Math.Round(totalBuys[i], 2),
                YearProfit = Math.Round(yearProfit[i], 2),
                TotalAmount = Math.Round(endOfYear[i], 2),
                InflationCorrectedAmount = Math.Round(endOfYear[i] / inflationFactor, 2),
                TaxPaid = cgtPaid,
                ExitTaxPaid = exitTaxPaid,
                AfterTaxTotalAmount = Math.Round(Math.Max(0m, endOfYear[i] - cumulativeTax), 2),
                AfterTaxInflationCorrectedAmount = Math.Round(Math.Max(0m, (endOfYear[i] - cumulativeTax) / inflationFactor), 2),
                SiaTax = siaTax,
                AfterTaxSia = Math.Round(Math.Max(0m, endOfYear[i] - cumulativeSia), 2),
                AfterTaxInflationCorrectedSia = Math.Round(Math.Max(0m, (endOfYear[i] - cumulativeSia) / inflationFactor), 2),
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
                UserId = userId,
                YearlyReturnPercent = dto.YearlyReturnPercent,
                MonthlyBuyAmount = dto.MonthlyBuyAmount,
                AnnualBuyIncreasePercent = dto.AnnualBuyIncreasePercent,
                ProjectionYears = dto.ProjectionYears,
                InflationPercent = dto.InflationPercent,
                CgtPercent = dto.CgtPercent,
                ExitTaxPercent = dto.ExitTaxPercent,
                ExcludePreExistingFromTax = dto.ExcludePreExistingFromTax,
                SiaAnnualPercent = dto.SiaAnnualPercent,
                StartAmount = (dto.StartAmount.HasValue && dto.StartAmount.Value > 0m) ? dto.StartAmount : null,
                IsIrishInvestor = dto.IsIrishInvestor,
                TaxFreeAllowancePerYear = dto.TaxFreeAllowancePerYear,
                DeemedDisposalPercent = dto.DeemedDisposalPercent,
                DeemedDisposalEnabled = dto.DeemedDisposalEnabled,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            };
            _context.ProjectionSettings.Add(newSettings);
        }
        else
        {
            existing.YearlyReturnPercent = dto.YearlyReturnPercent;
            existing.MonthlyBuyAmount = dto.MonthlyBuyAmount;
            existing.AnnualBuyIncreasePercent = dto.AnnualBuyIncreasePercent;
            existing.ProjectionYears = dto.ProjectionYears;
            existing.InflationPercent = dto.InflationPercent;
            existing.CgtPercent = dto.CgtPercent;
            existing.ExitTaxPercent = dto.ExitTaxPercent;
            existing.ExcludePreExistingFromTax = dto.ExcludePreExistingFromTax;
            existing.SiaAnnualPercent = dto.SiaAnnualPercent;
            existing.StartAmount = (dto.StartAmount.HasValue && dto.StartAmount.Value > 0m) ? dto.StartAmount : null;
            existing.IsIrishInvestor = dto.IsIrishInvestor;
            existing.TaxFreeAllowancePerYear = dto.TaxFreeAllowancePerYear;
            existing.DeemedDisposalPercent = dto.DeemedDisposalPercent;
            existing.DeemedDisposalEnabled = dto.DeemedDisposalEnabled;
            existing.UpdatedAt = utcNow;
        }

        await _context.SaveChangesAsync(ct);
        return dto;
    }

    public async Task<ProjectionVersionSummaryDto> SaveVersionAsync(
        int userId, SaveVersionRequestDto dto, CancellationToken ct = default)
    {
        var settings = dto.Settings;

        // Compute data points using current portfolio state
        var dataPoints = await ComputeDataPointsAsync(userId, settings, ct);

        // Serialise data points to JSON
        var json = System.Text.Json.JsonSerializer.Serialize(dataPoints);

        var entity = new ProjectionVersion
        {
            UserId = userId,
            VersionName = string.IsNullOrWhiteSpace(dto.VersionName) ? "Unnamed" : dto.VersionName.Trim(),
            IsDefault = false,
            SavedAt = DateTime.UtcNow,
            YearlyReturnPercent = settings.YearlyReturnPercent,
            MonthlyBuyAmount = settings.MonthlyBuyAmount,
            AnnualBuyIncreasePercent = settings.AnnualBuyIncreasePercent,
            ProjectionYears = settings.ProjectionYears,
            InflationPercent = settings.InflationPercent,
            CgtPercent = settings.CgtPercent,
            ExitTaxPercent = settings.ExitTaxPercent,
            ExcludePreExistingFromTax = settings.ExcludePreExistingFromTax,
            SiaAnnualPercent = settings.SiaAnnualPercent,
            IsIrishInvestor = settings.IsIrishInvestor,
            TaxFreeAllowancePerYear = settings.TaxFreeAllowancePerYear,
            DeemedDisposalPercent = settings.DeemedDisposalPercent,
            DataPointsJson = json,
        };
        _context.ProjectionVersions.Add(entity);
        await _context.SaveChangesAsync(ct);

        return new ProjectionVersionSummaryDto
        {
            Id = entity.Id,
            VersionName = entity.VersionName,
            IsDefault = entity.IsDefault,
            SavedAt = entity.SavedAt,
            Settings = settings,
            DataPoints = dataPoints,
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
            Id = pv.Id,
            VersionName = pv.VersionName,
            IsDefault = pv.IsDefault,
            SavedAt = pv.SavedAt,
            Settings = new ProjectionSettingsDto
            {
                YearlyReturnPercent = pv.YearlyReturnPercent,
                MonthlyBuyAmount = pv.MonthlyBuyAmount,
                AnnualBuyIncreasePercent = pv.AnnualBuyIncreasePercent,
                ProjectionYears = pv.ProjectionYears,
                InflationPercent = pv.InflationPercent,
                CgtPercent = pv.CgtPercent,
                ExitTaxPercent = pv.ExitTaxPercent,
                ExcludePreExistingFromTax = pv.ExcludePreExistingFromTax,
                SiaAnnualPercent = pv.SiaAnnualPercent,
                IsIrishInvestor = pv.IsIrishInvestor,
                TaxFreeAllowancePerYear = pv.TaxFreeAllowancePerYear,
                DeemedDisposalPercent = pv.DeemedDisposalPercent,
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
            Id = entity.Id,
            VersionName = entity.VersionName,
            IsDefault = entity.IsDefault,
            SavedAt = entity.SavedAt,
            Settings = new ProjectionSettingsDto
            {
                YearlyReturnPercent = entity.YearlyReturnPercent,
                MonthlyBuyAmount = entity.MonthlyBuyAmount,
                AnnualBuyIncreasePercent = entity.AnnualBuyIncreasePercent,
                ProjectionYears = entity.ProjectionYears,
                InflationPercent = entity.InflationPercent,
                CgtPercent = entity.CgtPercent,
                ExitTaxPercent = entity.ExitTaxPercent,
                ExcludePreExistingFromTax = entity.ExcludePreExistingFromTax,
                SiaAnnualPercent = entity.SiaAnnualPercent,
                IsIrishInvestor = entity.IsIrishInvestor,
                TaxFreeAllowancePerYear = entity.TaxFreeAllowancePerYear,
                DeemedDisposalPercent = entity.DeemedDisposalPercent,
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

        // Clear existing default for this user
        var currentDefaults = await _context.ProjectionVersions
            .Where(pv => pv.UserId == userId && pv.IsDefault)
            .ToListAsync(ct);
        foreach (var v in currentDefaults)
            v.IsDefault = false;

        target.IsDefault = true;
        await _context.SaveChangesAsync(ct);

        return new ProjectionVersionSummaryDto
        {
            Id = target.Id,
            VersionName = target.VersionName,
            IsDefault = target.IsDefault,
            SavedAt = target.SavedAt,
            Settings = new ProjectionSettingsDto
            {
                YearlyReturnPercent = target.YearlyReturnPercent,
                MonthlyBuyAmount = target.MonthlyBuyAmount,
                AnnualBuyIncreasePercent = target.AnnualBuyIncreasePercent,
                ProjectionYears = target.ProjectionYears,
                InflationPercent = target.InflationPercent,
                CgtPercent = target.CgtPercent,
                ExitTaxPercent = target.ExitTaxPercent,
                ExcludePreExistingFromTax = target.ExcludePreExistingFromTax,
                SiaAnnualPercent = target.SiaAnnualPercent,
                IsIrishInvestor = target.IsIrishInvestor,
                TaxFreeAllowancePerYear = target.TaxFreeAllowancePerYear,
                DeemedDisposalPercent = target.DeemedDisposalPercent,
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
