using HaladeHighSchool.Api.Configuration;
using HaladeHighSchool.Api.DTOs;
using HaladeHighSchool.Api.Models;
using HaladeHighSchool.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HaladeHighSchool.Api.Controllers;

/// <summary>Administrator-only listing and access management for portal accounts.</summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public sealed class AdminUsersController : PortalControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ILogger<AdminUsersController> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>Lists all portal accounts, including role membership and access status.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminUserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminUserResponse>>> GetUsers(
        CancellationToken cancellationToken)
    {
        var users = await _userManager.Users
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);
        var responses = new List<AdminUserResponse>(users.Count);

        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var roles = await _userManager.GetRolesAsync(user);
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            responses.Add(new AdminUserResponse(
                user.Id,
                user.FullName,
                user.Email,
                roles.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
                user.IsActive,
                lockoutEnd is not null && lockoutEnd > DateTimeOffset.UtcNow));
        }

        return Ok(responses);
    }

    /// <summary>Assigns the Admin role to an existing account.</summary>
    [HttpPost("{id}/promote")]
    [ProducesResponseType<AdminUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserResponse>> PromoteToAdmin(
        string id,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFoundProblem("The requested user account was not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase))
        {
            var result = await _userManager.AddToRoleAsync(user, Roles.Admin);
            if (!result.Succeeded)
            {
                return ConflictProblem(
                    "Admin role could not be assigned",
                    string.Join("; ", result.Errors.Select(error => error.Description)));
            }

            _logger.LogInformation(
                "Admin {AdminUserId} promoted user {UserId} to Admin",
                User.GetUserId(),
                user.Id);
        }

        var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
        return Ok(new AdminUserResponse(
            user.Id,
            user.FullName,
            user.Email,
            roles.Append(Roles.Admin).Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            user.IsActive,
            lockoutEnd is not null && lockoutEnd > DateTimeOffset.UtcNow));
    }

    /// <summary>Activates or deactivates an account while retaining its records and roles.</summary>
    [HttpPatch("{id}/status")]
    [ProducesResponseType<AdminUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserResponse>> UpdateStatus(
        string id,
        UpdateAdminUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFoundProblem("The requested user account was not found.");
        }

        if (!request.IsActive && string.Equals(User.GetUserId(), user.Id, StringComparison.Ordinal))
        {
            return BadRequestProblem("Account cannot be deactivated", "You cannot deactivate your own account.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (!request.IsActive && roles.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase))
        {
            var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            var activeAdminCount = admins.Count(admin => admin.IsActive);
            if (user.IsActive && activeAdminCount <= 1)
            {
                return ConflictProblem(
                    "Last active administrator",
                    "Promote and activate another administrator before deactivating this account.");
            }
        }

        user.IsActive = request.IsActive;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ConflictProblem(
                "Account status could not be updated",
                string.Join("; ", updateResult.Errors.Select(error => error.Description)));
        }

        if (!request.IsActive)
        {
            await _tokenService.RevokeAllForUserAsync(user.Id, cancellationToken);
        }

        _logger.LogInformation(
            "Admin {AdminUserId} changed user {UserId} active status to {IsActive}",
            User.GetUserId(),
            user.Id,
            user.IsActive);

        var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
        return Ok(new AdminUserResponse(
            user.Id,
            user.FullName,
            user.Email,
            roles.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            user.IsActive,
            lockoutEnd is not null && lockoutEnd > DateTimeOffset.UtcNow));
    }
}
