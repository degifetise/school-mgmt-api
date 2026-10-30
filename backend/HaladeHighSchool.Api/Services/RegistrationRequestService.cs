using HaladeHighSchool.Api.Data;
using HaladeHighSchool.Api.DTOs;
using HaladeHighSchool.Api.Models;
using HaladeHighSchool.Api.Configuration;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using System.Globalization;

namespace HaladeHighSchool.Api.Services;

/// <summary>
/// Outcome of a registration operation, with the reason a caller should translate into a status
/// code. Keeping the failure kind here means the controller decides HTTP shape and this service
/// decides the rules.
/// </summary>
public enum RegistrationFailure
{
    None = 0,

    /// <summary>The grade, section or request does not exist.</summary>
    NotFound,

    /// <summary>Submitted details are unusable: inactive class, duplicate application.</summary>
    Invalid,

    /// <summary>The request has already been decided, or the class filled up meanwhile.</summary>
    Conflict,
}

public record RegistrationResult<T> where T : class
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public RegistrationFailure Failure { get; init; }
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static RegistrationResult<T> Ok(T value) => new() { Succeeded = true, Value = value };

    public static RegistrationResult<T> Fail(RegistrationFailure failure, string title, params string[] errors) =>
        new() { Succeeded = false, Failure = failure, Title = title, Errors = errors };
}

public interface IRegistrationRequestService
{
    /// <summary>Records an application. Creates no login and no student record.</summary>
    Task<RegistrationResult<StudentRegistrationRequest>> SubmitAsync(
        RegisterStudentRequest request,
        CancellationToken cancellationToken = default,
        Guid? schoolId = null);

    /// <summary>Applications in one review state, oldest first.</summary>
    Task<IReadOnlyList<RegistrationRequestResponse>> ListAsync(
        string status,
        CancellationToken cancellationToken = default,
        Guid? schoolId = null);

    /// <summary>
    /// Issues credentials and provisions the account. The returned temporary password exists
    /// only in this response.
    /// </summary>
    Task<RegistrationResult<ApprovedRegistrationResponse>> ApproveAsync(
        int requestId,
        string? note,
        string reviewerUserId,
        CancellationToken cancellationToken = default,
        bool studentOnly = false,
        Guid? schoolId = null);

    /// <summary>Turns an application down. Nothing is provisioned.</summary>
    Task<RegistrationResult<RegistrationRequestResponse>> RejectAsync(
        int requestId,
        string? note,
        string reviewerUserId,
        CancellationToken cancellationToken = default,
        Guid? schoolId = null);
}

public class RegistrationRequestService : IRegistrationRequestService
{
    private readonly ApplicationDbContext _db;
    private readonly IAccountProvisioningService _provisioning;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegistrationRequestService> _logger;
    private readonly IProfilePhotoStorage _photoStorage;

    public RegistrationRequestService(
        ApplicationDbContext db,
        IAccountProvisioningService provisioning,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<RegistrationRequestService> logger,
        IProfilePhotoStorage photoStorage)
    {
        _db = db;
        _provisioning = provisioning;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
        _photoStorage = photoStorage;
    }

