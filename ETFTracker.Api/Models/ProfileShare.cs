namespace ETFTracker.Api.Models;

public enum ShareStatus
{
    Pending = 0,   // Guest email not yet registered
    Active  = 1,   // Guest is registered and can access
    Revoked = 2    // Owner revoked the share
}

public class ProfileShare
{
    public int Id { get; set; }

    /// <summary>The user who owns the portfolio being shared.</summary>
    public int OwnerId { get; set; }

    /// <summary>The email address of the invited guest.</summary>
    public string GuestEmail { get; set; } = string.Empty;

    /// <summary>Resolved once the guest signs up / logs in for the first time after being invited.</summary>
    public int? GuestUserId { get; set; }

    /// <summary>When true the guest can only view – cannot add buys or change projection settings.</summary>
    public bool IsReadOnly { get; set; } = true;

    public ShareStatus Status { get; set; } = ShareStatus.Pending;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User? Owner { get; set; }
    public User? GuestUser { get; set; }
}

