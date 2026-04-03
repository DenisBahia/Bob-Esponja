namespace ETFTracker.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ICollection<Holding> Holdings { get; set; } = new List<Holding>();
    public ProjectionSettings? ProjectionSettings { get; set; }
}

