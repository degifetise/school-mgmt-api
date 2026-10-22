using System.Data;
using System.Security.Claims;
using HaladeHighSchool.Api.Configuration;
using HaladeHighSchool.Api.Data;
using HaladeHighSchool.Api.DTOs;
using HaladeHighSchool.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HaladeHighSchool.Api.Controllers;

/// <summary>Tenant onboarding for school administrators.</summary>
[ApiController]
[Route("api/admin/schools")]
[Produces("application/json")]
public sealed class AdminSchoolsController : PortalControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminSchoolsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>Creates a tenant and its initial administrator.</summary>
    [HttpPost("/api/superadmin/schools")]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType<SchoolResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<SchoolResponse>> CreateSchool(
        CreateSchoolRequest request,
        CancellationToken cancellationToken)
        => CreateSchoolAsync(request, isSuperAdmin: true, cancellationToken);

    /// <summary>Creates a tenant through the legacy school administrator endpoint.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<SchoolResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<SchoolResponse>> CreateSchoolForAdmin(
        CreateSchoolRequest request,
        CancellationToken cancellationToken)
        => CreateSchoolAsync(request, isSuperAdmin: false, cancellationToken);

    private async Task<ActionResult<SchoolResponse>> CreateSchoolAsync(
        CreateSchoolRequest request,
        bool isSuperAdmin,
        CancellationToken cancellationToken)
    {
        if (await _userManager.FindByEmailAsync(request.AdminEmail.Trim()) is not null)
        {
            return ConflictProblem("Email already registered", "The initial administrator email is already in use.");
        }

        var normalizedCode = request.SchoolCode.Trim().ToUpperInvariant();
        if (await _db.Schools.AnyAsync(school => school.Code == normalizedCode, cancellationToken))
        {
            return ConflictProblem("School code already registered", "Choose a unique school code.");
        }

        if (!await _roleManager.RoleExistsAsync(Roles.Admin))
        {
            return ConflictProblem("Admin role is unavailable", "The Admin role has not been initialized.");
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            _db.ChangeTracker.Clear();
            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var schoolId = Guid.NewGuid();
            var grades = await _db.GradeLevels.AsNoTracking()
                .OrderBy(grade => grade.Level)
                .Select(grade => new { grade.Id, grade.Name })
                .ToListAsync(cancellationToken);
            var school = new School
            {
                Id = schoolId,
                Name = request.SchoolName.Trim(),
                Code = normalizedCode,
                ContactEmail = request.ContactEmail.Trim(),
                ContactPhone = request.ContactPhone.Trim(),
                Address = request.Address.Trim(),
                SupportedGrades = grades.Select(grade => grade.Name).ToArray(),
                CreatedAt = DateTime.UtcNow
            };
            _db.Schools.Add(school);

            var user = new ApplicationUser
            {
                SchoolId = schoolId,
                UserName = request.AdminEmail.Trim(),
                Email = request.AdminEmail.Trim(),
                EmailConfirmed = true,
                FullName = request.AdminFullName.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var createResult = await _userManager.CreateAsync(user, request.AdminTemporaryPassword);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (ActionResult<SchoolResponse>)BadRequest(new ValidationProblemDetails
                {
                    Title = "Initial administrator was not created",
                    Status = StatusCodes.Status400BadRequest,
                    Errors = { ["admin"] = createResult.Errors.Select(error => error.Description).ToArray() }
                });
            }

            var roleResult = await _userManager.AddToRoleAsync(user, Roles.Admin);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ConflictProblem(
                    "Initial administrator role could not be assigned",
                    string.Join("; ", roleResult.Errors.Select(error => error.Description)));
            }

            var sectionLinks = await _db.GradeLevelSections.AsNoTracking()
                .Select(link => new SchoolGradeSection
                {
                    SchoolId = schoolId,
                    GradeLevelId = link.GradeLevelId,
                    SectionId = link.SectionId
                })
                .ToListAsync(cancellationToken);
            _db.SchoolGradeSections.AddRange(sectionLinks);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var response = new SchoolResponse(
                school.Id,
                school.Name,
                school.Code,
                school.ContactEmail,
                school.ContactPhone,
                school.Address,
                user.FullName,
                user.Email!);
            return isSuperAdmin
                ? (ActionResult<SchoolResponse>)CreatedAtAction(
                    nameof(GetCreatedSchool),
                    new { schoolId = school.Id },
                    response)
                : CreatedAtAction(nameof(GetSchool), new { schoolId = school.Id }, response);
        });
    }

    /// <summary>Returns a tenant created through the SuperAdmin onboarding endpoint.</summary>
    [HttpGet("/api/superadmin/schools/{schoolId:guid}")]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType<SchoolResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SchoolResponse>> GetCreatedSchool(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        var school = await _db.Schools.AsNoTracking()
            .Where(item => item.Id == schoolId)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.Code,
                item.ContactEmail,
                item.ContactPhone,
                item.Address,
                Admin = item.Users
                    .Where(user => user.Email != null)
                    .OrderBy(user => user.CreatedAt)
                    .Select(user => new { user.FullName, user.Email })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return school is null || school.Admin is null
            ? NotFoundProblem("The school was not found.")
            : Ok(new SchoolResponse(
                school.Id,
                school.Name,
                school.Code,
                school.ContactEmail,
                school.ContactPhone,
                school.Address,
                school.Admin.FullName,
                school.Admin.Email!));
    }

    /// <summary>Returns the authenticated administrator's school identity.</summary>
    [HttpGet("{schoolId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<SchoolResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SchoolResponse>> GetSchool(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        var school = await _db.Schools.AsNoTracking()
            .Where(item => item.Id == schoolId && item.Id == CurrentSchoolId)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.Code,
                item.ContactEmail,
                item.ContactPhone,
                item.Address
            })
            .FirstOrDefaultAsync(cancellationToken);

        return school is null
            ? NotFoundProblem("The school was not found.")
            : Ok(new SchoolResponse(
                school.Id,
                school.Name,
                school.Code,
                school.ContactEmail,
                school.ContactPhone,
                school.Address,
                User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                User.FindFirstValue(ClaimTypes.Email) ?? string.Empty));
    }
}
