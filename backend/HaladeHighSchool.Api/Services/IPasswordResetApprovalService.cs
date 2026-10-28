using HaladeHighSchool.Api.DTOs;

namespace HaladeHighSchool.Api.Services;

public interface IPasswordResetApprovalService
{
    Task RequestAsync(string email, CancellationToken cancellationToken);

    Task<IReadOnlyList<PendingPasswordResetResponse>> GetPendingAsync(
        Guid schoolId,
        CancellationToken cancellationToken);

    Task<PasswordResetDecisionResponse?> ApproveAsync(
        long requestId,
        Guid schoolId,
        CancellationToken cancellationToken);

    Task<bool> RejectAsync(long requestId, Guid schoolId, CancellationToken cancellationToken);
}
