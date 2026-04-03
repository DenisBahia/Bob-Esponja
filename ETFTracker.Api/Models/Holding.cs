using System.ComponentModel.DataAnnotations;

namespace ETFTracker.Api.Models;

public class Holding
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string? EtfName { get; set; }
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public string? Broker { get; set; }
    [MaxLength(50)]
    public string? PriceSource { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set;}

    // Navigation
    public User? User { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

