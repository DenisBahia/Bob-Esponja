using ETFTracker.Api.Data;
using ETFTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ETFTracker.Api.Services;

public class SharingContextService : ISharingContextService
{
    private readonly IHttpContextAccessor _http;
    private readonly AppDbContext _db;

    private bool _resolved;
    private int _effectiveUserId;
    private bool _isReadOnly;
    private bool _isViewingAsOther;
    private bool _isUnauthorized;   // true when X-View-As-User was present but invalid

    public SharingContextService(IHttpContextAccessor http, AppDbContext db)
    {
        _http = http;
        _db   = db;
    }

    public int GetEffectiveUserId()
    {
        Resolve();
        if (_isUnauthorized)
            throw new UnauthorizedAccessException("No active share found for this profile.");
        return _effectiveUserId;
    }

    public bool IsReadOnly()       { Resolve(); return _isReadOnly; }
    public bool IsViewingAsOther() { Resolve(); return _isViewingAsOther; }

    /// <summary>True when the X-View-As-User header was sent but the share was not found/active.</summary>
    public bool IsUnauthorizedSharedAccess { get { Resolve(); return _isUnauthorized; } }

    private void Resolve()
    {
        if (_resolved) return;
        _resolved = true;

        var ctx = _http.HttpContext!;
        var callerIdStr = ctx.User.FindFirst("userId")?.Value;
        if (callerIdStr == null || !int.TryParse(callerIdStr, out var callerId))
            throw new UnauthorizedAccessException("userId claim missing");

        var viewAsHeader = ctx.Request.Headers["X-View-As-User"].FirstOrDefault();
        if (string.IsNullOrEmpty(viewAsHeader) || !int.TryParse(viewAsHeader, out var ownerId))
        {
            _effectiveUserId  = callerId;
            _isReadOnly       = false;
            _isViewingAsOther = false;
            return;
        }

        // Primary lookup: by GuestUserId (most reliable – always set for Active shares)
        var share = _db.ProfileShares
            .AsNoTracking()
            .FirstOrDefault(s =>
                s.OwnerId     == ownerId &&
                s.GuestUserId == callerId &&
                s.Status      == ShareStatus.Active);

        // Fallback: match by caller email (handles edge cases where GuestUserId wasn't linked)
        if (share == null)
        {
            var callerEmail = ctx.User.FindFirst("email")?.Value?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(callerEmail))
            {
                share = _db.ProfileShares
                    .AsNoTracking()
                    .FirstOrDefault(s =>
                        s.OwnerId    == ownerId &&
                        s.GuestEmail == callerEmail &&
                        s.Status     == ShareStatus.Active);

                // If found via email, eagerly fix GuestUserId so future requests use the fast path
                if (share != null)
                {
                    var tracked = _db.ProfileShares.Find(share.Id);
                    if (tracked != null && tracked.GuestUserId == null)
                    {
                        tracked.GuestUserId = callerId;
                        tracked.UpdatedAt   = DateTime.UtcNow;
                        _db.SaveChanges();
                    }
                }
            }
        }

        if (share == null)
        {
            // Mark as unauthorized – controllers will return 403
            _isUnauthorized   = true;
            _effectiveUserId  = callerId; // safe fallback
            _isReadOnly       = true;
            _isViewingAsOther = false;
            return;
        }

        _effectiveUserId  = ownerId;
        _isReadOnly       = share.IsReadOnly;
        _isViewingAsOther = true;
    }
}
