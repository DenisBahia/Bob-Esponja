namespace ETFTracker.Api.Dtos;

public class PeriodMetrics
{
    public decimal GainLossEur { get; set; }
    public decimal GainLossPercent { get; set; }
    public bool PricesUnavailable { get; set; }
}

public class HoldingDto
{
    public int Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string? EtfName { get; set; }
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal TotalValue { get; set; }
    public bool PriceUnavailable { get; set; }
    public string? PriceSource { get; set; }

    // Period metrics
    public PeriodMetrics DailyMetrics { get; set; } = new();
    public PeriodMetrics WeeklyMetrics { get; set; } = new();
    public PeriodMetrics MonthlyMetrics { get; set; } = new();
    public PeriodMetrics YtdMetrics { get; set; } = new();

    // Sell / tax summary
    public decimal TotalTaxPaid { get; set; }
    public decimal TotalTaxPending { get; set; }
    public decimal TotalExitTaxPending { get; set; }
    public decimal TotalCgtPending { get; set; }
    public decimal AvailableQuantity { get; set; }
    public DateOnly? NextDeemedDisposalDate { get; set; }
    public string? AssetType { get; set; }
}

public class DashboardHeaderDto
{
    public decimal TotalHoldingsAmount { get; set; }
    public PeriodMetrics TotalVariation { get; set; } = new();
    public PeriodMetrics DailyMetrics { get; set; } = new();
    public PeriodMetrics WeeklyMetrics { get; set; } = new();
    public PeriodMetrics MonthlyMetrics { get; set; } = new();
    public PeriodMetrics YtdMetrics { get; set; } = new();
}

public class DashboardDto
{
    public DashboardHeaderDto Header { get; set; } = new();
    public List<HoldingDto> Holdings { get; set; } = new();
}
