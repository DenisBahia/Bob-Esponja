namespace ETFTracker.Api.Dtos;

public class FireSettingsDto
{
    // ── Section 1: Your Situation Today ──────────────────────────────────────
    public int? CurrentAge { get; set; }
    /// <summary>Override for starting portfolio value. Null = use live portfolio value.</summary>
    public decimal? StartAmount { get; set; }

    // ── Section 2: Accumulation Phase ────────────────────────────────────────
    public decimal MonthlyInvestment { get; set; }
    public decimal AnnualInvestmentIncreasePercent { get; set; }
    public decimal AccumulationReturnPercent { get; set; }
    public decimal InflationPercent { get; set; }

    // ── Section 3: FIRE Target ────────────────────────────────────────────────
    public decimal MonthlyExpenses { get; set; }
    public decimal OtherMonthlyIncome { get; set; }
    public decimal SafeWithdrawalRate { get; set; }

    // ── Section 4: Withdrawal Phase ───────────────────────────────────────────
    public decimal WithdrawalReturnPercent { get; set; }
    public int WithdrawalYears { get; set; }

    /// <summary>
    /// Optional per-year monthly-investment overrides. Key = year index (0 = partial current year, 1..N = full years).
    /// When a key is present, that year uses the override value instead of the auto-calculated amount.
    /// Null / empty = use standard formula (MonthlyInvestment × annual increase factor).
    /// </summary>
    public Dictionary<int, decimal>? YearlyInvestmentOverrides { get; set; }
}

