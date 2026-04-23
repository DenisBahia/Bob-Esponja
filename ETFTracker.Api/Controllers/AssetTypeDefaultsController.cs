using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ETFTracker.Api.Dtos;
using ETFTracker.Api.Services;

namespace ETFTracker.Api.Controllers;

/// <summary>
/// Manages the user's preferred deemed-disposal flag per asset type (ETF, EQUITY, etc.).
/// Used to pre-fill the deemed disposal toggle in the buy modal.
/// </summary>
[Authorize]
[ApiController]
[Route("api/asset-type-defaults")]
public class AssetTypeDefaultsController : ControllerBase
{
    private readonly IAssetTypeDeemedDisposalDefaultService _service;
    private readonly ISharingContextService _sharingContext;

    public AssetTypeDefaultsController(
        IAssetTypeDeemedDisposalDefaultService service,
        ISharingContextService sharingContext)
    {
        _service = service;
        _sharingContext = sharingContext;
    }

    private int UserId => _sharingContext.GetEffectiveUserId();

    /// <summary>Get all asset-type deemed-disposal defaults for the current user.</summary>
    [HttpGet]
    public async Task<ActionResult<List<AssetTypeDeemedDisposalDefaultDto>>> GetDefaults(
        CancellationToken ct = default)
    {
        var result = await _service.GetDefaultsAsync(UserId, ct);
        return Ok(result);
    }

    /// <summary>Upsert the deemed-disposal default for a specific asset type.</summary>
    [HttpPost]
    public async Task<ActionResult<AssetTypeDeemedDisposalDefaultDto>> Upsert(
        [FromBody] AssetTypeDeemedDisposalDefaultDto dto, CancellationToken ct = default)
    {
        if (_sharingContext.IsReadOnly())
            return StatusCode(403, new { message = "Read-only profile." });

        if (string.IsNullOrWhiteSpace(dto.AssetType))
            return BadRequest(new { message = "AssetType is required." });

        var result = await _service.UpsertAsync(UserId, dto, ct);
        return Ok(result);
    }
}

