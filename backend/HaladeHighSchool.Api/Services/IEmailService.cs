namespace HaladeHighSchool.Api.Services;

public interface IEmailService
{
    Task SendStudentApprovalEmailAsync(
        string toEmail,
        string fullName,
        string generatedSchoolEmail,
        string tempPassword);

    Task SendTeacherApprovalEmailAsync(
        string toEmail,
        string fullName,
        string registeredPassword);
}
