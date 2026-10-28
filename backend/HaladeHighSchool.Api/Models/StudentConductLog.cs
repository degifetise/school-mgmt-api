namespace HaladeHighSchool.Api.Models;

public static class StudentConductTypes
{
    public const string Merit = "Merit";
    public const string Demerit = "Demerit";
    public const string DisciplinaryNote = "Disciplinary Note";
    public const string GeneralBehavioralLog = "General Behavioral Log";

    public static readonly string[] All =
    [
        Merit,
        Demerit,
        DisciplinaryNote,
        GeneralBehavioralLog,
    ];
}

public class StudentConductLog
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public string RecordedByUserId { get; set; } = string.Empty;

    public string RecordedByName { get; set; } = string.Empty;

    public string RecordedByRole { get; set; } = string.Empty;

    public string Type { get; set; } = StudentConductTypes.GeneralBehavioralLog;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime IncidentDate { get; set; } = DateTime.UtcNow;

    public bool IsVisibleToStudent { get; set; } = true;

    public bool IsVisibleToParent { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Student? Student { get; set; }
}
