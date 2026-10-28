using System.Security.Claims;
using HaladeHighSchool.Api.Configuration;
using HaladeHighSchool.Api.Data;
using HaladeHighSchool.Api.DTOs;
using HaladeHighSchool.Api.Models;
using HaladeHighSchool.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HaladeHighSchool.Api.Controllers;

[ApiController]
[Route("api/conduct")]
[Produces("application/json")]
public sealed class ConductController : PortalControllerBase
{
    private const string DisciplinaryFilter = "Disciplinary";
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ConductController> _logger;

    public ConductController(ApplicationDbContext db, ILogger<ConductController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Records a conduct entry for a student in the caller's school or teaching roster.</summary>
    [HttpPost("logs")]
    [Authorize(Roles = Roles.AdminOrSchoolAdminOrTeacher)]
    [ProducesResponseType<StudentConductLogResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentConductLogResponse>> CreateLog(
        CreateStudentConductLogRequest request,
        CancellationToken cancellationToken)
    {
        if (!StudentConductTypes.All.Contains(request.Type, StringComparer.Ordinal))
        {
            return BadRequestProblem("Invalid conduct type", "Choose a supported conduct category.");
        }

        var student = await _db.Students
            .Include(candidate => candidate.User)
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.StudentId
                    && candidate.User != null
                    && candidate.User.SchoolId == CurrentSchoolId,
                cancellationToken);
        if (student is null)
        {
            return NotFoundProblem("The student was not found.");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SchoolAdmin);
        var recordedByRole = isAdmin ? Roles.Admin : Roles.Teacher;
        if (!isAdmin && !await CanTeacherAccessStudentAsync(userId, request.StudentId, cancellationToken))
        {
            return ForbiddenProblem("You can only log conduct for students in your assigned classes.");
        }

        var actor = await _db.Users
            .AsNoTracking()
            .Where(user => user.Id == userId && user.SchoolId == CurrentSchoolId && user.IsActive)
            .Select(user => new { user.FullName })
            .FirstOrDefaultAsync(cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;
        var log = new StudentConductLog
        {
            StudentId = student.Id,
            RecordedByUserId = userId,
            RecordedByName = actor.FullName,
            RecordedByRole = recordedByRole,
            Type = request.Type,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            IncidentDate = request.IncidentDate.UtcDateTime,
            IsVisibleToStudent = request.IsVisibleToStudent,
            IsVisibleToParent = request.IsVisibleToParent,
            CreatedAt = now,
        };
        _db.StudentConductLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "{Role} {UserId} recorded conduct log {ConductLogId} for student {StudentId}",
            recordedByRole,
            userId,
            log.Id,
            log.StudentId);

        return CreatedAtAction(
            nameof(GetStudentLogs),
            new { studentId = log.StudentId },
            ToResponse(log));
    }

    /// <summary>Returns conduct history, applying student visibility and class access rules.</summary>
    [HttpGet("student/{studentId:int}")]
    [Authorize(Roles = Roles.AdminOrSchoolAdminOrTeacher + "," + Roles.Student)]
    [ProducesResponseType<IReadOnlyList<StudentConductLogResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<StudentConductLogResponse>>> GetStudentLogs(
        int studentId,
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        var student = await _db.Students
            .AsNoTracking()
            .Where(candidate => candidate.Id == studentId
                && candidate.User != null
                && candidate.User.SchoolId == CurrentSchoolId)
            .Select(candidate => new { candidate.Id, candidate.UserId })
            .FirstOrDefaultAsync(cancellationToken);
        if (student is null)
        {
            return NotFoundProblem("The student was not found.");
        }

        var isStudent = User.IsInRole(Roles.Student);
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (isStudent)
        {
            if (User.GetStudentId() != studentId || student.UserId != currentUserId)
            {
                return ForbiddenProblem("Students can only view their own conduct records.");
            }
        }
        else if (User.IsInRole(Roles.Teacher)
            && (currentUserId is null
                || !await CanTeacherAccessStudentAsync(currentUserId, studentId, cancellationToken)))
        {
            return ForbiddenProblem("You can only view students in your assigned classes.");
        }

        if (!string.IsNullOrWhiteSpace(type)
            && !string.Equals(type, "All", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(type, DisciplinaryFilter, StringComparison.OrdinalIgnoreCase)
            && !StudentConductTypes.All.Contains(type, StringComparer.Ordinal))
        {
            return BadRequestProblem("Invalid conduct filter", "Choose All, Merit, or Disciplinary.");
        }

        var logs = _db.StudentConductLogs
            .AsNoTracking()
            .Where(log => log.StudentId == studentId);
        if (isStudent)
        {
            logs = logs.Where(log => log.IsVisibleToStudent);
        }

        if (string.Equals(type, DisciplinaryFilter, StringComparison.OrdinalIgnoreCase))
        {
            logs = logs.Where(log =>
                log.Type == StudentConductTypes.Demerit
                || log.Type == StudentConductTypes.DisciplinaryNote);
        }
        else if (!string.IsNullOrWhiteSpace(type)
            && !string.Equals(type, "All", StringComparison.OrdinalIgnoreCase))
        {
            logs = logs.Where(log => log.Type == type);
        }

        var rows = await logs
            .OrderByDescending(log => log.IncidentDate)
            .ThenByDescending(log => log.CreatedAt)
            .ToListAsync(cancellationToken);
        return Ok(rows.Select(ToResponse).ToArray());
    }

    /// <summary>Returns the signed-in student's own visible conduct history and standing.</summary>
    [HttpGet("my-status")]
    [Authorize(Roles = Roles.Student)]
    [ProducesResponseType<StudentConductStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentConductStatusResponse>> GetMyStatus(
        CancellationToken cancellationToken)
    {
        var studentId = User.GetStudentId();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (studentId is null || userId is null)
        {
            return NotFoundProblem("The student profile was not found.");
        }

        var isLinkedStudent = await _db.Students
            .AsNoTracking()
            .AnyAsync(student => student.Id == studentId
                && student.IsActive
                && student.UserId == userId
                && student.User != null
                && student.User.SchoolId == CurrentSchoolId,
                cancellationToken);
        if (!isLinkedStudent)
        {
            return NotFoundProblem("The student profile was not found.");
        }

        var logs = await _db.StudentConductLogs
            .AsNoTracking()
            .Where(log => log.StudentId == studentId && log.IsVisibleToStudent)
            .OrderByDescending(log => log.IncidentDate)
            .ThenByDescending(log => log.CreatedAt)
            .ToListAsync(cancellationToken);

        var standing = logs.Any(IsDisciplinary)
            ? "Active Disciplinary Notes"
            : logs.Any(log => log.Type == StudentConductTypes.Merit)
                ? "Active Merits"
                : "Good Standing";
        return Ok(new StudentConductStatusResponse(standing, logs.Select(ToResponse).ToArray()));
    }

    /// <summary>Removes a conduct record for an administrator or the teacher who authored it.</summary>
    [HttpDelete("logs/{logId:int}")]
    [Authorize(Roles = Roles.AdminOrSchoolAdminOrTeacher)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLog(int logId, CancellationToken cancellationToken)
    {
        var log = await _db.StudentConductLogs
            .Include(entry => entry.Student)
                .ThenInclude(student => student!.User)
            .FirstOrDefaultAsync(
                entry => entry.Id == logId
                    && entry.Student != null
                    && entry.Student.User != null
                    && entry.Student.User.SchoolId == CurrentSchoolId,
                cancellationToken);
        if (log is null)
        {
            return NotFoundProblem("The conduct record was not found.");
        }

        var isAdmin = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SchoolAdmin);
        if (!isAdmin && log.RecordedByUserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            return ForbiddenProblem("Teachers can only remove conduct records they authored.");
        }

        _db.StudentConductLogs.Remove(log);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Task<bool> CanTeacherAccessStudentAsync(
        string userId,
        int studentId,
        CancellationToken cancellationToken) =>
        _db.TeacherSubjects
            .AsNoTracking()
            .AnyAsync(assignment =>
                assignment.IsActive
                && assignment.Teacher != null
                && assignment.Teacher.IsActive
                && assignment.Teacher.UserId == userId
                && assignment.Teacher.User != null
                && assignment.Teacher.User.SchoolId == CurrentSchoolId
                && _db.Students.Any(student =>
                    student.Id == studentId
                    && student.IsActive
                    && student.User != null
                    && student.User.SchoolId == CurrentSchoolId
                    && student.SectionId == assignment.SectionId),
                cancellationToken);

    private static bool IsDisciplinary(StudentConductLog log) =>
        log.Type == StudentConductTypes.Demerit
        || log.Type == StudentConductTypes.DisciplinaryNote;

    private static StudentConductLogResponse ToResponse(StudentConductLog log) =>
        new(
            log.Id,
            log.StudentId,
            log.RecordedByName,
            log.RecordedByRole,
            log.Type,
            log.Title,
            log.Description,
            new DateTimeOffset(DateTime.SpecifyKind(log.IncidentDate, DateTimeKind.Utc)),
            log.IsVisibleToStudent,
            log.IsVisibleToParent,
            new DateTimeOffset(DateTime.SpecifyKind(log.CreatedAt, DateTimeKind.Utc)));
}
