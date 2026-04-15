using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ETFTracker.Api.Dtos;
using ETFTracker.Api.Services;

namespace ETFTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GoalController : ControllerBase
{
    private readonly IGoalService _goalService;
    private readonly ISharingContextService _sharingContext;
    private readonly ILogger<GoalController> _logger;

    public GoalController(
        IGoalService goalService,
        ISharingContextService sharingContext,
        ILogger<GoalController> logger)
    {
        _goalService    = goalService;
        _sharingContext = sharingContext;
        _logger         = logger;
    }

    private int GetUserId() => _sharingContext.GetEffectiveUserId();

    /// <summary>Returns the authenticated user's goal, or 404 if none has been saved yet.</summary>
    [HttpGet]
    public async Task<ActionResult<UserGoalDto>> GetGoal(CancellationToken ct = default)
    {
        try
        {
            var goal = await _goalService.GetGoalAsync(GetUserId(), ct);
            if (goal == null) return NotFound();
            return Ok(goal);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading goal");
            return StatusCode(500, new { message = "Error loading goal" });
        }
    }

    /// <summary>Creates or replaces the authenticated user's goal with the supplied data points.</summary>
    [HttpPut]
    public async Task<ActionResult<UserGoalDto>> UpsertGoal(
        [FromBody] UpsertGoalRequestDto dto,
        CancellationToken ct = default)
    {
        try
        {
            if (_sharingContext.IsReadOnly())
                return StatusCode(403, new { message = "This profile is shared as read-only." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _goalService.UpsertGoalAsync(GetUserId(), dto, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving goal");
            return StatusCode(500, new { message = "Error saving goal" });
        }
    }
}

