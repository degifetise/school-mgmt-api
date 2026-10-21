using HaladeHighSchool.Api.Data;
using HaladeHighSchool.Api.DTOs;
using HaladeHighSchool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HaladeHighSchool.Api.Services;

public sealed record TeacherSectionAssignmentsResult(
    bool TeacherFound,
    bool AssignmentsValid,
    IReadOnlyList<TeacherSectionAssignmentResponse> Assignments);

public interface ITeacherAssignmentService
{
    Task<IReadOnlyList<TeacherSectionAssignmentResponse>?> GetSectionAssignmentsAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<TeacherSectionAssignmentsResult> AddSectionAssignmentsAsync(
        int teacherId,
        IReadOnlyList<TeacherSectionAssignmentRequestItem> assignments,
        CancellationToken cancellationToken = default);

    Task<TeacherSectionAssignmentsResult> ReplaceSectionAssignmentsAsync(
        int teacherId,
        IReadOnlyList<TeacherSectionAssignmentRequestItem> assignments,
        CancellationToken cancellationToken = default);

    Task<bool?> RemoveSectionAssignmentAsync(
        int teacherId,
        int sectionId,
        CancellationToken cancellationToken = default);
}

public sealed class TeacherAssignmentService(ApplicationDbContext db) : ITeacherAssignmentService
{
    public async Task<IReadOnlyList<TeacherSectionAssignmentResponse>?> GetSectionAssignmentsAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Teachers.AnyAsync(teacher => teacher.Id == teacherId, cancellationToken))
        {
            return null;
        }

        return await ProjectAssignments(db.TeacherSectionAssignments.AsNoTracking()
                .Where(assignment => assignment.TeacherId == teacherId))
            .OrderBy(assignment => assignment.GradeLevelName)
            .ThenBy(assignment => assignment.SectionName)
            .ToListAsync(cancellationToken);
    }

    public Task<TeacherSectionAssignmentsResult> AddSectionAssignmentsAsync(
        int teacherId,
        IReadOnlyList<TeacherSectionAssignmentRequestItem> assignments,
        CancellationToken cancellationToken = default) =>
        SaveAssignmentsAsync(teacherId, assignments, replace: false, cancellationToken);

    public Task<TeacherSectionAssignmentsResult> ReplaceSectionAssignmentsAsync(
        int teacherId,
        IReadOnlyList<TeacherSectionAssignmentRequestItem> assignments,
        CancellationToken cancellationToken = default) =>
        SaveAssignmentsAsync(teacherId, assignments, replace: true, cancellationToken);

    public async Task<bool?> RemoveSectionAssignmentAsync(
        int teacherId,
        int sectionId,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Teachers.AnyAsync(teacher => teacher.Id == teacherId, cancellationToken))
        {
            return null;
        }

        var matching = await db.TeacherSectionAssignments
            .Where(assignment => assignment.TeacherId == teacherId && assignment.SectionId == sectionId)
            .ToListAsync(cancellationToken);
        if (matching.Count == 0)
        {
            return false;
        }

        db.TeacherSectionAssignments.RemoveRange(matching);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<TeacherSectionAssignmentsResult> SaveAssignmentsAsync(
        int teacherId,
        IReadOnlyList<TeacherSectionAssignmentRequestItem> requested,
        bool replace,
        CancellationToken cancellationToken)
    {
        if (!await db.Teachers.AnyAsync(teacher => teacher.Id == teacherId, cancellationToken))
        {
            return new(false, false, []);
        }

        var normalized = requested
            .DistinctBy(item => new { item.GradeLevelId, item.SectionId })
            .ToArray();
        var gradeLevelIds = normalized.Select(item => item.GradeLevelId).Distinct().ToArray();
        var sectionIds = normalized.Select(item => item.SectionId).Distinct().ToArray();
        var validPairs = await db.GradeLevelSections
            .AsNoTracking()
            .Where(link => gradeLevelIds.Contains(link.GradeLevelId)
                && sectionIds.Contains(link.SectionId)
                && link.GradeLevel!.IsActive
                && link.Section!.IsActive)
            .Select(link => new { link.GradeLevelId, link.SectionId })
            .ToListAsync(cancellationToken);
        var validPairSet = validPairs
            .Select(pair => (pair.GradeLevelId, pair.SectionId))
            .ToHashSet();

        if (normalized.Any(item => !validPairSet.Contains((item.GradeLevelId, item.SectionId))))
        {
            return new(true, false, []);
        }

        var existing = await db.TeacherSectionAssignments
            .Where(assignment => assignment.TeacherId == teacherId)
            .ToListAsync(cancellationToken);

        if (replace)
        {
            var requestedPairs = normalized
                .Select(item => (item.GradeLevelId, item.SectionId))
                .ToHashSet();
            var removed = existing
                .Where(assignment => !requestedPairs.Contains((assignment.GradeLevelId, assignment.SectionId)))
                .ToArray();
            db.TeacherSectionAssignments.RemoveRange(removed);
            existing = existing.Except(removed).ToList();
        }

        foreach (var item in normalized)
        {
            if (existing.Any(assignment =>
                    assignment.GradeLevelId == item.GradeLevelId &&
                    assignment.SectionId == item.SectionId))
            {
                continue;
            }

            db.TeacherSectionAssignments.Add(new TeacherSectionAssignment
            {
                TeacherId = teacherId,
                GradeLevelId = item.GradeLevelId,
                SectionId = item.SectionId,
                AssignedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        var result = await ProjectAssignments(db.TeacherSectionAssignments.AsNoTracking()
                .Where(assignment => assignment.TeacherId == teacherId))
            .OrderBy(assignment => assignment.GradeLevelName)
            .ThenBy(assignment => assignment.SectionName)
            .ToListAsync(cancellationToken);

        return new(true, true, result);
    }

    private static IQueryable<TeacherSectionAssignmentResponse> ProjectAssignments(
        IQueryable<TeacherSectionAssignment> query) =>
        query.Select(assignment => new TeacherSectionAssignmentResponse(
            assignment.TeacherId,
            assignment.GradeLevelId,
            assignment.GradeLevelSection!.GradeLevel!.Name,
            assignment.SectionId,
            assignment.GradeLevelSection!.Section!.Name));
}
