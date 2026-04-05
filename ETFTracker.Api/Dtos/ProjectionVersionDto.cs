namespace ETFTracker.Api.Dtos;

/// <summary>Returned in the list endpoint — no data points.</summary>
public class ProjectionVersionSummaryDto
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public DateTime SavedAt { get; set; }
    public ProjectionSettingsDto Settings { get; set; } = new();
}

/// <summary>Returned in the detail endpoint — includes computed data points.</summary>
public class ProjectionVersionDetailDto
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public DateTime SavedAt { get; set; }
    public ProjectionSettingsDto Settings { get; set; } = new();
    public List<ProjectionDataPointDto> DataPoints { get; set; } = new();
}

