namespace ETFTracker.Api.Models;

public class User
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }

    // Email/password authentication
    public string? PasswordHash { get; set; }

    // OAuth provider identifiers
    public string? GitHubId { get; set; }
    public string? GitHubUsername { get; set; }
    public string? GoogleId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ICollection<Holding> Holdings { get; set; } = new List<Holding>();
    public ProjectionSettings? ProjectionSettings { get; set; }

    // Sharing
    public ICollection<ProfileShare> SharedByMe { get; set; } = new List<ProfileShare>();
    public ICollection<ProfileShare> SharedWithMe { get; set; } = new List<ProfileShare>();
}
