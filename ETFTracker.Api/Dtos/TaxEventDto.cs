namespace ETFTracker.Api.Dtos;

public class TaxEventDto
{
    public int Id { get; set; }
    public int HoldingId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string? EtfName { get; set; }
    public int? BuyTransactionId { get; set; }
    public int? SellRecordId { get; set; }
    public string EventType { get; set; } = string.Empty;   // "Sell" | "DeemedDisposal"
    public string? TaxSubType { get; set; }                 // "CGT" | "ExitTax" | "DeemedDisposal"
    public DateOnly EventDate { get; set; }
    public decimal QuantityAtEvent { get; set; }
    public decimal CostBasisPerUnit { get; set; }
    public decimal PricePerUnitAtEvent { get; set; }
    public decimal TaxableGain { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRateUsed { get; set; }
    public string Status { get; set; } = string.Empty;      // "Pending" | "Paid"
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TaxSummaryDto
{
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
    public bool IsIrishInvestor { get; set; }
    public DateOnly? NextDeemedDisposalDate { get; set; }
    public List<TaxEventDto> Events { get; set; } = new();

    // Annual CGT consolidation (non-Irish + Irish CGT lots)
    public List<TaxYearSummaryDto> CgtByYear { get; set; } = new();

    // Exit Tax pots — per asset per year (Irish only)
    public List<ExitTaxPotDto> ExitTaxPots { get; set; } = new();

    // Legacy / backward compat kept for UI
    public decimal AnnualTaxFreeAllowance { get; set; }
    public List<TaxYearAllowanceSummaryDto> AllowanceByYear { get; set; } = new();
    public decimal TotalPendingAfterAllowance { get; set; }
}

public class TaxYearSummaryDto
{
    public int Year { get; set; }
    public decimal TotalProfits { get; set; }
    public decimal TotalLosses { get; set; }
    public decimal NetGain { get; set; }
    public decimal TaxFreeAllowance { get; set; }
    public decimal TaxableGain { get; set; }
    public decimal TaxDue { get; set; }
    public string Status { get; set; } = "Pending";
}

public class ExitTaxPotDto
{
    public int HoldingId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal TotalProfits { get; set; }
    public decimal TotalLosses { get; set; }
    public decimal DeemedDisposalCreditUsed { get; set; }
    public decimal NetTaxableGain { get; set; }
    public decimal TaxDue { get; set; }
    public string Status { get; set; } = "Pending";
}

public class RecalculateTaxYearResultDto
{
    public int Year { get; set; }
    public decimal CgtTaxDue { get; set; }
    public List<ExitTaxPotDto> ExitTaxPots { get; set; } = new();
    public decimal TotalTaxDue { get; set; }
}

public class MarkTaxEventPaidDto
{
    public DateTime? PaidAt { get; set; }
}

public class TaxYearAllowanceSummaryDto
{
    public int Year { get; set; }
    public decimal TotalTaxableGain { get; set; }
    public decimal TaxBeforeAllowance { get; set; }
    public decimal AllowanceApplied { get; set; }
    public decimal TaxAfterAllowance { get; set; }
}
