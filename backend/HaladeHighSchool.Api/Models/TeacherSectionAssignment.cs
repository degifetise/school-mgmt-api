namespace HaladeHighSchool.Api.Models;

public class TeacherSectionAssignment
{
    public int TeacherId { get; set; }

    public int GradeLevelId { get; set; }

    public int SectionId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public Teacher? Teacher { get; set; }

    public GradeLevelSection? GradeLevelSection { get; set; }
}