    public async Task<RegistrationResult<StudentRegistrationRequest>> SubmitAsync(
        RegisterStudentRequest request,
        CancellationToken cancellationToken = default,
        Guid? schoolId = null)
    {
        var targetSchoolId = schoolId ?? School.DefaultSchoolId;
        var targetSchool = await _db.Schools
            .SingleOrDefaultAsync(school => school.Id == targetSchoolId && school.IsActive, cancellationToken);
        if (targetSchool is null)
        {
            return RegistrationResult<StudentRegistrationRequest>.Fail(
                RegistrationFailure.NotFound,
                "School unavailable",
                "This school is not currently accepting registration requests.");
        }

        if (request.RequestedRole is not (Roles.Student or Roles.Teacher)
            || string.IsNullOrWhiteSpace(request.PhoneNumber)
            || request.Photo is null
            || (request.RequestedRole == Roles.Teacher
                && string.IsNullOrWhiteSpace(request.DepartmentSubject)))
        {
            return RegistrationResult<StudentRegistrationRequest>.Fail(
                RegistrationFailure.Invalid,
                "Incomplete registration",
                "A phone number and profile photo are required; teachers must also provide a department or subject.");
        }

        var contactEmail = request.Email.Trim();
        var requestedRole = request.RequestedRole;
        GradeLevel? gradeLevel = null;
        Section? section = null;

        if (requestedRole == "Student")
        {
            if (request.GradeLevelId is not int gradeLevelId || request.SectionId is not int sectionId)
            {
                return RegistrationResult<StudentRegistrationRequest>.Fail(
                    RegistrationFailure.Invalid,
                    "Class required",
                    "Students must choose a grade and section.");
            }

            gradeLevel = await _db.GradeLevels
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == gradeLevelId, cancellationToken);

            if (gradeLevel is null || !gradeLevel.IsActive)
            {
                return RegistrationResult<StudentRegistrationRequest>.Fail(
                    RegistrationFailure.Invalid,
                    "Unknown grade",
                    "That grade level does not exist or is not taking students.");
            }

            section = await _db.Sections
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sectionId, cancellationToken);

            if (section is null || !section.IsActive)
            {
                return RegistrationResult<StudentRegistrationRequest>.Fail(
                    RegistrationFailure.Invalid,
                    "Unknown section",
                    "That section does not exist or is not taking students.");
            }

