using System.ComponentModel.DataAnnotations;

namespace HaladeHighSchool.Api.DTOs;

/// <summary>Data a teacher or administrator records for a student's conduct history.</summary>
public sealed record CreateStudentConductLogRequest
{
    [Range(1, int.MaxValue)]
    public required int StudentId { get; init; }

    [Required, MaxLength(40)]
    public required string Type { get; init; }

    [Required, MaxLength(150)]
    public required string Title { get; init; }

    [Required, MaxLength(4000)]
    public required string Description { get; init; }

    public required DateTimeOffset IncidentDate { get; init; }

    public bool IsVisibleToStudent { get; init; } = true;

    public bool IsVisibleToParent { get; init; } = true;
}

/// <summary>A conduct record returned to an authorized portal user.</summary>
public sealed record StudentConductLogResponse(
    int Id,
    int StudentId,
    string RecordedByName,
    string RecordedByRole,
    string Type,
    string Title,
    string Description,
    DateTimeOffset IncidentDate,
    bool IsVisibleToStudent,
    bool IsVisibleToParent,
    DateTimeOffset CreatedAt);

/// <summary>Conduct history and derived standing for the signed-in student.</summary>
public sealed record StudentConductStatusResponse(
    string Standing,
    IReadOnlyList<StudentConductLogResponse> Logs);
