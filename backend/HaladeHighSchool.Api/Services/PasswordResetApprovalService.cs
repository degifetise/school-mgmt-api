using System.Security.Cryptography;
using HaladeHighSchool.Api.Configuration;
using HaladeHighSchool.Api.Data;
using HaladeHighSchool.Api.DTOs;
using HaladeHighSchool.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HaladeHighSchool.Api.Services;

public sealed class PasswordResetApprovalService : IPasswordResetApprovalService
{
    private const string PendingStatus = "Pending";
    private const string ApprovedStatus = "Approved";
    private const string RejectedStatus = "Rejected";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly EmailSettings _emailSettings;
    private readonly ITokenService _tokens;
    private readonly ILogger<PasswordResetApprovalService> _logger;

    public PasswordResetApprovalService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IOptions<EmailSettings> emailSettings,
        ITokenService tokens,
        ILogger<PasswordResetApprovalService> logger)
    {
        _db = db;
        _userManager = userManager;
        _emailService = emailService;
        _emailSettings = emailSettings.Value;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task RequestAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        var deliveryEmail = normalizedEmail;
        if (user is null)
        {
            var registration = await _db.StudentRegistrationRequests
                .AsNoTracking()
                .Where(request => request.Status == RegistrationRequestStatus.Approved
                    && request.ContactEmail.ToUpper() == normalizedEmail.ToUpper()
                    && request.CreatedStudent != null
                    && request.CreatedStudent.UserId != null)
                .OrderByDescending(request => request.SubmittedAt)
                .Select(request => new
                {
                    UserId = request.CreatedStudent!.UserId!,
                    request.ContactEmail,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (registration is not null)
            {
                user = await _userManager.FindByIdAsync(registration.UserId);
                deliveryEmail = registration.ContactEmail;
            }
        }

        if (user is null || !user.IsActive)
        {
            return;
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Any(candidate =>
                string.Equals(candidate, Roles.Admin, StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate, Roles.SchoolAdmin, StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var role = roles.FirstOrDefault(candidate =>
            string.Equals(candidate, Roles.Student, StringComparison.OrdinalIgnoreCase)
            || string.Equals(candidate, Roles.Teacher, StringComparison.OrdinalIgnoreCase));
        if (role is null)
        {
            return;
        }

        var alreadyPending = await _db.PasswordResetRequests.AnyAsync(
            request => request.UserId == user.Id
                && request.Token == null
                && request.Status == PendingStatus,
            cancellationToken);
        if (alreadyPending)
        {
            return;
        }

        var now = DateTime.UtcNow;
        _db.PasswordResetRequests.Add(new PasswordResetRequest
        {
            UserId = user.Id,
            Email = deliveryEmail,
            Role = role,
            RequestDate = now,
            CreatedAt = now,
            Status = PendingStatus,
            IsApproved = false,
        });
        var admins = (await _userManager.GetUsersInRoleAsync(Roles.Admin))
            .Concat(await _userManager.GetUsersInRoleAsync(Roles.SchoolAdmin))
            .Where(admin => admin.SchoolId == user.SchoolId)
            .DistinctBy(admin => admin.Id);
        foreach (var admin in admins)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = admin.Id,
                Title = "Password reset awaiting approval",
                Message = $"{role} {user.FullName} requested a password reset.",
                Type = "PasswordReset",
                TargetUrl = "/admin/dashboard",
                CreatedAt = now,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PendingPasswordResetResponse>> GetPendingAsync(
        Guid schoolId,
        CancellationToken cancellationToken) =>
        await _db.PasswordResetRequests
            .AsNoTracking()
            .Where(request => request.Token == null
                && request.Status == PendingStatus
                && request.User != null
                && request.User.SchoolId == schoolId)
            .OrderBy(request => request.RequestDate)
            .Select(request => new PendingPasswordResetResponse(
                request.Id,
                request.User!.FullName,
                request.Email,
                request.Role,
                new DateTimeOffset(DateTime.SpecifyKind(request.RequestDate, DateTimeKind.Utc))))
            .ToListAsync(cancellationToken);

    public async Task<PasswordResetDecisionResponse?> ApproveAsync(
        long requestId,
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        var temporaryPassword = GenerateTemporaryPassword();
        var executionStrategy = _db.Database.CreateExecutionStrategy();
        var approvedRequest = await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var claimed = await _db.PasswordResetRequests
                .Where(request => request.Id == requestId
                    && request.Token == null
                    && request.Status == PendingStatus
                    && request.User != null
                    && request.User.SchoolId == schoolId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(request => request.Status, ApprovedStatus)
                    .SetProperty(request => request.IsApproved, true)
                    .SetProperty(request => request.ProcessedDate, DateTime.UtcNow),
                    cancellationToken);
            if (claimed == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var request = await _db.PasswordResetRequests
                .Include(item => item.User)
                .SingleAsync(item => item.Id == requestId, cancellationToken);
            if (request.User is null || !request.User.IsActive)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var identityToken = await _userManager.GeneratePasswordResetTokenAsync(request.User);
            var passwordResult = await _userManager.ResetPasswordAsync(
                request.User,
                identityToken,
                temporaryPassword);
            if (!passwordResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"The temporary password could not be applied: {string.Join("; ", passwordResult.Errors.Select(error => error.Description))}");
            }

            request.User.MustChangePasswordOnLogin = true;
            var updateResult = await _userManager.UpdateAsync(request.User);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"The account's temporary password requirement could not be saved: {string.Join("; ", updateResult.Errors.Select(error => error.Description))}");
            }

            await _tokens.RevokeAllForUserAsync(request.UserId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return request;
        });

        if (approvedRequest?.User is null)
        {
            return null;
        }

        var emailSent = false;
        try
        {
            await _emailService.SendPasswordResetApprovedEmailAsync(
                approvedRequest.Email,
                approvedRequest.User.FullName,
                temporaryPassword,
                BuildLoginUrl(),
                cancellationToken);
            emailSent = true;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Approved password reset for request {RequestId} was saved, but delivery failed for user {UserId}.",
                requestId,
                approvedRequest.UserId);
        }

        return new PasswordResetDecisionResponse(
            emailSent
                ? "The password reset was approved and a temporary password was emailed."
                : "The password reset was approved, but the email could not be delivered. Contact the user securely.",
            emailSent);
    }

    public async Task<bool> RejectAsync(
        long requestId,
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var updated = await _db.PasswordResetRequests
            .Where(request => request.Id == requestId
                && request.Token == null
                && request.Status == PendingStatus
                && request.User != null
                && request.User.SchoolId == schoolId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(request => request.Status, RejectedStatus)
                .SetProperty(request => request.IsApproved, false)
                .SetProperty(request => request.ProcessedDate, now),
                cancellationToken);
        return updated > 0;
    }

    private string BuildLoginUrl()
    {
        var frontendUrl = string.IsNullOrWhiteSpace(_emailSettings.FrontendBaseUrl)
            ? "http://localhost:5173"
            : _emailSettings.FrontendBaseUrl.TrimEnd('/');
        return $"{frontendUrl}/login";
    }

    private static string GenerateTemporaryPassword()
    {
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string symbols = "!@#$%&*?";
        var all = lower + upper + digits + symbols;
        var characters = new char[20];
        characters[0] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        characters[1] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        characters[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        characters[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];
        for (var index = 4; index < characters.Length; index++)
        {
            characters[index] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        for (var index = characters.Length - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (characters[index], characters[swapIndex]) = (characters[swapIndex], characters[index]);
        }

        return new string(characters);
    }
}
