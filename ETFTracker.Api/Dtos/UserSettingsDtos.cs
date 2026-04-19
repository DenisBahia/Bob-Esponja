namespace ETFTracker.Api.Dtos;

/// <summary>
/// User tax default settings — the single source of truth for all tax rates
/// used across projections, sell calculations, and deemed-disposal events.
/// </summary>
public class UserTaxDefaultsDto
{
    /// <summary>True = Irish investor (Exit Tax / Deemed Disposal regime). Hides CGT and tax-free allowance fields.</summary>
    public bool IsIrishInvestor { get; set; }

    // ── Irish investor fields ─────────────────────────────────────────────────
    /// <summary>Exit Tax % applied when selling (Irish investors only). User can override at point-of-sale.</summary>
    public decimal ExitTaxPercent { get; set; }
    /// <summary>Deemed Disposal % applied to 8-year anniversary events (Irish investors only). CANNOT be overridden at point-of-use.</summary>
    public decimal DeemedDisposalPercent { get; set; }
    /// <summary>SIA annual tax % charged on total portfolio value (Irish investors only).</summary>
    public decimal SiaAnnualPercent { get; set; }

    // ── Non-Irish investor fields ─────────────────────────────────────────────
    /// <summary>CGT % applied on profits when selling (non-Irish investors only). User can override at point-of-sale.</summary>
    public decimal CgtPercent { get; set; }
    /// <summary>Annual tax-free CGT allowance amount (non-Irish investors only). 0 = disabled.</summary>
    public decimal TaxFreeAllowancePerYear { get; set; }
}

