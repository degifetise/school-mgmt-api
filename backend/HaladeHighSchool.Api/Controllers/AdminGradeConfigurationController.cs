using System.Security.Claims;
using HaladeHighSchool.Api.Configuration;
using HaladeHighSchool.Api.Data;
using HaladeHighSchool.Api.DTOs;
using HaladeHighSchool.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HaladeHighSchool.Api.Controllers;

/// <summary>Manages the active grades and sections available to one school.</summary>
[ApiController]
[Route("api/admin/grade-configuration")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public sealed class AdminGradeConfigurationController : PortalControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminGradeConfigurationController(ApplicationDbContext db) => _db = db;

    /// <summary>Returns the grade catalog and this school's active grade-section configuration.</summary>
    [HttpGet]
    [ProducesResponseType<SchoolGradeConfigurationListResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SchoolGradeConfigurationListResponse>> GetConfiguration(
        CancellationToken cancellationToken)
    {
        if (!TryGetSchoolId(out var schoolId))
        {
            return Forbid();
        }

        var school = await _db.Schools.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == schoolId, cancellationToken);
        if (school is null)
        {
            return NotFoundProblem("The school associated with this account was not found.");
        }

        var assigned = await _db.SchoolGradeSections.AsNoTracking()
            .Where(link => link.SchoolId == schoolId)
            .GroupBy(link => link.GradeLevelId)
            .Select(group => new { GradeLevelId = group.Key, SectionIds = group.Select(link => link.SectionId).ToList() })
            .ToDictionaryAsync(group => group.GradeLevelId, group => group.SectionIds, cancellationToken);

        var grades = await _db.GradeLevels.AsNoTracking()
            .OrderBy(grade => grade.Level)
            .Select(grade => new
            {
                grade.Id,
                grade.Name,
                grade.Level,
                Sections = grade.SectionLinks
                    .Where(link => link.Section != null && link.Section.IsActive)
                    .OrderBy(link => link.Section!.Name)
                    .Select(link => new SchoolSectionOptionResponse(
                        link.Section!.Id,
                        link.Section.Name,
                        link.Section.Code))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(new SchoolGradeConfigurationListResponse(
            school.SupportedGrades,
            grades.Select(grade => new SchoolGradeConfigurationResponse(
                grade.Id,
                grade.Name,
                grade.Level,
                school.SupportedGrades.Contains(grade.Name, StringComparer.OrdinalIgnoreCase),
                assigned.GetValueOrDefault(grade.Id, []),
                grade.Sections)).ToList()));
    }

    /// <summary>Replaces this school's active grade list and grade-section assignments.</summary>
    [HttpPut]
    [ProducesResponseType<SchoolGradeConfigurationListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SchoolGradeConfigurationListResponse>> UpdateConfiguration(
        UpdateSchoolGradeConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetSchoolId(out var schoolId))
        {
            return Forbid();
        }

        var school = await _db.Schools.FirstOrDefaultAsync(item => item.Id == schoolId, cancellationToken);
        if (school is null)
        {
            return NotFoundProblem("The school associated with this account was not found.");
        }

        if (request.Grades.Select(grade => grade.GradeLevelId).Distinct().Count() != request.Grades.Count)
        {
            ModelState.AddModelError(nameof(request.Grades), "Each grade level must appear only once.");
            return ValidationProblem(ModelState);
        }

        var gradeIds = request.Grades.Select(grade => grade.GradeLevelId).ToArray();
        var catalogGradeCount = await _db.GradeLevels.CountAsync(cancellationToken);
        if (gradeIds.Length != catalogGradeCount)
        {
            ModelState.AddModelError(nameof(request.Grades), "Submit a configuration entry for every grade level.");
            return ValidationProblem(ModelState);
        }

        var gradeCatalog = await _db.GradeLevels
            .Where(grade => gradeIds.Contains(grade.Id))
            .ToDictionaryAsync(grade => grade.Id, cancellationToken);
        if (gradeCatalog.Count != gradeIds.Length)
        {
            ModelState.AddModelError(nameof(request.Grades), "One or more grade levels are not in the catalog.");
            return ValidationProblem(ModelState);
        }

        var requestedSectionIds = request.Grades
            .SelectMany(grade => grade.SectionIds)
            .Distinct()
            .ToArray();
        var validLinks = await _db.GradeLevelSections.AsNoTracking()
            .Where(link => gradeIds.Contains(link.GradeLevelId)
                && requestedSectionIds.Contains(link.SectionId)
                && link.Section != null
                && link.Section.IsActive)
            .Select(link => new { link.GradeLevelId, link.SectionId })
            .ToListAsync(cancellationToken);
        var validSectionPairs = validLinks.Select(link => (link.GradeLevelId, link.SectionId)).ToHashSet();

        foreach (var grade in request.Grades)
        {
            if (grade.SectionIds.Distinct().Count() != grade.SectionIds.Count)
            {
                ModelState.AddModelError(nameof(request.Grades), $"Duplicate section assigned to {gradeCatalog[grade.GradeLevelId].Name}.");
                continue;
            }

            if (grade.SectionIds.Any(sectionId => !validSectionPairs.Contains((grade.GradeLevelId, sectionId))))
            {
                ModelState.AddModelError(nameof(request.Grades), $"A selected section is not available for {gradeCatalog[grade.GradeLevelId].Name}.");
            }

            if (!grade.IsActive && grade.SectionIds.Count > 0)
            {
                ModelState.AddModelError(nameof(request.Grades), $"Inactive grade {gradeCatalog[grade.GradeLevelId].Name} cannot have assigned sections.");
            }
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        school.SupportedGrades = request.Grades
            .Where(grade => grade.IsActive)
            .OrderBy(grade => gradeCatalog[grade.GradeLevelId].Level)
            .Select(grade => gradeCatalog[grade.GradeLevelId].Name)
            .ToArray();

        var previousAssignments = await _db.SchoolGradeSections
            .Where(link => link.SchoolId == schoolId)
            .ToListAsync(cancellationToken);
        _db.SchoolGradeSections.RemoveRange(previousAssignments);
        _db.SchoolGradeSections.AddRange(request.Grades
            .Where(grade => grade.IsActive)
            .SelectMany(grade => grade.SectionIds.Select(sectionId => new SchoolGradeSection
            {
                SchoolId = schoolId,
                GradeLevelId = grade.GradeLevelId,
                SectionId = sectionId
            })));

        await _db.SaveChangesAsync(cancellationToken);
        return await GetConfiguration(cancellationToken);
    }

    private bool TryGetSchoolId(out Guid schoolId) =>
        Guid.TryParse(User.FindFirstValue(PortalClaims.SchoolId), out schoolId);
}
