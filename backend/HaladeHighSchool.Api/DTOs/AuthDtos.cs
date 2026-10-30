using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HaladeHighSchool.Api.DTOs;

public record LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public record AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserProfileResponse User { get; init; } = new();
}

public record UserProfileResponse
{
    public string Id { get; init; } = string.Empty;
    public Guid SchoolId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? ProfileImageUrl { get; init; }
    public bool MustChangePasswordOnLogin { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];

    /// <summary>Populated when the user is a student.</summary>
    public int? StudentId { get; init; }
    public string? StudentIdNumber { get; init; }
    public int? GradeLevelId { get; init; }
    public string? GradeLevelName { get; init; }
    public int? SectionId { get; init; }
    public string? SectionName { get; init; }

    /// <summary>Populated when the user is a teacher.</summary>
    public int? TeacherId { get; init; }
    public string? EmployeeId { get; init; }
    public string? Specialization { get; init; }

    /// <summary>Populated when the user is a guardian.</summary>
    public int? GuardianId { get; init; }
    public IReadOnlyList<int> LinkedStudentIds { get; init; } = [];
}

/// <summary>
/// What an applicant sends to ask for a place. No password and no student number: the portal
/// issues both at approval, so an applicant cannot choose their own credentials or claim an
/// identifier that is not theirs.
/// </summary>
<<<<<<< HEAD
public record RegisterStudentRequest : IValidatableObject
=======
public record RegisterStudentRequest
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
{
    private string _fullName = string.Empty;
    private string _email = string.Empty;

    [Required]
    [RegularExpression("^(Student|Teacher)$", ErrorMessage = "RequestedRole must be Student or Teacher.")]
    public string RequestedRole { get; init; } = "Student";

    [Required(ErrorMessage = "Tell us your full name.")]
    [MaxLength(150)]
    [JsonPropertyName("fullName")]
    public string FullName
    {
        get => _fullName;
        init => _fullName = value?.Trim() ?? string.Empty;
    }

<<<<<<< HEAD
    /// <summary>The contact address for the applicant or their parent/guardian.</summary>
=======
    /// <summary>The applicant's own address. The issued credentials are sent here.</summary>
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
    [Required(ErrorMessage = "An email address is required so the school can reach you.")]
    [EmailAddress(ErrorMessage = "That does not look like an email address.")]
    [MaxLength(256)]
    [JsonPropertyName("email")]
    public string Email
    {
        get => _email;
        init => _email = value?.Trim() ?? string.Empty;
    }

<<<<<<< HEAD
    [Required(ErrorMessage = "A phone number is required.")]
    [MaxLength(30)]
    public string PhoneNumber { get; init; } = string.Empty;

    [MaxLength(120)]
    public string? DepartmentSubject { get; init; }

=======
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
    [Range(1, int.MaxValue, ErrorMessage = "Choose a grade.")]
    public int? GradeLevelId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose a section.")]
    public int? SectionId { get; init; }

<<<<<<< HEAD
    [Required(ErrorMessage = "A profile photo is required to generate your Smart ID.")]
    [JsonIgnore]
    public IFormFile? Photo { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RequestedRole == "Student")
        {
            if (GradeLevelId is null or < 1)
            {
                yield return new ValidationResult("Choose a grade.", [nameof(GradeLevelId)]);
            }

            if (SectionId is null or < 1)
            {
                yield return new ValidationResult("Choose a section.", [nameof(SectionId)]);
            }
        }
        else if (RequestedRole == "Teacher" && string.IsNullOrWhiteSpace(DepartmentSubject))
        {
            yield return new ValidationResult(
                "Enter a department or subject.",
                [nameof(DepartmentSubject)]);
        }
    }
=======
    [JsonIgnore]
    public IFormFile? Photo { get; init; }
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
}

/// <summary>
/// Receipt for a submitted request. Carries no token and no credentials: nothing exists to sign
/// in with until an administrator approves it.
/// </summary>
public record RegistrationSubmittedResponse
{
    public int RequestId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string RequestedRole { get; init; } = "Student";
    public string FullName { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string GradeLevelName { get; init; } = string.Empty;
    public string SectionName { get; init; } = string.Empty;
    public DateTime SubmittedAt { get; init; }
    public string Message { get; init; } = string.Empty;
}

public record RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