            if (!await _db.SchoolGradeSections.AnyAsync(
                    link => link.SchoolId == targetSchoolId
                         && link.GradeLevelId == gradeLevelId
                         && link.SectionId == sectionId,
                    cancellationToken))
            {
                return RegistrationResult<StudentRegistrationRequest>.Fail(
                    RegistrationFailure.Invalid,
                    "Section is not available",
                    "That section is not available for the selected grade.");
            }
        }

        // A second application while one is outstanding is a duplicate, not a new applicant.
        var alreadyPending = await _db.StudentRegistrationRequests.AnyAsync(
            r => r.SchoolId == targetSchoolId
              && r.ContactEmail == contactEmail
              && r.Status == RegistrationRequestStatus.Pending,
            cancellationToken);

        if (alreadyPending)
        {
            return RegistrationResult<StudentRegistrationRequest>.Fail(
                RegistrationFailure.Conflict,
                "Already applied",
                "A registration for this email address is already waiting to be reviewed.");
        }

        /* An address that already signs in is not an applicant. Reported the same way as a
           duplicate application so this endpoint cannot be used to test which addresses exist. */
        if (await _userManager.FindByEmailAsync(contactEmail) is not null)
        {
            return RegistrationResult<StudentRegistrationRequest>.Fail(
                RegistrationFailure.Conflict,
                "Already applied",
                "A registration for this email address is already waiting to be reviewed.");
        }

        var photoUrl = await _photoStorage.SaveAsync(request.Photo, cancellationToken);

        var entity = new StudentRegistrationRequest
        {
            SchoolId = targetSchoolId,
            School = targetSchool,
            FullName = request.FullName.Trim(),
            ContactEmail = contactEmail,
            PhoneNumber = request.PhoneNumber.Trim(),
            DepartmentSubject = requestedRole == Roles.Teacher
                ? request.DepartmentSubject?.Trim()
                : null,
            PhotoUrl = photoUrl,
            RequestedRole = requestedRole,
            GradeLevelId = request.GradeLevelId,
            SectionId = request.SectionId,
            Status = RegistrationRequestStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
        };

        var admins = (await _userManager.GetUsersInRoleAsync(Roles.Admin))
            .Where(admin => admin.SchoolId == targetSchoolId)
            .ToList();

        _db.StudentRegistrationRequests.Add(entity);
        foreach (var admin in admins)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = admin.Id,
                Title = $"{requestedRole} registration pending review",
                Message = $"New {requestedRole.ToLowerInvariant()} registration pending review for {entity.FullName}",
                Type = "Registration",
                TargetUrl = "/admin/accounts",
                CreatedAt = DateTime.UtcNow,
            });
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        })
        {
            await _photoStorage.DeleteAsync(photoUrl);
            // The filtered unique index is the real guard; two simultaneous submissions land here.
            _logger.LogWarning(ex, "Duplicate registration request for {ContactEmail}", contactEmail);
            return RegistrationResult<StudentRegistrationRequest>.Fail(
                RegistrationFailure.Conflict,
                "Already applied",
                "A registration for this email address is already waiting to be reviewed.");
        }
        catch
        {
            await _photoStorage.DeleteAsync(photoUrl);
            throw;
        }

        entity.GradeLevel = gradeLevel;
        entity.Section = section;

        _logger.LogInformation(
            "Registration request {RequestId} submitted as {RequestedRole}",
            entity.Id,
            requestedRole);

        return RegistrationResult<StudentRegistrationRequest>.Ok(entity);
    }

    public async Task<IReadOnlyList<RegistrationRequestResponse>> ListAsync(
        string status,
        CancellationToken cancellationToken = default,
        Guid? schoolId = null)
    {
        var rows = await _db.StudentRegistrationRequests
            .AsNoTracking()
            .Where(r => r.Status == status && (schoolId == null || r.SchoolId == schoolId))
            .OrderBy(r => r.SubmittedAt)
            .Select(r => new
            {
                r.Id,
                r.SchoolId,
                r.FullName,
                r.ContactEmail,
                r.PhoneNumber,
                r.DepartmentSubject,
                r.RequestedRole,
                r.GradeLevelId,
                GradeLevelName = r.GradeLevel != null ? r.GradeLevel.Name : null,
                r.SectionId,
                SectionName = r.Section != null ? r.Section.Name : null,
                SectionCapacity = r.Section != null ? r.Section.Capacity : 0,
                r.Status,
                r.SubmittedAt,
                r.ReviewedAt,
                r.ReviewedByUserId,
                r.ReviewNote,
                r.CreatedStudentId,
                r.IssuedEmail,
                StudentIdNumber = r.CreatedStudent != null ? r.CreatedStudent.StudentIdNumber : null,
                // Enrolled students only: what the reviewer needs is the seats actually taken.
                Occupancy = _db.Students.Count(s =>
                    s.SectionId == r.SectionId
                    && s.GradeLevelId == r.GradeLevelId
                    && s.User != null
                    && s.User.SchoolId == r.SchoolId
                    && s.IsActive),
            })
            .ToListAsync(cancellationToken);

        // Resolved separately because ReviewedByUserId deliberately carries no foreign key.
        var reviewerIds = rows.Select(r => r.ReviewedByUserId).Where(id => id != null).Distinct().ToList();
        var reviewers = reviewerIds.Count == 0
            ? []
            : await _db.Users
                .AsNoTracking()
                .Where(u => reviewerIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName })
                .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        return rows
            .Select(r => new RegistrationRequestResponse
            {
                Id = r.Id,
                FullName = r.FullName,
                ContactEmail = r.ContactEmail,
                PhoneNumber = r.PhoneNumber,
                DepartmentSubject = r.DepartmentSubject,
                RequestedRole = r.RequestedRole,
                GradeLevelId = r.GradeLevelId,
                GradeLevelName = r.GradeLevelName,
                SectionId = r.SectionId,
                SectionName = r.SectionName,
                Status = r.Status,
                SubmittedAt = r.SubmittedAt,
                SectionCapacity = r.SectionCapacity,
                SectionOccupancy = r.Occupancy,
                ReviewedAt = r.ReviewedAt,
                ReviewedByName = r.ReviewedByUserId is not null && reviewers.TryGetValue(r.ReviewedByUserId, out var name)
                    ? name
                    : null,
                ReviewNote = r.ReviewNote,
                CreatedStudentId = r.CreatedStudentId,
                IssuedEmail = r.IssuedEmail,
                StudentIdNumber = r.StudentIdNumber,
            })
            .ToList();
    }

    public async Task<RegistrationResult<ApprovedRegistrationResponse>> ApproveAsync(
        int requestId,
        string? note,
        string reviewerUserId,
        CancellationToken cancellationToken = default,
        bool studentOnly = false,
        Guid? schoolId = null)
    {
        return await WithRequestLockAsync(
            requestId,
            token => ApproveUnderLockAsync(requestId, note, reviewerUserId, token, studentOnly, schoolId),
            cancellationToken);
    }

    private async Task<RegistrationResult<ApprovedRegistrationResponse>> ApproveUnderLockAsync(
        int requestId,
        string? note,
        string reviewerUserId,
        CancellationToken cancellationToken,
        bool studentOnly,
        Guid? schoolId)
    {
        var request = await _db.StudentRegistrationRequests
            .Include(r => r.GradeLevel)
            .Include(r => r.Section)
            .FirstOrDefaultAsync(
                r => r.Id == requestId && (schoolId == null || r.SchoolId == schoolId),
                cancellationToken);

        if (request is null)
        {
            return RegistrationResult<ApprovedRegistrationResponse>.Fail(
                RegistrationFailure.NotFound,
                "Request not found",
                $"No registration request with id {requestId} exists.");
        }

        if (studentOnly && request.RequestedRole != Roles.Student)
        {
            return RegistrationResult<ApprovedRegistrationResponse>.Fail(
                RegistrationFailure.Invalid,
                "Not a student application",
                "This approval endpoint only accepts student applications.");
        }

        if (request.Status != RegistrationRequestStatus.Pending)
        {
            return RegistrationResult<ApprovedRegistrationResponse>.Fail(
                RegistrationFailure.Conflict,
                "Already decided",
                $"This request was already {request.Status.ToLowerInvariant()}.");
        }

        var isTeacher = request.RequestedRole == "Teacher";
        if (string.IsNullOrWhiteSpace(request.PhotoUrl)
            || string.IsNullOrWhiteSpace(request.PhoneNumber)
            || (isTeacher && string.IsNullOrWhiteSpace(request.DepartmentSubject)))
        {
            return RegistrationResult<ApprovedRegistrationResponse>.Fail(
                RegistrationFailure.Invalid,
                "Incomplete application",
                "This request is missing a required phone number, department/subject, or profile photo.");
        }

        if (!isTeacher && (request.GradeLevelId is null || request.SectionId is null
            || request.Section is null || request.GradeLevel is null))
        {
            return RegistrationResult<ApprovedRegistrationResponse>.Fail(
                RegistrationFailure.Invalid,
                "Incomplete student application",
                "A student application must include a grade and section.");
        }

        if (!isTeacher && !await _db.SchoolGradeSections.AnyAsync(
                link => link.SchoolId == request.SchoolId
                     && link.GradeLevelId == request.GradeLevelId
                     && link.SectionId == request.SectionId,
                cancellationToken))
        {
            return RegistrationResult<ApprovedRegistrationResponse>.Fail(
                RegistrationFailure.Invalid,
                "Section is not available",
                "That section is no longer available for the selected grade.");
        }

        /* Checked before provisioning as well as inside it: the class may have filled up while
           the application sat in the queue, and a clear message beats a provisioning failure. */
        var occupancy = isTeacher
            ? 0
            : await _db.Students.CountAsync(
                s => s.User != null
                  && s.User.SchoolId == request.SchoolId
                  && s.SectionId == request.SectionId
                  && s.GradeLevelId == request.GradeLevelId
                  && s.IsActive,
                cancellationToken);

        if (!isTeacher && occupancy >= request.Section!.Capacity)
        {
            return RegistrationResult<ApprovedRegistrationResponse>.Fail(
                RegistrationFailure.Conflict,
                "Class is full",
                $"{request.GradeLevel!.Name} {request.Section.Name} is full "
              + $"({occupancy}/{request.Section.Capacity}). Free a seat or move the applicant first.");
        }

        /* Only this authenticated approval path creates an account. */
        var studentProvisioned = isTeacher
            ? null
            : await _provisioning.CreateStudentAsync(
                new ProvisionStudentRequest
                {
                    SchoolId = request.SchoolId,
                    FullName = request.FullName,
                    Email = null,
                    GradeLevelId = request.GradeLevelId!.Value,
                    SectionId = request.SectionId!.Value,
                    GuardianPhone = request.PhoneNumber,
                    PhotoUrl = request.PhotoUrl,
                },
                cancellationToken,
                async (student, issuedEmail, token) =>
                {
                    request.Status = RegistrationRequestStatus.Approved;
                    request.ReviewedAt = DateTime.UtcNow;
                    request.ReviewedByUserId = reviewerUserId;
                    request.ReviewNote = Trim(note);
                    request.CreatedStudentId = student.Id;
                    request.IssuedEmail = issuedEmail;
                    await _db.SaveChangesAsync(token);
                });
        var teacherProvisioned = isTeacher
            ? await _provisioning.CreateTeacherAsync(
                new ProvisionTeacherRequest
                {
                    SchoolId = request.SchoolId,
                    FullName = request.FullName,
                    Email = request.ContactEmail,
                    PhoneNumber = request.PhoneNumber,
                    Specialization = request.DepartmentSubject,
                    PhotoUrl = request.PhotoUrl,
                },
                cancellationToken,
                async (teacher, teacherEmail, token) =>
                {
                    request.Status = RegistrationRequestStatus.Approved;
                    request.ReviewedAt = DateTime.UtcNow;
                    request.ReviewedByUserId = reviewerUserId;
                    request.ReviewNote = Trim(note);
                    request.CreatedStudentId = null;
                    request.IssuedEmail = teacherEmail;
                    await _db.SaveChangesAsync(token);
                })
            : null;

        if ((studentProvisioned is null || !studentProvisioned.Succeeded || studentProvisioned.Entity is null)
            && (teacherProvisioned is null || !teacherProvisioned.Succeeded || teacherProvisioned.Entity is null))
        {
            return RegistrationResult<ApprovedRegistrationResponse>.Fail(
                RegistrationFailure.Invalid,
                "Could not create the account",
                [.. (studentProvisioned?.Errors ?? teacherProvisioned?.Errors ?? [])]);
        }

        var student = studentProvisioned?.Entity;
        var teacher = teacherProvisioned?.Entity;
        var userId = student?.UserId ?? teacher?.UserId;
        var issuedEmail = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        _logger.LogInformation(
            "Registration request {RequestId} approved as {RequestedRole} by {Reviewer}",
            request.Id,
            request.RequestedRole,
            reviewerUserId);

        var temporaryPassword =
            studentProvisioned?.TemporaryPassword ?? teacherProvisioned?.TemporaryPassword ?? string.Empty;

        var emailSent = false;
        try
        {
            if (isTeacher)
            {
                await _emailService.SendTeacherApprovalEmailAsync(
                    request.ContactEmail,
                    request.FullName,
                    temporaryPassword,
                    cancellationToken);
            }
            else
            {
                await _emailService.SendStudentApprovalEmailAsync(
                    request.ContactEmail,
                    request.FullName,
                    issuedEmail,
                    temporaryPassword,
                    cancellationToken);
            }

            emailSent = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                ex,
                "Approval email failed for registration request {RequestId} ({Role})",
                request.Id,
                request.RequestedRole);
        }

        var approvedResponse = new ApprovedRegistrationResponse
        {
            RequestId = request.Id,
            RequestedRole = request.RequestedRole,
            StudentId = student?.Id,
            TeacherId = teacher?.Id,
            FullName = request.FullName,
            StudentIdNumber = student?.StudentIdNumber,
            EmployeeId = teacher?.EmployeeId,
            IssuedEmail = issuedEmail,
            ContactEmail = request.ContactEmail,
            // Provisioning returns only a generated password; Identity stores its hash.
            TemporaryPassword = temporaryPassword,
            EmailSent = emailSent,
            GradeLevelName = request.GradeLevel?.Name,
            SectionName = request.Section?.Name,
            ApprovedAt = request.ReviewedAt
                ?? throw new InvalidOperationException("Approved registration is missing its review timestamp."),
            Message = emailSent
                ? $"Credentials were emailed to {request.ContactEmail}. The temporary password is shown once "
                    + "and cannot be retrieved again; the new account holder should change it after signing in."
                : $"The account was approved, but credentials could not be emailed to {request.ContactEmail}. "
                    + "Share the credentials shown here securely. The temporary password cannot be retrieved again.",
        };

        return RegistrationResult<ApprovedRegistrationResponse>.Ok(approvedResponse);
    }

    public async Task<RegistrationResult<RegistrationRequestResponse>> RejectAsync(
        int requestId,
        string? note,
        string reviewerUserId,
        CancellationToken cancellationToken = default,
        Guid? schoolId = null)
    {
        return await WithRequestLockAsync(
            requestId,
            token => RejectUnderLockAsync(requestId, note, reviewerUserId, token, schoolId),
            cancellationToken);
    }

    private async Task<RegistrationResult<RegistrationRequestResponse>> RejectUnderLockAsync(
        int requestId,
        string? note,
        string reviewerUserId,
        CancellationToken cancellationToken,
        Guid? schoolId)
    {
        var request = await _db.StudentRegistrationRequests
            .FirstOrDefaultAsync(
                r => r.Id == requestId && (schoolId == null || r.SchoolId == schoolId),
                cancellationToken);

        if (request is null)
        {
            return RegistrationResult<RegistrationRequestResponse>.Fail(
                RegistrationFailure.NotFound,
                "Request not found",
                $"No registration request with id {requestId} exists.");
        }

        if (request.Status != RegistrationRequestStatus.Pending)
        {
            return RegistrationResult<RegistrationRequestResponse>.Fail(
                RegistrationFailure.Conflict,
                "Already decided",
                $"This request was already {request.Status.ToLowerInvariant()}.");
        }

        request.Status = RegistrationRequestStatus.Rejected;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedByUserId = reviewerUserId;
        request.ReviewNote = Trim(note);

        var rejectedPhotoUrl = request.PhotoUrl;
        request.PhotoUrl = null;
        await _db.SaveChangesAsync(cancellationToken);
        await _photoStorage.DeleteAsync(rejectedPhotoUrl);

        _logger.LogInformation(
            "Registration request {RequestId} rejected by {Reviewer}",
            request.Id,
            reviewerUserId);

        var rejected = await ListAsync(RegistrationRequestStatus.Rejected, cancellationToken, schoolId);

        return RegistrationResult<RegistrationRequestResponse>.Ok(
            rejected.First(r => r.Id == request.Id));
    }

    private async Task<RegistrationResult<T>> WithRequestLockAsync<T>(
        int requestId,
        Func<CancellationToken, Task<RegistrationResult<T>>> action,
        CancellationToken cancellationToken)
        where T : class
    {
        // Session ownership lets the lock span the provisioning transaction on this connection.
        await _db.Database.OpenConnectionAsync(cancellationToken);
        var lockAcquired = false;

        try
        {
            var resourceName = $"StudentRegistrationApproval:{requestId}";
            var timeout = Stopwatch.StartNew();
            while (!lockAcquired && timeout.Elapsed < TimeSpan.FromSeconds(10))
            {
                await using var command = _db.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT pg_try_advisory_lock(hashtextextended(@resource, 0));";
                var resource = command.CreateParameter();
                resource.ParameterName = "@resource";
                resource.DbType = DbType.String;
                resource.Value = resourceName;
                command.Parameters.Add(resource);

                lockAcquired = Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken));
                if (!lockAcquired)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
                }
            }

            if (!lockAcquired)
            {
                return RegistrationResult<T>.Fail(
                    RegistrationFailure.Conflict,
                    "Review is already in progress",
                    "This application is currently being reviewed. Refresh the queue and try again.");
            }

            return await action(cancellationToken);
        }
        finally
        {
            if (lockAcquired)
            {
                try
                {
                    await using var release = _db.Database.GetDbConnection().CreateCommand();
                    release.CommandText = "SELECT pg_advisory_unlock(hashtextextended(@resource, 0));";
                    var resource = release.CreateParameter();
                    resource.ParameterName = "@resource";
                    resource.DbType = DbType.String;
                    resource.Value = $"StudentRegistrationApproval:{requestId}";
                    release.Parameters.Add(resource);
                    await release.ExecuteScalarAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not release registration review lock for request {RequestId}", requestId);
                }
            }

            await _db.Database.CloseConnectionAsync();
        }
    }

    private static string? Trim(string? note) =>
        note?.Trim() is { Length: > 0 } trimmed ? trimmed : null;
}
