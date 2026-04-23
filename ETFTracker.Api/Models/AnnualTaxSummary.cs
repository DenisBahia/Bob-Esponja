namespace ETFTracker.Api.Models;

public class AnnualTaxSummary
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TaxYear { get; set; }
    /// <summary>"CGT" | "ExitTax"</summary>
    public string TaxType { get; set; } = string.Empty;
    /// <summary>null for CGT (all assets combined); set for ExitTax (per asset pot).</summary>
    public int? HoldingId { get; set; }
    public decimal TotalProfits { get; set; }
    public decimal TotalLosses { get; set; }
    public decimal NetGain { get; set; }
    /// <summary>CGT only: annual tax-free allowance applied.</summary>
    public decimal AllowanceApplied { get; set; }
    /// <summary>ExitTax only: deemed disposal tax already paid (reduces taxable gain).</summary>
    public decimal DeemedDisposalCredit { get; set; }
    public decimal TaxableGain { get; set; }
    public decimal TaxDue { get; set; }
    public decimal TaxRateUsed { get; set; }
    /// <summary>"Pending" | "Paid"</summary>
    public string Status { get; set; } = "Pending";
    /// <summary>Snapshot of TaxDue at the time the user last clicked Mark Paid.
    /// Used to compute deltas when TaxDue changes after a recalculation.</summary>
    public decimal PaidTaxAmount { get; set; }
    public DateTime RecalculatedAt { get; set; }

    // Navigation
    public User? User { get; set; }
    public Holding? Holding { get; set; }
}
