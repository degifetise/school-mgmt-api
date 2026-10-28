namespace HaladeHighSchool.Api.Models;

/// <summary>A one-time password reset token issued to an existing account.</summary>
public class PasswordResetRequest
{
    public long Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>Null for a request awaiting administrator approval.</summary>
    public string? Token { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public string? RequestedFromIp { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;

    public bool IsApproved { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime? ProcessedDate { get; set; }

    public ApplicationUser? User { get; set; }
}
