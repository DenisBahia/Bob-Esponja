namespace ETFTracker.Api.Dtos;

public class PortfolioEvolutionDataPointDto
{
    /// <summary>Date in "yyyy-MM-dd" format.</summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>Total portfolio value on this day.</summary>
    public decimal TotalValue { get; set; }

    /// <summary>True when at least one buy transaction was recorded on this date.</summary>
    public bool HasBuy { get; set; }

    /// <summary>True when at least one sell transaction was recorded on this date.</summary>
    public bool HasSell { get; set; }
}

public class PortfolioEvolutionDto
{
    public List<PortfolioEvolutionDataPointDto> DataPoints { get; set; } = new();
}
