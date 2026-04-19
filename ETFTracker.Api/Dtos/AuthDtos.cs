using System.ComponentModel.DataAnnotations;

namespace ETFTracker.Api.Dtos;

public record RegisterDto(
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, MinLength(8), StringLength(128)] string Password,
    string? FirstName,
    string? LastName
);

public record LoginDto(
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, MinLength(1), StringLength(128)] string Password
);

