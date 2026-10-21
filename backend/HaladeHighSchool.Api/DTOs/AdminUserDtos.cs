namespace HaladeHighSchool.Api.DTOs;

/// <summary>An account and its assigned roles in the administrator user directory.</summary>
public sealed record AdminUserResponse(
    string Id,
    string FullName,
    string? Email,
    IReadOnlyList<string> Roles,
    bool IsActive,
    bool IsLockedOut);

/// <summary>Changes whether an account can sign in.</summary>
public sealed record UpdateAdminUserStatusRequest(bool IsActive);
