namespace HaladeHighSchool.Api.Services;

public interface IEmailService
{
    Task SendStudentApprovalEmailAsync(
        string toEmail,
        string fullName,
        string generatedSchoolEmail,
        string tempPassword,
        CancellationToken cancellationToken = default);

    Task SendTeacherApprovalEmailAsync(
        string toEmail,
        string fullName,
        string registeredPassword,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetApprovedEmailAsync(
        string toEmail,
        string fullName,
        string temporaryPassword,
        string loginUrl,
        CancellationToken cancellationToken = default);
}
