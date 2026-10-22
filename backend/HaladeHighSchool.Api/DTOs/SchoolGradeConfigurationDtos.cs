using System.ComponentModel.DataAnnotations;

namespace HaladeHighSchool.Api.DTOs;

/// <summary>A section option available for a configured school grade.</summary>
public sealed record SchoolSectionOptionResponse(int Id, string Name, string Code);

/// <summary>A grade and its school-specific availability and section assignments.</summary>
public sealed record SchoolGradeConfigurationResponse(
    int GradeLevelId,
    string Name,
    int Level,
    bool IsActive,
    IReadOnlyList<int> SectionIds,
    IReadOnlyList<SchoolSectionOptionResponse> AvailableSections);

/// <summary>School grade configuration and the currently supported grade names.</summary>
public sealed record SchoolGradeConfigurationListResponse(
    IReadOnlyList<string> SupportedGrades,
    IReadOnlyList<SchoolGradeConfigurationResponse> Grades);

/// <summary>School-specific grade availability and section assignments to persist.</summary>
public sealed record UpdateSchoolGradeConfigurationRequest
{
    [Required]
    public required IReadOnlyList<SchoolGradeConfigurationUpdate> Grades { get; init; }
}

/// <summary>Availability and assigned sections for one grade level.</summary>
public sealed record SchoolGradeConfigurationUpdate
{
    [Range(1, int.MaxValue)]
    public int GradeLevelId { get; init; }

    public bool IsActive { get; init; }

    public IReadOnlyList<int> SectionIds { get; init; } = [];
}

/// <summary>Creates a school tenant and its first administrator account.</summary>
public sealed record CreateSchoolRequest
{
    [Required, MaxLength(200)]
    public required string SchoolName { get; init; }

    [Required, MaxLength(50), RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9-]*$")]
    public required string SchoolCode { get; init; }

    [Required, EmailAddress, MaxLength(256)]
    public required string ContactEmail { get; init; }

    [Required, Phone, MaxLength(50)]
    public required string ContactPhone { get; init; }

    [Required, MaxLength(500)]
    public required string Address { get; init; }

    [Required, MaxLength(150)]
    public required string AdminFullName { get; init; }

    [Required, EmailAddress, MaxLength(256)]
    public required string AdminEmail { get; init; }

    [Required, MinLength(8), MaxLength(100)]
    public required string AdminTemporaryPassword { get; init; }
}

/// <summary>Tenant details returned after school creation.</summary>
public sealed record SchoolResponse(
    Guid SchoolId,
    string SchoolName,
    string SchoolCode,
    string ContactEmail,
    string ContactPhone,
    string Address,
    string AdminFullName,
    string AdminEmail);
