using System.ComponentModel.DataAnnotations;

namespace HaladeHighSchool.Api.DTOs;

/// <summary>One grade and section pair to assign to a teacher.</summary>
public sealed record TeacherSectionAssignmentRequestItem
{
    [Range(1, int.MaxValue)]
    public int GradeLevelId { get; init; }

    [Range(1, int.MaxValue)]
    public int SectionId { get; init; }
}

/// <summary>Creates or replaces a teacher's section access assignments.</summary>
public sealed record UpdateTeacherSectionAssignmentsRequest
{
    [Required]
    public required IReadOnlyList<TeacherSectionAssignmentRequestItem> Assignments { get; init; }
}

/// <summary>A teacher's assignment to a grade and section.</summary>
public sealed record TeacherSectionAssignmentResponse(
    int TeacherId,
    int GradeLevelId,
    string GradeLevelName,
    int SectionId,
    string SectionName);
