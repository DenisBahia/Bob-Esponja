using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ETFTracker.Api.Data;
using ETFTracker.Api.Models;
using ETFTracker.Api.Services;

namespace ETFTracker.Api.Controllers;

/// <summary>
/// Handles authentication and user management including OAuth (GitHub, Google) and JWT tokens.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext    _db;
    private readonly JwtService      _jwtService;
    private readonly IConfiguration  _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext db,
        JwtService jwtService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _db            = db;
        _jwtService    = jwtService;
        _configuration = configuration;
        _logger        = logger;
    }

    // ── GitHub ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initiates GitHub OAuth login flow.
    /// </summary>
    /// <returns>Redirects to GitHub for authentication</returns>
    [HttpGet("github")]
    public IActionResult LoginWithGitHub()
    {
        var redirectUri = BuildCallbackUri(nameof(GitHubComplete));
        return Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, "GitHub");
    }

    /// <summary>
    /// GitHub OAuth callback handler. Completes the OAuth flow and returns a JWT token.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Redirects to frontend with JWT token in query parameter</returns>
    [HttpGet("github/complete")]
    public async Task<IActionResult> GitHubComplete(CancellationToken ct = default)
        => await HandleOAuthComplete("GitHub", ct);

    // ── Google ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initiates Google OAuth login flow.
    /// </summary>
    /// <returns>Redirects to Google for authentication</returns>
    [HttpGet("google")]
    public IActionResult LoginWithGoogle()
    {
        var redirectUri = BuildCallbackUri(nameof(GoogleComplete));
        return Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, "Google");
    }

    /// <summary>
    /// Google OAuth callback handler. Completes the OAuth flow and returns a JWT token.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Redirects to frontend with JWT token in query parameter</returns>
    [HttpGet("google/complete")]
    public async Task<IActionResult> GoogleComplete(CancellationToken ct = default)
        => await HandleOAuthComplete("Google", ct);

    // ── Me ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the current authenticated user's information.
    /// </summary>
    /// <returns>User object containing id, email, name, and avatar information</returns>
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser() => Ok(new
    {
        userId        = User.FindFirst("userId")?.Value,
        email         = User.FindFirst("email")?.Value,
        name          = User.FindFirst("name")?.Value,
        avatarUrl     = User.FindFirst("avatarUrl")?.Value,
        githubUsername = User.FindFirst("githubUsername")?.Value,
    });

    // ── Shared handler ─────────────────────────────────────────────────────────

    /// <summary>
    /// Builds an absolute callback URI for the given action, always using https
    /// when the forwarded-proto header says so (Render reverse-proxy).
    /// </summary>
    private string BuildCallbackUri(string actionName)
    {
        // Request.Scheme is already corrected to "https" by the ForwardedHeaders
        // middleware when running behind Render. We double-guard here just in case.
        var scheme = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? Request.Scheme;
        return Url.Action(actionName, "Auth", null, scheme)!;
    }

    private async Task<IActionResult> HandleOAuthComplete(string provider, CancellationToken ct)
    {
        var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";

        // Read the user that the OAuth middleware signed in to the external cookie
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            _logger.LogWarning("OAuth sign-in failed for {Provider}: {Error}", provider, result.Failure?.Message);
            return Redirect($"{frontendUrl}/login?error=oauth_failed");
        }

        var principal = result.Principal!;
        var now       = DateTime.UtcNow;
        User? user;

        if (provider == "GitHub")
        {
            var githubId       = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var githubUsername = principal.FindFirst("urn:github:login")?.Value;
            var email          = principal.FindFirst(ClaimTypes.Email)?.Value;
            var name           = principal.FindFirst(ClaimTypes.Name)?.Value
                              ?? principal.FindFirst("urn:github:name")?.Value;
            var avatarUrl      = principal.FindFirst("urn:github:avatar")?.Value;

            _logger.LogInformation("GitHub OAuth – id:{Id} login:{Login} email:{Email}", githubId, githubUsername, email);

            // 1. By GitHub ID
            user = await _db.Users.FirstOrDefaultAsync(u => u.GitHubId == githubId, ct);
            // 2. By pre-seeded GitHub username (links DenisBahia's existing data)
            user ??= await _db.Users.FirstOrDefaultAsync(u => u.GitHubUsername == githubUsername, ct);
            // 3. By email
            if (user == null && !string.IsNullOrEmpty(email))
                user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

            if (user == null)
            {
                var parts = (name ?? "").Split(' ', 2);
                user = new User
                {
                    Email          = email,
                    FirstName      = parts.Length > 0 ? parts[0] : null,
                    LastName       = parts.Length > 1 ? parts[1] : null,
                    AvatarUrl      = avatarUrl,
                    GitHubId       = githubId,
                    GitHubUsername = githubUsername,
                    CreatedAt      = now,
                    UpdatedAt      = now
                };
                _db.Users.Add(user);
            }
            else
            {
                user.GitHubId       = githubId;
                user.GitHubUsername ??= githubUsername;
                user.AvatarUrl      ??= avatarUrl;
                user.Email          ??= email;
                user.UpdatedAt       = now;
            }
        }
        else // Google
        {
            var googleId  = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email     = principal.FindFirst(ClaimTypes.Email)?.Value;
            var firstName = principal.FindFirst(ClaimTypes.GivenName)?.Value;
            var lastName  = principal.FindFirst(ClaimTypes.Surname)?.Value;
            var avatarUrl = principal.FindFirst("urn:google:picture")?.Value
                         ?? principal.FindFirst("picture")?.Value;

            _logger.LogInformation("Google OAuth – id:{Id} email:{Email}", googleId, email);

            // 1. By Google ID
            user = await _db.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId, ct);
            // 2. By email
            if (user == null && !string.IsNullOrEmpty(email))
                user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

            if (user == null)
            {
                user = new User
                {
                    Email     = email,
                    FirstName = firstName,
                    LastName  = lastName,
                    AvatarUrl = avatarUrl,
                    GoogleId  = googleId,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _db.Users.Add(user);
            }
            else
            {
                user.GoogleId  = googleId;
                user.AvatarUrl ??= avatarUrl;
                user.Email     ??= email;
                user.UpdatedAt  = now;
            }
        }

        await _db.SaveChangesAsync(ct);

        // Sign out the temporary external cookie now that we have our own JWT
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var token = _jwtService.GenerateToken(user);
        return Redirect($"{frontendUrl}/auth/callback?token={Uri.EscapeDataString(token)}");
    }
}

