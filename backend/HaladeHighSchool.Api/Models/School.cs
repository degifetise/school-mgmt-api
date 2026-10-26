namespace HaladeHighSchool.Api.Models;

/// <summary>Tenant record and its active academic grade catalog.</summary>
public sealed class School
{
    public static readonly Guid DefaultSchoolId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string HeroSubtitle { get; set; } = "Nursery through Grade 12, one portal.";

    public string HeroDescription { get; set; } =
        "Lessons, weighted assessments and report cards in one place.";

    public string PrimaryColor { get; set; } = "#1e3a8a";

    public string? LogoUrl { get; set; }

    public bool AllowPublicRegistration { get; set; }

    public string AcademicYear { get; set; } =
        $"{DateTime.UtcNow.Year}-{DateTime.UtcNow.Year + 1}";

    public string[] SupportedGrades { get; set; } =
    [
        "Nursery", "KG 1", "KG 2", "KG 3",
        .. Enumerable.Range(1, 12).Select(grade => $"Grade {grade}")
    ];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

    public ICollection<SchoolGradeSection> GradeSections { get; set; } = new List<SchoolGradeSection>();
}

/// <summary>A section made available to a grade in one school.</summary>
public sealed class SchoolGradeSection
{
    public Guid SchoolId { get; set; }

    public int GradeLevelId { get; set; }

    public int SectionId { get; set; }

    public School? School { get; set; }

    public GradeLevel? GradeLevel { get; set; }

    public Section? Section { get; set; }
}
