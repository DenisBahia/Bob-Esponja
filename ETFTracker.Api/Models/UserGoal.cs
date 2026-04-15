namespace ETFTracker.Api.Models;

public class UserGoal
{
    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>Which projection version was used to seed this goal (nullable).</summary>
    public int? SourceVersionId { get; set; }

    public DateTime SavedAt { get; set; }

    /// <summary>JSON array of [{year, targetValue}] — editable per year by the user.</summary>
    public string GoalPointsJson { get; set; } = "[]";

    // Navigation
    public User? User { get; set; }
}

