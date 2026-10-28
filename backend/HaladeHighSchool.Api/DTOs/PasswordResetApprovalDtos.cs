namespace HaladeHighSchool.Api.DTOs;

/// <summary>One password reset request awaiting administrator review.</summary>
public sealed record PendingPasswordResetResponse(
    long Id,
    string FullName,
    string Email,
    string Role,
    DateTimeOffset RequestDate);

/// <summary>Result of an administrator's password reset decision.</summary>
public sealed record PasswordResetDecisionResponse(string Message, bool EmailSent);
