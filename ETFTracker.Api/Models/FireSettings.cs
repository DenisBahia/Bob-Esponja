namespace ETFTracker.Api.Models;

/// <summary>Stores the user's FIRE (Financial Independence, Retire Early) calculator settings.</summary>
public class FireSettings
{
    public int Id { get; set; }
    public int UserId { get; set; }

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

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User? User { get; set; }
}

