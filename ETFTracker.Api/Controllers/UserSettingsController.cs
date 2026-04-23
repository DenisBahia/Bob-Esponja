using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ETFTracker.Api.Data;
using ETFTracker.Api.Dtos;
using ETFTracker.Api.Models;
using ETFTracker.Api.Services;

namespace ETFTracker.Api.Controllers;

/// <summary>
/// User-level settings: investor profile and default tax rates.
/// </summary>
[Authorize]
[ApiController]
[Route("api/user-settings")]
public class UserSettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISharingContextService _sharingContext;
    private readonly ILogger<UserSettingsController> _logger;

    public UserSettingsController(AppDbContext db, ISharingContextService sharingContext,
        ILogger<UserSettingsController> logger)
    {
        _db = db;
        _sharingContext = sharingContext;
        _logger = logger;
    }

    private int UserId => _sharingContext.GetEffectiveUserId();

    /// <summary>Returns the user's current tax defaults.</summary>
    [HttpGet("tax-defaults")]
    public async Task<ActionResult<UserTaxDefaultsDto>> GetTaxDefaults(CancellationToken ct = default)
    {
        try
        {
            var settings = await _db.ProjectionSettings
                .FirstOrDefaultAsync(ps => ps.UserId == UserId, ct);

            if (settings == null)
                return Ok(new UserTaxDefaultsDto
                {
                    IsIrishInvestor = true,
                    ExitTaxPercent = 38m,
                    DeemedDisposalPercent = 38m,
                    SiaAnnualPercent = 0m,
                    CgtPercent = 38m,
                    TaxFreeAllowancePerYear = 1270m,
                });

            return Ok(MapToDto(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax defaults for user {UserId}", UserId);
            return StatusCode(500, new { message = "Error retrieving tax defaults" });
        }
    }

    /// <summary>Saves the user's tax defaults. Updates ProjectionSettings in place (upsert).</summary>
    [HttpPut("tax-defaults")]
    public async Task<ActionResult<UserTaxDefaultsDto>> SaveTaxDefaults(
        [FromBody] UserTaxDefaultsDto dto, CancellationToken ct = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "Read-only profile." });

            var settings = await _db.ProjectionSettings
                .FirstOrDefaultAsync(ps => ps.UserId == UserId, ct);

            if (settings == null)
            {
                settings = new ProjectionSettings
                {
                    UserId = UserId,
                    YearlyReturnPercent = 7m,
                    MonthlyBuyAmount = 500m,
                    AnnualBuyIncreasePercent = 3m,
                    ProjectionYears = 10,
                    InflationPercent = 2m,
                    CreatedAt = DateTime.UtcNow,
                };
                _db.ProjectionSettings.Add(settings);
            }

            // Apply tax-defaults fields
            settings.IsIrishInvestor = dto.IsIrishInvestor;
            settings.ExitTaxPercent = dto.ExitTaxPercent;
            settings.DeemedDisposalPercent = dto.DeemedDisposalPercent;
            settings.SiaAnnualPercent = dto.SiaAnnualPercent;
            settings.CgtPercent = dto.CgtPercent;
            settings.TaxFreeAllowancePerYear = dto.TaxFreeAllowancePerYear;
            settings.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Ok(MapToDto(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tax defaults for user {UserId}", UserId);
            return StatusCode(500, new { message = "Error saving tax defaults" });
        }
    }

    private static UserTaxDefaultsDto MapToDto(ProjectionSettings s) => new()
    {
        IsIrishInvestor = s.IsIrishInvestor,
        ExitTaxPercent = s.ExitTaxPercent,
        DeemedDisposalPercent = s.DeemedDisposalPercent,
        SiaAnnualPercent = s.SiaAnnualPercent,
        CgtPercent = s.CgtPercent,
        TaxFreeAllowancePerYear = s.TaxFreeAllowancePerYear,
    };
}

