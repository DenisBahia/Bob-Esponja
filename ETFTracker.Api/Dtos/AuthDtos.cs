using System.ComponentModel.DataAnnotations;

namespace ETFTracker.Api.Dtos;

public record RegisterDto(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    string? FirstName,
    string? LastName
);

public record LoginDto(
    [Required, EmailAddress] string Email,
    [Required]               string Password
);

