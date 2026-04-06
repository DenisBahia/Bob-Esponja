using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ETFTracker.Api.Data;
using ETFTracker.Api.Dtos;
using ETFTracker.Api.Models;

namespace ETFTracker.Api.Controllers;

/// <summary>
/// Manages profile sharing – allows users to share their portfolio with other registered users.
/// </summary>
[Authorize]
[ApiController]
[Route("api/sharing")]
public class SharingController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<SharingController> _logger;

    public SharingController(AppDbContext db, ILogger<SharingController> logger)
    {
        _db     = db;
        _logger = logger;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedAccessException("userId claim missing"));

    // ── POST /api/sharing ─────────────────────────────────────────────────────

    /// <summary>Share your portfolio with another user by email.</summary>
    [HttpPost]
    public async Task<ActionResult<ShareSummaryDto>> CreateShare(
        [FromBody] CreateShareDto dto,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ownerId    = GetUserId();
        var guestEmail = dto.GuestEmail.Trim().ToLowerInvariant();

        // Prevent sharing with yourself
        var owner = await _db.Users.FindAsync(new object[] { ownerId }, ct);
        if (owner?.Email?.ToLowerInvariant() == guestEmail)
            return BadRequest(new { message = "You cannot share your profile with yourself." });

        // Duplicate check
        var existing = await _db.ProfileShares
            .FirstOrDefaultAsync(s => s.OwnerId == ownerId && s.GuestEmail == guestEmail, ct);
        if (existing != null && existing.Status != ShareStatus.Revoked)
            return Conflict(new { message = "You already have a share for this email address." });

        var now = DateTime.UtcNow;

        // Try to resolve the guest user now
        var guestUser = await _db.Users
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == guestEmail, ct);

        ProfileShare share;
        if (existing != null) // it was Revoked – reactivate
        {
            existing.IsReadOnly  = dto.IsReadOnly;
            existing.GuestUserId = guestUser?.Id;
            existing.Status      = guestUser != null ? ShareStatus.Active : ShareStatus.Pending;
            existing.UpdatedAt   = now;
            share = existing;
        }
        else
        {
            share = new ProfileShare
            {
                OwnerId      = ownerId,
                GuestEmail   = guestEmail,
                GuestUserId  = guestUser?.Id,
                IsReadOnly   = dto.IsReadOnly,
                Status       = guestUser != null ? ShareStatus.Active : ShareStatus.Pending,
                CreatedAt    = now,
                UpdatedAt    = now
            };
            _db.ProfileShares.Add(share);
        }

        await _db.SaveChangesAsync(ct);

        return Ok(ToSummaryDto(share, guestUser));
    }

    // ── GET /api/sharing/my-shares ───────────────────────────────────────────

    /// <summary>List all shares the current user has granted to others.</summary>
    [HttpGet("my-shares")]
    public async Task<ActionResult<List<ShareSummaryDto>>> GetMyShares(CancellationToken ct = default)
    {
        var ownerId = GetUserId();
        var shares  = await _db.ProfileShares
            .Where(s => s.OwnerId == ownerId && s.Status != ShareStatus.Revoked)
            .Include(s => s.GuestUser)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

        return Ok(shares.Select(s => ToSummaryDto(s, s.GuestUser)).ToList());
    }

    // ── PUT /api/sharing/{id} ────────────────────────────────────────────────

    /// <summary>Update IsReadOnly or Status for a share you own.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ShareSummaryDto>> UpdateShare(
        int id,
        [FromBody] UpdateShareDto dto,
        CancellationToken ct = default)
    {
        var ownerId = GetUserId();
        var share   = await _db.ProfileShares
            .Include(s => s.GuestUser)
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == ownerId, ct);

        if (share == null) return NotFound();

        if (dto.IsReadOnly.HasValue) share.IsReadOnly = dto.IsReadOnly.Value;
        if (dto.Status.HasValue)     share.Status     = dto.Status.Value;
        share.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(ToSummaryDto(share, share.GuestUser));
    }

    // ── DELETE /api/sharing/{id} ─────────────────────────────────────────────

    /// <summary>Revoke a share (soft-delete).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteShare(int id, CancellationToken ct = default)
    {
        var ownerId = GetUserId();
        var share   = await _db.ProfileShares
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == ownerId, ct);

        if (share == null) return NotFound();

        share.Status    = ShareStatus.Revoked;
        share.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── GET /api/sharing/shared-with-me ──────────────────────────────────────

    /// <summary>List all profiles that have been shared with the current user.</summary>
    [HttpGet("shared-with-me")]
    public async Task<ActionResult<List<SharedWithMeDto>>> GetSharedWithMe(CancellationToken ct = default)
    {
        var userId = GetUserId();
        var user   = await _db.Users.FindAsync(new object[] { userId }, ct);
        var email  = user?.Email?.ToLowerInvariant() ?? string.Empty;

        var shares = await _db.ProfileShares
            .Where(s =>
                s.Status != ShareStatus.Revoked &&
                (s.GuestUserId == userId || s.GuestEmail == email))
            .Include(s => s.Owner)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

        // Auto-link any Pending shares that matched by email – this handles the
        // case where the share was created before the guest registered, or the
        // auto-link in the login callback was missed for any reason.
        var needsLinking = shares
            .Where(s => s.GuestUserId == null && s.Status == ShareStatus.Pending)
            .ToList();

        if (needsLinking.Count > 0)
        {
            var now = DateTime.UtcNow;
            foreach (var s in needsLinking)
            {
                s.GuestUserId = userId;
                s.Status      = ShareStatus.Active;
                s.UpdatedAt   = now;
            }
            await _db.SaveChangesAsync(ct);
        }

        // Only return Active shares (no point showing Pending/Revoked to the guest)
        var activeShares = shares.Where(s => s.Status == ShareStatus.Active).ToList();

        return Ok(activeShares.Select(s => new SharedWithMeDto
        {
            Id           = s.Id,
            OwnerUserId  = s.OwnerId,
            OwnerEmail   = s.Owner?.Email ?? string.Empty,
            OwnerName    = s.Owner != null
                ? $"{s.Owner.FirstName} {s.Owner.LastName}".Trim()
                : null,
            OwnerAvatarUrl = s.Owner?.AvatarUrl,
            IsReadOnly   = s.IsReadOnly
        }).ToList());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ShareSummaryDto ToSummaryDto(ProfileShare s, User? guest) => new()
    {
        Id         = s.Id,
        GuestEmail = s.GuestEmail,
        GuestName  = guest != null
            ? $"{guest.FirstName} {guest.LastName}".Trim()
            : null,
        IsReadOnly = s.IsReadOnly,
        Status     = s.Status.ToString(),
        IsLinked   = s.GuestUserId.HasValue,
        CreatedAt  = s.CreatedAt
    };
}


