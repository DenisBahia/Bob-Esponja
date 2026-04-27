using System.ComponentModel.DataAnnotations;

namespace ETFTracker.Api.Dtos;

public record RegisterDto(
    [Required, StringLength(50, MinimumLength = 3)] string Username,
    [EmailAddress, StringLength(320)] string? Email,
    [Required, MinLength(8), StringLength(128)] string Password,
    string? FirstName,
    string? LastName
);

public record LoginDto(
    /// <summary>Email address or username.</summary>
    [Required, StringLength(320)] string EmailOrUsername,
    [Required, MinLength(1), StringLength(128)] string Password
);

