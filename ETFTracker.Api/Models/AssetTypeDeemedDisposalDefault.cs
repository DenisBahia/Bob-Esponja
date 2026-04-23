namespace ETFTracker.Api.Models;

public class AssetTypeDeemedDisposalDefault
{
    public int Id { get; set; }
    public int UserId { get; set; }
    /// <summary>e.g. "ETF", "EQUITY", "MUTUALFUND", "CRYPTO"</summary>
    public string AssetType { get; set; } = string.Empty;
    public bool DeemedDisposalDue { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User? User { get; set; }
}

