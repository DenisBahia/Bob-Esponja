namespace ETFTracker.Api.Models;

/// <summary>
/// Stores the user's tax profile and investor configuration.
/// Completely separate from projection parameters — changes here affect the tax machine,
/// not projection calculations.
/// </summary>
public class UserSettings
{
    public int Id { get; set; }
    public int UserId { get; set; }

    // ── Investor profile ─────────────────────────────────────────────────────
    public bool IsIrishInvestor { get; set; }

    // ── Irish investor rates ─────────────────────────────────────────────────
    public decimal ExitTaxPercent { get; set; }
    public decimal DeemedDisposalPercent { get; set; }
    public decimal SiaAnnualPercent { get; set; }
    public bool DeemedDisposalEnabled { get; set; }

    // ── Non-Irish investor rates ─────────────────────────────────────────────
    public decimal CgtPercent { get; set; }
    public decimal TaxFreeAllowancePerYear { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User? User { get; set; }
}

