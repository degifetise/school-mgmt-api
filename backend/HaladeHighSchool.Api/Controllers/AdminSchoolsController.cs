using System.Data;
using System.Security.Claims;
using System.Text.RegularExpressions;
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

    /// <summary>Lists school tenants and their assigned administrators.</summary>
    [HttpGet("/api/superadmin/schools")]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType<IReadOnlyList<SuperAdminSchoolResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SuperAdminSchoolResponse>>> ListSchools(
        CancellationToken cancellationToken)
    {
        var schools = await _db.Schools.AsNoTracking()
            .OrderBy(school => school.Name)
            .ToListAsync(cancellationToken);
        var administrators = await (
            from user in _db.Users.AsNoTracking()
            join userRole in _db.UserRoles on user.Id equals userRole.UserId
            join role in _db.Roles on userRole.RoleId equals role.Id
            where role.Name == Roles.Admin || role.Name == Roles.SchoolAdmin
            select new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.SchoolId,
                RoleName = role.Name
            })
            .ToListAsync(cancellationToken);
        var adminBySchool = administrators
            .GroupBy(admin => admin.SchoolId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var admin = group
                        .OrderByDescending(item => item.RoleName == Roles.SchoolAdmin)
                        .ThenBy(item => item.FullName)
                        .First();
                    return admin.Email is null
                        ? null
                        : new SchoolAdminResponse(admin.Id, admin.FullName, admin.Email);
                });

        return Ok(schools.Select(school => new SuperAdminSchoolResponse(
                school.Id,
                school.Name,
                new DateTimeOffset(DateTime.SpecifyKind(school.CreatedAt, DateTimeKind.Utc)),
                school.IsActive,
                adminBySchool.GetValueOrDefault(school.Id)))
            .ToList());
    }

    /// <summary>Toggles whether users assigned to a school can access the system.</summary>
    [HttpPost("/api/superadmin/schools/{schoolId:guid}/toggle-status")]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType<SchoolStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SchoolStatusResponse>> ToggleSchoolStatus(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        var affectedRows = await _db.Schools
            .Where(school => school.Id == schoolId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(school => school.IsActive, school => !school.IsActive),
                cancellationToken);
        if (affectedRows == 0)
        {
            return NotFoundProblem("The school was not found.");
        }

        var isActive = await _db.Schools.AsNoTracking()
            .Where(school => school.Id == schoolId)
            .Select(school => school.IsActive)
            .SingleAsync(cancellationToken);
        return Ok(new SchoolStatusResponse(schoolId, isActive));
    }

    /// <summary>Assigns an existing account to a school as its administrator.</summary>
    [HttpPost("/api/superadmin/schools/{schoolId:guid}/admin")]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType<SchoolAdminResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SchoolAdminResponse>> AssignAdmin(
        Guid schoolId,
        AssignSchoolAdminRequest request,
        CancellationToken cancellationToken)
    {
        var schoolExists = await _db.Schools
            .AnyAsync(school => school.Id == schoolId, cancellationToken);
        if (!schoolExists)
        {
            return NotFoundProblem("The school was not found.");
        }

        var admin = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (admin is null)
        {
            return NotFoundProblem("No user account was found for that email.");
        }

        if (await _userManager.IsInRoleAsync(admin, Roles.SuperAdmin))
        {
            return ConflictProblem("SuperAdmin cannot be assigned", "A SuperAdmin account cannot be assigned to a school.");
        }

        if (admin.SchoolId != School.DefaultSchoolId && admin.SchoolId != schoolId)
        {
            return ConflictProblem(
                "Account belongs to another school",
                "Unassign this account from its current school before assigning it to another.");
        }

        var assignedSchoolAdmins = await _userManager.GetUsersInRoleAsync(Roles.SchoolAdmin);
        var currentAdmin = assignedSchoolAdmins.FirstOrDefault(user => user.SchoolId == schoolId);
        if (currentAdmin is null)
        {
            var legacyAdmins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            currentAdmin = legacyAdmins.FirstOrDefault(user => user.SchoolId == schoolId);
        }

        if (currentAdmin is not null && currentAdmin.Id != admin.Id)
        {
            return ConflictProblem(
                "School already has an administrator",
                "Unassign the current administrator before assigning another account.");
        }

        if (!admin.IsActive)
        {
            return ConflictProblem("Account is inactive", "Activate the account before assigning it as an administrator.");
        }

        if (!await _roleManager.RoleExistsAsync(Roles.Admin)
            || !await _roleManager.RoleExistsAsync(Roles.SchoolAdmin))
        {
            return ConflictProblem(
                "Administrator role is unavailable",
                "The required school administrator roles have not been initialized.");
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        var result = await strategy.ExecuteAsync(async () =>
        {
            _db.ChangeTracker.Clear();
            await using var transaction = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable,
                cancellationToken);

            admin.SchoolId = schoolId;
            var updateResult = await _userManager.UpdateAsync(admin);
            if (!updateResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (Succeeded: false, Errors: updateResult.Errors.Select(error => error.Description).ToArray());
            }

            foreach (var role in new[] { Roles.Admin, Roles.SchoolAdmin })
            {
                if (await _userManager.IsInRoleAsync(admin, role))
                {
                    continue;
                }

                var roleResult = await _userManager.AddToRoleAsync(admin, role);
                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return (Succeeded: false, Errors: roleResult.Errors.Select(error => error.Description).ToArray());
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return (Succeeded: true, Errors: Array.Empty<string>());
        });

        if (!result.Succeeded)
        {
            return ConflictProblem("Administrator assignment failed", string.Join("; ", result.Errors));
        }

        return Ok(new SchoolAdminResponse(admin.Id, admin.FullName, admin.Email!));
    }

    /// <summary>Removes the assigned administrator role from a school's account.</summary>
    [HttpDelete("/api/superadmin/schools/{schoolId:guid}/admin")]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnassignAdmin(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        var schoolExists = await _db.Schools
            .AnyAsync(school => school.Id == schoolId, cancellationToken);
        if (!schoolExists)
        {
            return NotFoundProblem("The school was not found.");
        }

        var admin = await _userManager.GetUsersInRoleAsync(Roles.SchoolAdmin);
        var assignedAdmin = admin.FirstOrDefault(user => user.SchoolId == schoolId);
        if (assignedAdmin is null)
        {
            var legacyAdmins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            assignedAdmin = legacyAdmins.FirstOrDefault(user => user.SchoolId == schoolId);
        }

        if (assignedAdmin is null)
        {
            return NotFoundProblem("The school has no assigned administrator.");
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        var unassignResult = await strategy.ExecuteAsync(async () =>
        {
            _db.ChangeTracker.Clear();
            await using var transaction = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable,
                cancellationToken);

            var currentAdmin = await _userManager.FindByIdAsync(assignedAdmin.Id);
            if (currentAdmin is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (Succeeded: false, Errors: new[] { "The administrator account no longer exists." });
            }

            foreach (var role in new[] { Roles.SchoolAdmin, Roles.Admin })
            {
                if (!await _userManager.IsInRoleAsync(currentAdmin, role))
                {
                    continue;
                }

                var roleResult = await _userManager.RemoveFromRoleAsync(currentAdmin, role);
                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return (Succeeded: false, Errors: roleResult.Errors.Select(error => error.Description).ToArray());
                }
            }

            currentAdmin.SchoolId = School.DefaultSchoolId;
            var updateResult = await _userManager.UpdateAsync(currentAdmin);
            if (!updateResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (Succeeded: false, Errors: updateResult.Errors.Select(error => error.Description).ToArray());
            }

            await transaction.CommitAsync(cancellationToken);
            return (Succeeded: true, Errors: Array.Empty<string>());
        });

        if (!unassignResult.Succeeded)
        {
            return ConflictProblem(
                "Administrator could not be unassigned",
                string.Join("; ", unassignResult.Errors));
        }
        return NoContent();
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

    /// <summary>Creates a tenant through the legacy route, restricted to SuperAdmin.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType<SchoolResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<SchoolResponse>> CreateSchoolForAdmin(
        CreateSchoolRequest request,
        CancellationToken cancellationToken)
        => CreateSchoolAsync(request, isSuperAdmin: true, cancellationToken);

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

        if (!await _roleManager.RoleExistsAsync(Roles.Admin)
            || !await _roleManager.RoleExistsAsync(Roles.SchoolAdmin))
        {
            return ConflictProblem(
                "Administrator role is unavailable",
                "The required school administrator roles have not been initialized.");
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            _db.ChangeTracker.Clear();
            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var schoolId = Guid.NewGuid();
            var slugBase = Slugify(request.SchoolName);
            if (string.IsNullOrWhiteSpace(slugBase))
            {
                slugBase = $"school-{schoolId.ToString("N")[..8]}";
            }

            slugBase = slugBase[..Math.Min(slugBase.Length, 160)];
            var slug = slugBase;
            if (await _db.Schools.AnyAsync(school => school.Slug == slug, cancellationToken))
            {
                slug = $"{slugBase}-{Slugify(normalizedCode)}";
                if (await _db.Schools.AnyAsync(school => school.Slug == slug, cancellationToken))
                {
                    slug = $"{slugBase}-{schoolId.ToString("N")[..8]}";
                }
            }
            var grades = await _db.GradeLevels.AsNoTracking()
                .OrderBy(grade => grade.Level)
                .Select(grade => new { grade.Id, grade.Name })
                .ToListAsync(cancellationToken);
            var school = new School
            {
                Id = schoolId,
                Name = request.SchoolName.Trim(),
                Slug = slug,
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

            var adminRoleResult = await _userManager.AddToRoleAsync(user, Roles.Admin);
            var schoolAdminRoleResult = adminRoleResult.Succeeded
                ? await _userManager.AddToRoleAsync(user, Roles.SchoolAdmin)
                : adminRoleResult;
            if (!adminRoleResult.Succeeded || !schoolAdminRoleResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ConflictProblem(
                    "Initial administrator role could not be assigned",
                    string.Join(
                        "; ",
                        adminRoleResult.Errors.Concat(schoolAdminRoleResult.Errors)
                            .Select(error => error.Description)));
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
                user.Email!,
                school.Slug);
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
                item.Slug,
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
                school.Admin.Email!,
                school.Slug));
    }

    /// <summary>Updates the name of exactly one school tenant.</summary>
    [HttpPut("/api/superadmin/schools/{schoolId:guid}")]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType<SchoolNameResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ActionResult<SchoolNameResponse>> UpdateSchoolNameAsSuperAdmin(
        Guid schoolId,
        UpdateSchoolNameRequest request,
        CancellationToken cancellationToken)
        => UpdateSchoolNameAsync(schoolId, request, isSuperAdmin: true, cancellationToken);

    /// <summary>Updates only the authenticated school administrator's school name.</summary>
    [HttpPut("/api/admin/schools/{schoolId:guid}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.SchoolAdmin)]
    [ProducesResponseType<SchoolNameResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ActionResult<SchoolNameResponse>> UpdateSchoolNameAsSchoolAdmin(
        Guid schoolId,
        UpdateSchoolNameRequest request,
        CancellationToken cancellationToken)
        => UpdateSchoolNameAsync(schoolId, request, isSuperAdmin: false, cancellationToken);

    private async Task<ActionResult<SchoolNameResponse>> UpdateSchoolNameAsync(
        Guid schoolId,
        UpdateSchoolNameRequest request,
        bool isSuperAdmin,
        CancellationToken cancellationToken)
    {
        if (!isSuperAdmin && schoolId != CurrentSchoolId)
        {
            return ForbiddenProblem("You can only update your own school's name.");
        }

        var school = await _db.Schools
            .SingleOrDefaultAsync(item => item.Id == schoolId, cancellationToken);
        if (school is null)
        {
            return NotFoundProblem("The school was not found.");
        }

        school.Name = request.Name.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new SchoolNameResponse(school.Id, school.Name));
    }

    private static string Slugify(string value) =>
        Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');

    /// <summary>Returns the authenticated administrator's school identity.</summary>
    [HttpGet("{schoolId:guid}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.SchoolAdmin)]
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
                item.Address,
                item.Slug
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
                User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                school.Slug));
    }
}
