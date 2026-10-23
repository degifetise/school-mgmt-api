using System.Globalization;
using HaladeHighSchool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HaladeHighSchool.Api.Data;

public static class SchoolMasterDataSeeder
{
    private sealed record GradeDefinition(string Name, int Level, string Description);

    private sealed record SubjectDefinition(string Name, string CodePrefix, int CreditHours);

    private static readonly GradeDefinition[] Grades =
    [
        new("Nursery", -3, "Early childhood"),
        new("KG 1", -2, "Kindergarten"),
        new("KG 2", -1, "Kindergarten"),
        new("KG 3", 0, "Kindergarten"),
        .. Enumerable.Range(1, 12)
            .Select(grade => new GradeDefinition(
                $"Grade {grade}",
                grade,
                grade <= 5 ? "Primary school" : grade <= 8 ? "Middle school" : "Secondary school"))
    ];

    private static readonly string[] SectionCodes = ["A", "B", "C", "D", "E", "F"];

    private static readonly string[] SectionNames =
        ["Section A", "Section B", "Section C", "Section D", "Section E", "Section F"];

    private static readonly SubjectDefinition[] Subjects =
    [
        new("Mathematics", "MATH", 5),
        new("English", "ENG", 4),
        new("Physics", "PHY", 4),
        new("Chemistry", "CHEM", 4),
        new("Biology", "BIO", 4),
        new("Civics", "CIV", 2),
        new("History", "HIST", 3),
        new("Geography", "GEO", 3),
        new("Information Technology", "ICT", 3),
        new("Physical Education", "PE", 2)
    ];

    public static async Task SeedAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultSchoolAsync(db, cancellationToken);
        await SeedGradesAsync(db, logger, cancellationToken);
        await SeedSectionsAsync(db, cancellationToken);
        await SeedGradeSectionLinksAsync(db, cancellationToken);
        await SeedSchoolGradeSectionsAsync(db, cancellationToken);
        await SeedSubjectsAsync(db, cancellationToken);
    }

    private static async Task EnsureDefaultSchoolAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var existingSchool = await db.Schools
            .FirstOrDefaultAsync(school => school.Id == School.DefaultSchoolId, cancellationToken);
        if (existingSchool is not null)
        {
            if (string.IsNullOrWhiteSpace(existingSchool.Slug))
            {
                existingSchool.Slug = $"school-management-system-{existingSchool.Id.ToString("N")[..8]}";
            }

            if (string.IsNullOrWhiteSpace(existingSchool.Code))
            {
                existingSchool.Code = "DEFAULT";
            }

            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        db.Schools.Add(new School
        {
            Id = School.DefaultSchoolId,
            Name = "School Management System",
            Slug = "school-management-system",
            Code = "DEFAULT",
            ContactEmail = string.Empty,
            ContactPhone = string.Empty,
            Address = string.Empty,
            SupportedGrades = Grades.Select(grade => grade.Name).ToArray(),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedGradesAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var existingGrades = await db.GradeLevels.ToListAsync(cancellationToken);
        var byLevel = existingGrades.ToDictionary(grade => grade.Level);

        foreach (var definition in Grades)
        {
            if (byLevel.TryGetValue(definition.Level, out var existing))
            {
                var legacyName = (definition.Level, existing.Name) switch
                {
                    (-2, "KG") => "KG 1",
                    (-1, "LKG") => "KG 2",
                    (0, "UKG") => "KG 3",
                    _ => null
                };

                if (legacyName is not null)
                {
                    existing.Name = legacyName;
                }

                continue;
            }

            if (existingGrades.Any(grade => grade.Name == definition.Name))
            {
                throw new InvalidOperationException(
                    $"Grade '{definition.Name}' already exists at a different level.");
            }

            var grade = new GradeLevel
            {
                Name = definition.Name,
                Level = definition.Level,
                Description = definition.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.GradeLevels.Add(grade);
            existingGrades.Add(grade);
            byLevel.Add(definition.Level, grade);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Ensured {GradeCount} required grade levels.", Grades.Length);
    }

    private static async Task SeedSectionsAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var existingCodes = await db.Sections
            .Select(section => section.Code)
            .ToListAsync(cancellationToken);
        var codeSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (code, index) in SectionCodes.Select((code, index) => (code, index)))
        {
            var name = SectionNames[index];
            if (codeSet.Contains(code))
            {
                continue;
            }

            db.Sections.Add(new Section
            {
                Name = name,
                Code = code,
                Capacity = 40,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            codeSet.Add(code);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedGradeSectionLinksAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var gradeIds = await db.GradeLevels.Select(grade => grade.Id).ToListAsync(cancellationToken);
        var sections = await db.Sections
            .Where(section => SectionCodes.Contains(section.Code))
            .Select(section => new { section.Id, section.Code })
            .ToListAsync(cancellationToken);
        var existingLinks = (await db.GradeLevelSections
                .Select(link => new { link.GradeLevelId, link.SectionId })
                .ToListAsync(cancellationToken))
            .Select(link => (link.GradeLevelId, link.SectionId))
            .ToHashSet();

        foreach (var gradeId in gradeIds)
        {
            foreach (var section in sections)
            {
                if (existingLinks.Add((gradeId, section.Id)))
                {
                    db.GradeLevelSections.Add(new GradeLevelSection
                    {
                        GradeLevelId = gradeId,
                        SectionId = section.Id
                    });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSchoolGradeSectionsAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var schoolId = School.DefaultSchoolId;
        var existingLinks = (await db.SchoolGradeSections
                .Where(link => link.SchoolId == schoolId)
                .Select(link => new { link.GradeLevelId, link.SectionId })
                .ToListAsync(cancellationToken))
            .Select(link => (link.GradeLevelId, link.SectionId))
            .ToHashSet();
        var gradeSectionLinks = await db.GradeLevelSections
            .Select(link => new { link.GradeLevelId, link.SectionId })
            .ToListAsync(cancellationToken);

        db.SchoolGradeSections.AddRange(gradeSectionLinks
            .Where(link => existingLinks.Add((link.GradeLevelId, link.SectionId)))
            .Select(link => new SchoolGradeSection
            {
                SchoolId = schoolId,
                GradeLevelId = link.GradeLevelId,
                SectionId = link.SectionId
            }));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSubjectsAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var grades = await db.GradeLevels
            .OrderBy(grade => grade.Level)
            .ToListAsync(cancellationToken);
        var existingSubjects = await db.Subjects
            .Select(subject => new { subject.GradeLevelId, subject.SubjectName, subject.Code })
            .ToListAsync(cancellationToken);
        var existingByGradeAndName = existingSubjects
            .Select(subject => (subject.GradeLevelId, subject.SubjectName))
            .ToHashSet();
        var existingCodes = existingSubjects
            .Select(subject => subject.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var grade in grades)
        {
            foreach (var definition in Subjects)
            {
                if (existingByGradeAndName.Contains((grade.Id, definition.Name)))
                {
                    continue;
                }

                var level = grade.Level.ToString(CultureInfo.InvariantCulture);
                var code = $"{definition.CodePrefix}-{level}";
                if (existingCodes.Contains(code))
                {
                    code = $"{definition.CodePrefix}-G{grade.Id}";
                }

                if (!existingCodes.Add(code))
                {
                    throw new InvalidOperationException(
                        $"Could not generate a unique code for '{definition.Name}' in '{grade.Name}'.");
                }

                db.Subjects.Add(new Subject
                {
                    SubjectName = definition.Name,
                    Code = code,
                    GradeLevelId = grade.Id,
                    Description = $"{definition.Name} for {grade.Name}",
                    CreditHours = definition.CreditHours,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
