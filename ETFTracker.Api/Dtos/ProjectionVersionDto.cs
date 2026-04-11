namespace ETFTracker.Api.Dtos;

/// <summary>Returned in the list endpoint — includes saved data points.</summary>
public class ProjectionVersionSummaryDto
{
    public int Id { get; set; }
    public string VersionName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime SavedAt { get; set; }
    public ProjectionSettingsDto Settings { get; set; } = new();
    /// <summary>Saved (frozen) yearly data points — never recalculated after save.</summary>
    public List<ProjectionDataPointDto> DataPoints { get; set; } = new();
}

/// <summary>Returned in the detail endpoint — includes computed data points.</summary>
public class ProjectionVersionDetailDto
{
    public int Id { get; set; }
    public string VersionName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime SavedAt { get; set; }
    public ProjectionSettingsDto Settings { get; set; } = new();
    public List<ProjectionDataPointDto> DataPoints { get; set; } = new();
}

/// <summary>Request body for saving a new version.</summary>
public class SaveVersionRequestDto
{
    public string VersionName { get; set; } = string.Empty;
    public ProjectionSettingsDto Settings { get; set; } = new();
}

