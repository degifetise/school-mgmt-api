namespace HaladeHighSchool.Api.Models;

public class GradeLevelSection
{
    public int GradeLevelId { get; set; }

    public int SectionId { get; set; }

    public GradeLevel? GradeLevel { get; set; }

    public Section? Section { get; set; }

    public ICollection<TeacherSectionAssignment> TeacherAssignments { get; set; } = new List<TeacherSectionAssignment>();
}
