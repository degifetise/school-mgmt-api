using HaladeHighSchool.Api.Data;
using HaladeHighSchool.Api.DTOs;
using HaladeHighSchool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HaladeHighSchool.Api.Services;

public sealed class AnalyticsService(ApplicationDbContext db) : IAnalyticsService
{
    private const decimal PassMarkPercentage = 50m;

    private sealed record PublishedMark(
        int StudentId,
        int SubjectId,
        AssessmentType AssessmentType,
        decimal Score,
        decimal MaxScore);

    public async Task<AdminAnalyticsOverviewResponse> GetAdminOverviewAsync(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        var totalStudents = await db.Students
            .AsNoTracking()
            .CountAsync(student => student.IsActive
                && student.User != null
                && student.User.SchoolId == schoolId, cancellationToken);
        var totalTeachers = await db.Teachers
            .AsNoTracking()
            .CountAsync(teacher => teacher.IsActive
                && teacher.User != null
                && teacher.User.SchoolId == schoolId, cancellationToken);

        var gradeCounts = await (
            from student in db.Students.AsNoTracking()
            join grade in db.GradeLevels.AsNoTracking()
                on student.GradeLevelId equals grade.Id
            where student.IsActive
                && student.User != null
                && student.User!.SchoolId == schoolId
            group student by grade.Level into studentsByGrade
            select new { GradeLevel = studentsByGrade.Key, StudentCount = studentsByGrade.Count() })
            .ToDictionaryAsync(row => row.GradeLevel, row => row.StudentCount, cancellationToken);

        var publishedMarks = await (
            from mark in db.Marks.AsNoTracking()
            join student in db.Students.AsNoTracking()
                on mark.StudentId equals student.Id
            join assessment in db.Assessments.AsNoTracking()
                on mark.AssessmentId equals assessment.Id
            where student.IsActive
                && student.User != null
                && student.User.SchoolId == schoolId
                && mark.IsPublished
                && assessment.MaxScore > 0
            select new PublishedMark(
                student.Id,
                mark.SubjectId,
                assessment.AssessmentType,
                mark.Score,
                assessment.MaxScore))
            .ToListAsync(cancellationToken);

        var weights = await GetActiveWeightsAsync(cancellationToken);
        var studentAverages = CalculateStudentAverages(publishedMarks, weights);

        var gradeDistribution = Enumerable.Range(9, 4)
            .Select(level => new GradeEnrollmentResponse(
                level,
                gradeCounts.GetValueOrDefault(level)))
            .ToArray();

        return new AdminAnalyticsOverviewResponse(
            totalStudents,
            totalTeachers,
            gradeDistribution,
            studentAverages.Count,
            studentAverages.Count(average => average >= PassMarkPercentage),
            studentAverages.Count(average => average < PassMarkPercentage),
            PassMarkPercentage);
    }

    public async Task<TeacherAnalyticsPerformanceResponse> GetTeacherPerformanceAsync(
        int? requestedTeacherId,
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        if (requestedTeacherId is not int teacherId)
        {
            return EmptyTeacherResponse();
        }

        var teacherExists = await db.Teachers
            .AsNoTracking()
            .AnyAsync(teacher => teacher.Id == teacherId
                && teacher.IsActive
                && teacher.User != null
                && teacher.User.SchoolId == schoolId,
                cancellationToken);

        if (!teacherExists)
        {
            return EmptyTeacherResponse();
        }

        var assignedClassCount = await db.TeacherSubjects
            .AsNoTracking()
            .CountAsync(assignment => assignment.TeacherId == teacherId && assignment.IsActive, cancellationToken);

        var publishedMarks = await (
            from assignment in db.TeacherSubjects.AsNoTracking()
            join student in db.Students.AsNoTracking()
                on assignment.SectionId equals student.SectionId
            join subject in db.Subjects.AsNoTracking()
                on assignment.SubjectId equals subject.Id
            join assessment in db.Assessments.AsNoTracking()
                on subject.Id equals assessment.SubjectId
            join mark in db.Marks.AsNoTracking()
                on new { StudentId = student.Id, AssessmentId = assessment.Id, SubjectId = subject.Id }
                equals new { mark.StudentId, mark.AssessmentId, mark.SubjectId }
            where assignment.TeacherId == teacherId
                && assignment.IsActive
                && student.IsActive
                && student.User != null
                && student.User!.SchoolId == schoolId
                && subject.GradeLevelId == student.GradeLevelId
                && mark.IsPublished
                && assessment.MaxScore > 0
            select new PublishedMark(
                student.Id,
                subject.Id,
                assessment.AssessmentType,
                mark.Score,
                assessment.MaxScore))
            .ToListAsync(cancellationToken);

        var weights = await GetActiveWeightsAsync(cancellationToken);
        var studentAverages = CalculateStudentAverages(publishedMarks, weights);

        return new TeacherAnalyticsPerformanceResponse(
            assignedClassCount,
            studentAverages.Count,
            studentAverages.Count(average => average >= PassMarkPercentage),
            studentAverages.Count(average => average < PassMarkPercentage),
            PassMarkPercentage);
    }

    private async Task<IReadOnlyDictionary<string, decimal>> GetActiveWeightsAsync(
        CancellationToken cancellationToken) =>
        await db.AssessmentTypeWeights
            .AsNoTracking()
            .Where(weight => weight.IsActive)
            .ToDictionaryAsync(
                weight => weight.Name,
                weight => weight.WeightPercentage,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

    private static List<decimal> CalculateStudentAverages(
        IEnumerable<PublishedMark> marks,
        IReadOnlyDictionary<string, decimal> weights)
    {
        var subjectTotals = marks
            .GroupBy(mark => new { mark.StudentId, mark.SubjectId, mark.AssessmentType })
            .Select(group =>
            {
                var possible = group.Sum(mark => mark.MaxScore);
                if (possible <= 0
                    || !weights.TryGetValue(group.Key.AssessmentType.ToString(), out var weight))
                {
                    return (group.Key.StudentId, group.Key.SubjectId, Score: (decimal?)null);
                }

                return (
                    group.Key.StudentId,
                    group.Key.SubjectId,
                    Score: (decimal?)(group.Sum(mark => mark.Score) / possible * weight));
            })
            .Where(result => result.Score.HasValue)
            .GroupBy(result => new { result.StudentId, result.SubjectId })
            .Select(group => new
            {
                group.Key.StudentId,
                Total = group.Sum(result => result.Score.GetValueOrDefault())
            });

        return subjectTotals
            .GroupBy(result => result.StudentId)
            .Select(group => group.Average(result => result.Total))
            .ToList();
    }

    private static TeacherAnalyticsPerformanceResponse EmptyTeacherResponse() =>
        new(0, 0, 0, 0, PassMarkPercentage);
}
