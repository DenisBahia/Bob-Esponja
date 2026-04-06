using System.ComponentModel.DataAnnotations;
using ETFTracker.Api.Models;

namespace ETFTracker.Api.Dtos;

public class CreateShareDto
{
    [Required, EmailAddress]
    public string GuestEmail { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; } = true;
}

public class UpdateShareDto
{
    public bool? IsReadOnly { get; set; }
    public ShareStatus? Status { get; set; }
}

public class ShareSummaryDto
{
    public int Id { get; set; }
    public string GuestEmail { get; set; } = string.Empty;
    public string? GuestName { get; set; }
    public bool IsReadOnly { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsLinked { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SharedWithMeDto
{
    public int Id { get; set; }
    public int OwnerUserId { get; set; }
    public string OwnerEmail { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? OwnerAvatarUrl { get; set; }
    public bool IsReadOnly { get; set; }
}

