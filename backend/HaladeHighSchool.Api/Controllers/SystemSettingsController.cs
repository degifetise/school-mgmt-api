using HaladeHighSchool.Api.Configuration;
using HaladeHighSchool.Api.Data;
using HaladeHighSchool.Api.DTOs;
using HaladeHighSchool.Api.Models;
using HaladeHighSchool.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HaladeHighSchool.Api.Controllers;

/// <summary>
/// Global configuration held in the SystemSettings table and tenant-specific school branding.
/// </summary>
[ApiController]
[Route("api/system-settings")]
[Authorize]
[Produces("application/json")]
public class SystemSettingsController : PortalControllerBase
{
    private readonly ISystemSettingsService _settings;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SystemSettingsController> _logger;

    public SystemSettingsController(
        ISystemSettingsService settings,
        ApplicationDbContext db,
        ILogger<SystemSettingsController> logger)
    {
        _settings = settings;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// The school's public identity. Deliberately anonymous so the login screen and the
    /// dashboard header can show the school name before a token exists. Only non-sensitive
    /// values are returned; the pass mark and upload limit stay behind the admin endpoint.
    /// </summary>
    [HttpGet("school-info")]
    [AllowAnonymous]
    [ProducesResponseType<SchoolInfoResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SchoolInfoResponse>> GetSchoolInfo(CancellationToken cancellationToken)
    {
        var info = await _settings.GetSchoolInfoAsync(cancellationToken);
        var schoolId = User.GetSchoolId() ?? School.DefaultSchoolId;
        var school = await _db.Schools
            .AsNoTracking()
            .Where(tenant => tenant.Id == schoolId)
            .Select(tenant => new
            {
                tenant.Name,
                tenant.HeroSubtitle,
                tenant.HeroDescription,
                tenant.PrimaryColor,
                tenant.AcademicYear
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (school is not null)
        {
            info = info with
            {
                SchoolName = school.Name,
                HeroSubtitle = school.HeroSubtitle,
                HeroDescription = school.HeroDescription,
                PrimaryColor = school.PrimaryColor,
                AcademicYear = school.AcademicYear
            };
        }

        return Ok(info);
    }

    /// <summary>All current system settings.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType<SystemSettingsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemSettingsResponse>> GetSettings(
        CancellationToken cancellationToken)
    {
        return Ok(await _settings.GetSettingsAsync(cancellationToken));
    }

    /// <summary>
    /// Updates the writable settings and returns the stored result. Changes apply to the next
    /// request without a restart, because settings are read from the database each time.
    /// </summary>
    [HttpPut]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType<SystemSettingsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SystemSettingsResponse>> UpdateSettings(
        UpdateSystemSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _settings.UpdateSettingsAsync(request, cancellationToken);

        _logger.LogInformation(
            "System settings updated by {User}: academic year {AcademicYear}, " +
            "pass mark {PassMark}, upload limit {UploadMb} MB, self registration {SelfRegistration}",
            User.Identity?.Name,
            updated.AcademicYear,
            updated.PassMarkPercentage,
            updated.MaxUploadSizeMb,
            updated.AllowSelfRegistration);

        return Ok(updated);
    }

    /// <summary>Gets branding and academic-year settings for the signed-in administrator's school.</summary>
    [HttpGet("/api/systemsettings/school-settings")]
    [Authorize(Roles = Roles.AdminOrSchoolAdmin + "," + Roles.SuperAdmin)]
    [ProducesResponseType<SchoolBrandingSettingsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SchoolBrandingSettingsResponse>> GetSchoolSettings(
        CancellationToken cancellationToken)
    {
        var schoolId = User.GetSchoolId();
        if (schoolId is null)
        {
            return Forbid();
        }

        var settings = await _db.Schools
            .AsNoTracking()
            .Where(school => school.Id == schoolId.Value)
            .Select(school => new SchoolBrandingSettingsResponse(
                school.HeroSubtitle,
                school.HeroDescription,
                school.PrimaryColor,
                school.AcademicYear))
            .SingleOrDefaultAsync(cancellationToken);

        return settings is null ? NotFound() : Ok(settings);
    }

    /// <summary>Updates branding and academic-year settings for the signed-in administrator's school.</summary>
    [HttpPut("/api/systemsettings/school-settings")]
    [Authorize(Roles = Roles.AdminOrSchoolAdmin + "," + Roles.SuperAdmin)]
    [ProducesResponseType<SchoolBrandingSettingsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SchoolBrandingSettingsResponse>> UpdateSchoolSettings(
        UpdateSchoolBrandingSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var schoolId = User.GetSchoolId();
        if (schoolId is null)
        {
            return Forbid();
        }

        var school = await _db.Schools
            .SingleOrDefaultAsync(tenant => tenant.Id == schoolId.Value, cancellationToken);
        if (school is null)
        {
            return NotFound();
        }

        school.HeroSubtitle = request.HeroSubtitle.Trim();
        school.HeroDescription = request.HeroDescription.Trim();
        school.PrimaryColor = request.PrimaryColor.ToUpperInvariant();
        school.AcademicYear = request.AcademicYear;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "School branding settings updated for {SchoolId} by {User}",
            school.Id,
            User.Identity?.Name);

        return Ok(new SchoolBrandingSettingsResponse(
            school.HeroSubtitle,
            school.HeroDescription,
            school.PrimaryColor,
            school.AcademicYear));
    }
}
