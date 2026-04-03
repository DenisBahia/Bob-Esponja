using Microsoft.AspNetCore.Mvc;
using ETFTracker.Api.Dtos;
using ETFTracker.Api.Services;

namespace ETFTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectionsController : ControllerBase
{
    private readonly IProjectionService _projectionService;
    private readonly ILogger<ProjectionsController> _logger;
    private const int DefaultUserId = 1;

    public ProjectionsController(IProjectionService projectionService, ILogger<ProjectionsController> logger)
    {
        _projectionService = projectionService;
        _logger = logger;
    }

    /// <summary>
    /// Returns current projection settings + calculated data points.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ProjectionResultDto>> GetProjection(CancellationToken ct = default)
    {
        try
        {
            var result = await _projectionService.GetProjectionAsync(DefaultUserId, ct);
            return Ok(result);
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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var saved = await _projectionService.SaveSettingsAsync(DefaultUserId, dto, ct);
            return Ok(saved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving projection settings");
            return StatusCode(500, new { message = "Error saving projection settings" });
        }
    }
}

