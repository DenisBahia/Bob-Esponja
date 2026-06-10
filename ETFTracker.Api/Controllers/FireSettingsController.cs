using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ETFTracker.Api.Data;
using ETFTracker.Api.Dtos;
using ETFTracker.Api.Models;
using ETFTracker.Api.Services;
using System.Text.Json;

namespace ETFTracker.Api.Controllers;

/// <summary>
/// FIRE (Financial Independence, Retire Early) calculator settings.
/// </summary>
[Authorize]
[ApiController]
[Route("api/fire-settings")]
public class FireSettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISharingContextService _sharingContext;
    private readonly ILogger<FireSettingsController> _logger;

    public FireSettingsController(AppDbContext db, ISharingContextService sharingContext,
        ILogger<FireSettingsController> logger)
    {
        _db = db;
        _sharingContext = sharingContext;
        _logger = logger;
    }

    private int UserId => _sharingContext.GetEffectiveUserId();

    // ── Helpers for per-year investment overrides ────────────────────────────────
    private static Dictionary<int, decimal>? DeserializeOverrides(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<Dictionary<int, decimal>>(json);
        }
        catch
        {
            return null;
        }
    }

    private static string? SerializeOverrides(Dictionary<int, decimal>? overrides)
    {
        if (overrides == null || overrides.Count == 0) return null;
        return JsonSerializer.Serialize(overrides);
    }

    /// <summary>Returns the user's saved FIRE settings, or sensible defaults if not yet saved.</summary>
    [HttpGet]
    public async Task<ActionResult<FireSettingsDto>> GetFireSettings(CancellationToken ct = default)
    {
        try
        {
            var settings = await _db.FireSettings
                .FirstOrDefaultAsync(fs => fs.UserId == UserId, ct);

            if (settings == null)
                return Ok(new FireSettingsDto
                {
                    CurrentAge = null,
                    StartAmount = null,
                    MonthlyInvestment = 500m,
                    AnnualInvestmentIncreasePercent = 3m,
                    AccumulationReturnPercent = 7m,
                    InflationPercent = 2m,
                    MonthlyExpenses = 0m,
                    OtherMonthlyIncome = 0m,
                    SafeWithdrawalRate = 4m,
                    WithdrawalReturnPercent = 7m,
                    WithdrawalYears = 30,
                });

            return Ok(MapToDto(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving FIRE settings for user {UserId}", UserId);
            return StatusCode(500, new { message = "Error retrieving FIRE settings" });
        }
    }

    /// <summary>Saves the user's FIRE settings.</summary>
    [HttpPut]
    public async Task<ActionResult<FireSettingsDto>> SaveFireSettings(
        [FromBody] FireSettingsDto dto, CancellationToken ct = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "Read-only profile." });

            var settings = await _db.FireSettings
                .FirstOrDefaultAsync(fs => fs.UserId == UserId, ct);

            if (settings == null)
            {
                settings = new FireSettings
                {
                    UserId = UserId,
                    CreatedAt = DateTime.UtcNow,
                };
                _db.FireSettings.Add(settings);
            }

            settings.CurrentAge = dto.CurrentAge;
            settings.StartAmount = dto.StartAmount;
            settings.MonthlyInvestment = dto.MonthlyInvestment;
            settings.AnnualInvestmentIncreasePercent = dto.AnnualInvestmentIncreasePercent;
            settings.AccumulationReturnPercent = dto.AccumulationReturnPercent;
            settings.InflationPercent = dto.InflationPercent;
            settings.MonthlyExpenses = dto.MonthlyExpenses;
            settings.OtherMonthlyIncome = dto.OtherMonthlyIncome;
            settings.SafeWithdrawalRate = dto.SafeWithdrawalRate;
            settings.WithdrawalReturnPercent = dto.WithdrawalReturnPercent;
            settings.WithdrawalYears = dto.WithdrawalYears;
            settings.YearlyInvestmentOverridesJson = SerializeOverrides(dto.YearlyInvestmentOverrides);
            settings.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Ok(MapToDto(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving FIRE settings for user {UserId}", UserId);
            return StatusCode(500, new { message = "Error saving FIRE settings" });
        }
    }

    private static FireSettingsDto MapToDto(FireSettings s) => new()
    {
        CurrentAge = s.CurrentAge,
        StartAmount = s.StartAmount,
        MonthlyInvestment = s.MonthlyInvestment,
        AnnualInvestmentIncreasePercent = s.AnnualInvestmentIncreasePercent,
        AccumulationReturnPercent = s.AccumulationReturnPercent,
        InflationPercent = s.InflationPercent,
        MonthlyExpenses = s.MonthlyExpenses,
        OtherMonthlyIncome = s.OtherMonthlyIncome,
        SafeWithdrawalRate = s.SafeWithdrawalRate,
        WithdrawalReturnPercent = s.WithdrawalReturnPercent,
        WithdrawalYears = s.WithdrawalYears,
        YearlyInvestmentOverrides = DeserializeOverrides(s.YearlyInvestmentOverridesJson),
    };
}

