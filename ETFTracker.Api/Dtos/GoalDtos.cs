namespace ETFTracker.Api.Dtos;

/// <summary>A single year/target pair in the user's goal.</summary>
public class GoalDataPointDto
{
    public int Year { get; set; }
    public decimal TargetValue { get; set; }
}

/// <summary>The user's full goal as returned by the API.</summary>
public class UserGoalDto
{
    public int Id { get; set; }
    public int? SourceVersionId { get; set; }
    public DateTime SavedAt { get; set; }
    public List<GoalDataPointDto> DataPoints { get; set; } = new();
}

/// <summary>Request body for creating or replacing the user's goal.</summary>
public class UpsertGoalRequestDto
{
    public int? SourceVersionId { get; set; }
    public List<GoalDataPointDto> DataPoints { get; set; } = new();
}

