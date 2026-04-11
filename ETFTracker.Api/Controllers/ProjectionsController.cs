using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ETFTracker.Api.Dtos;
using ETFTracker.Api.Services;

namespace ETFTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProjectionsController : ControllerBase
{
    private readonly IProjectionService _projectionService;
    private readonly ISharingContextService _sharingContext;
    private readonly ILogger<ProjectionsController> _logger;

    public ProjectionsController(
        IProjectionService projectionService,
        ISharingContextService sharingContext,
        ILogger<ProjectionsController> logger)
    {
        _projectionService = projectionService;
        _sharingContext    = sharingContext;
        _logger            = logger;
    }

    private int GetUserId() => _sharingContext.GetEffectiveUserId();

    /// <summary>
    /// Returns current projection settings + calculated data points.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ProjectionResultDto>> GetProjection(CancellationToken ct = default)
    {
        try
        {
            var result = await _projectionService.GetProjectionAsync(GetUserId(), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating projection");
            return StatusCode(500, new { message = "Error calculating projection" });
        }
    }

    /// <summary>
    /// Saves projection settings and returns the saved values.
    /// </summary>
    [HttpPut("settings")]
    public async Task<ActionResult<ProjectionSettingsDto>> SaveSettings(
        [FromBody] ProjectionSettingsDto dto,
        CancellationToken ct = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "This profile is shared as read-only." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var saved = await _projectionService.SaveSettingsAsync(GetUserId(), dto, ct);
            return Ok(saved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving projection settings");
            return StatusCode(500, new { message = "Error saving projection settings" });
        }
    }

    /// <summary>
    /// Saves a new named version with the supplied settings + current computed data points.
    /// </summary>
    [HttpPost("versions")]
    public async Task<ActionResult<ProjectionVersionSummaryDto>> SaveVersion(
        [FromBody] SaveVersionRequestDto dto,
        CancellationToken ct = default)
    {
        if (_sharingContext.IsReadOnly())
            return StatusCode(403, new { message = "This profile is shared as read-only." });
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        try
        {
            var result = await _projectionService.SaveVersionAsync(GetUserId(), dto, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving projection version");
            return StatusCode(500, new { message = "Error saving projection version" });
        }
    }

    /// <summary>
    /// Lists all saved versions for the authenticated user (no data points).
    /// </summary>
    [HttpGet("versions")]
    public async Task<ActionResult<List<ProjectionVersionSummaryDto>>> GetVersions(CancellationToken ct = default)
    {
        try
        {
            var result = await _projectionService.GetVersionsAsync(GetUserId(), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading projection versions");
            return StatusCode(500, new { message = "Error loading projection versions" });
        }
    }

    /// <summary>
    /// Returns the full detail of a single version including its saved data points.
    /// </summary>
    [HttpGet("versions/{id:int}")]
    public async Task<ActionResult<ProjectionVersionDetailDto>> GetVersionDetail(int id, CancellationToken ct = default)
    {
        try
        {
            var result = await _projectionService.GetVersionDetailAsync(GetUserId(), id, ct);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading projection version {Id}", id);
            return StatusCode(500, new { message = "Error loading projection version" });
        }
    }

    /// <summary>
    /// Deletes a saved projection version belonging to the authenticated user.
    /// </summary>
    [HttpDelete("versions/{id:int}")]
    public async Task<IActionResult> DeleteVersion(int id, CancellationToken ct = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "This profile is shared as read-only." });

            var deleted = await _projectionService.DeleteVersionAsync(GetUserId(), id, ct);
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting projection version {Id}", id);
            return StatusCode(500, new { message = "Error deleting projection version" });
        }
    }

    /// <summary>
    /// Sets the specified version as the default for the authenticated user.
    /// </summary>
    [HttpPatch("versions/{id:int}/default")]
    public async Task<ActionResult<ProjectionVersionSummaryDto>> SetDefaultVersion(int id, CancellationToken ct = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "This profile is shared as read-only." });

            var result = await _projectionService.SetDefaultVersionAsync(GetUserId(), id, ct);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default projection version {Id}", id);
            return StatusCode(500, new { message = "Error setting default version" });
        }
    }
}
