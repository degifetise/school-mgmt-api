using HaladeHighSchool.Api.Configuration;
using HaladeHighSchool.Api.DTOs;
using HaladeHighSchool.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HaladeHighSchool.Api.Controllers;

/// <summary>Administrator review of pending password reset requests.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.AdminOrSchoolAdmin)]
[Produces("application/json")]
public sealed class AdminPasswordResetRequestsController : PortalControllerBase
{
    private readonly IPasswordResetApprovalService _passwordResets;

    public AdminPasswordResetRequestsController(IPasswordResetApprovalService passwordResets)
    {
        _passwordResets = passwordResets;
    }

    /// <summary>Lists pending password reset requests for the current school.</summary>
    [HttpGet("password-reset-requests/pending")]
    [ProducesResponseType<IReadOnlyList<PendingPasswordResetResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PendingPasswordResetResponse>>> GetPending(
        CancellationToken cancellationToken) =>
        Ok(await _passwordResets.GetPendingAsync(CurrentSchoolId, cancellationToken));

    /// <summary>Approves a reset, assigns a temporary password, and emails the account holder.</summary>
    [HttpPost("approve-password-reset/{requestId:long}")]
    [ProducesResponseType<PasswordResetDecisionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PasswordResetDecisionResponse>> Approve(
        long requestId,
        CancellationToken cancellationToken)
    {
        var result = await _passwordResets.ApproveAsync(requestId, CurrentSchoolId, cancellationToken);
        return result is null
            ? NotFoundProblem("The pending password reset request was not found.")
            : Ok(result);
    }

    /// <summary>Rejects a pending password reset request.</summary>
    [HttpPost("reject-password-reset/{requestId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(long requestId, CancellationToken cancellationToken)
    {
        var rejected = await _passwordResets.RejectAsync(requestId, CurrentSchoolId, cancellationToken);
        return rejected ? NoContent() : NotFoundProblem("The pending password reset request was not found.");
    }
}
