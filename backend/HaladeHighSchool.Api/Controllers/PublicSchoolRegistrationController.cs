using HaladeHighSchool.Api.DTOs;
using HaladeHighSchool.Api.Models;
using HaladeHighSchool.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HaladeHighSchool.Api.Data;

namespace HaladeHighSchool.Api.Controllers;

/// <summary>Public school lookup and registration endpoints addressed by tenant slug.</summary>
[ApiController]
[Route("api/public/schools/slug/{slug}")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class PublicSchoolRegistrationController : PortalControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IRegistrationRequestService _registrations;
    private readonly ISystemSettingsService _settings;

    public PublicSchoolRegistrationController(
        ApplicationDbContext db,
        IRegistrationRequestService registrations,
        ISystemSettingsService settings)
    {
        _db = db;
        _registrations = registrations;
        _settings = settings;
    }

    /// <summary>Resolves a public registration page to its school and configured classes.</summary>
    [HttpGet]
    [ProducesResponseType<PublicSchoolRegistrationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicSchoolRegistrationResponse>> GetBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        var school = await _db.Schools.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Slug == slug.ToLowerInvariant() && item.IsActive,
                cancellationToken);
        if (school is null)
        {
            return NotFoundProblem("This school registration page could not be found.");
        }

        var rows = await _db.SchoolGradeSections.AsNoTracking()
            .Where(link => link.SchoolId == school.Id
                && link.GradeLevel != null
                && link.GradeLevel.IsActive
                && link.Section != null
                && link.Section.IsActive
                && school.SupportedGrades.Contains(link.GradeLevel.Name))
            .OrderBy(link => link.GradeLevel!.Level)
            .ThenBy(link => link.Section!.Name)
            .Select(link => new
            {
                GradeId = link.GradeLevelId,
                GradeName = link.GradeLevel!.Name,
                GradeLevel = link.GradeLevel.Level,
                SectionId = link.SectionId,
                SectionName = link.Section!.Name
            })
            .ToListAsync(cancellationToken);
        var grades = rows.GroupBy(row => new { row.GradeId, row.GradeName, row.GradeLevel })
            .Select(group => new SchoolRegistrationGradeResponse(
                group.Key.GradeId,
                group.Key.GradeName,
                group.Key.GradeLevel,
                group.Select(row => new SchoolRegistrationSectionResponse(row.SectionId, row.SectionName))
                    .ToArray()))
            .ToArray();

        return Ok(new PublicSchoolRegistrationResponse(
            school.Name,
            school.Slug,
            school.AllowPublicRegistration &&
                await _settings.IsSelfRegistrationAllowedAsync(cancellationToken),
            grades));
    }

<<<<<<< HEAD
    /// <summary>Submits a student or teacher application for the school identified by the URL slug.</summary>
    [HttpPost("register")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit((2 * 1024 * 1024) + (64 * 1024))]
=======
    /// <summary>Submits a student application for the school identified by the URL slug.</summary>
    [HttpPost("register")]
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
    [ProducesResponseType<RegistrationSubmittedResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        string slug,
<<<<<<< HEAD
        [FromForm] RegisterStudentRequest request,
=======
        RegisterStudentRequest request,
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
        CancellationToken cancellationToken)
    {
        var schoolId = await _db.Schools.AsNoTracking()
            .Where(item => item.Slug == slug.ToLowerInvariant() && item.IsActive)
            .Select(item => new { item.Id, item.AllowPublicRegistration })
            .SingleOrDefaultAsync(cancellationToken);
        if (schoolId is null)
        {
            return NotFoundProblem("This school registration page could not be found.");
        }

        if (!schoolId.AllowPublicRegistration)
        {
            return PublicRegistrationClosed();
        }

<<<<<<< HEAD
=======
        if (request.RequestedRole != "Student")
        {
            return BadRequestProblem("Student registration only", "Only student applications are accepted on this route.");
        }

>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
        if (!await _settings.IsSelfRegistrationAllowedAsync(cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Registration closed",
                Detail = "Self-registration is disabled. Ask the school administrator to create your account.",
                Status = StatusCodes.Status403Forbidden
            });
        }

<<<<<<< HEAD
        RegistrationResult<StudentRegistrationRequest> result;
        try
        {
            result = await _registrations.SubmitAsync(request, cancellationToken, schoolId.Id);
        }
        catch (InvalidProfilePhotoException exception)
        {
            return BadRequestProblem("Invalid profile photo", exception.Message);
        }
=======
        var result = await _registrations.SubmitAsync(request, cancellationToken, schoolId.Id);
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
        if (result.Succeeded && result.Value is { } submitted)
        {
            return Accepted(new RegistrationSubmittedResponse
            {
                RequestId = submitted.Id,
                Status = submitted.Status,
                RequestedRole = submitted.RequestedRole,
                FullName = submitted.FullName,
                ContactEmail = submitted.ContactEmail,
                GradeLevelName = submitted.GradeLevel?.Name ?? string.Empty,
                SectionName = submitted.Section?.Name ?? string.Empty,
                SubmittedAt = submitted.SubmittedAt,
                Message = $"Your registration has been received by {submitted.School?.Name ?? "the school"} and is waiting for review."
            });
        }

        return result.Failure switch
        {
            RegistrationFailure.NotFound => NotFoundProblem(string.Join(" ", result.Errors)),
            RegistrationFailure.Conflict => Conflict(new ProblemDetails
            {
                Title = result.Title,
                Detail = string.Join(" ", result.Errors),
                Status = StatusCodes.Status409Conflict
            }),
            _ => BadRequest(new ValidationProblemDetails
            {
                Title = result.Title,
                Status = StatusCodes.Status400BadRequest,
                Errors = { ["registration"] = [.. result.Errors] }
            })
        };
    }

    private ObjectResult PublicRegistrationClosed() =>
        StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
        {
            Title = "Registration closed",
            Detail = "Public registration is currently closed for this school.",
            Status = StatusCodes.Status403Forbidden
        });
}
