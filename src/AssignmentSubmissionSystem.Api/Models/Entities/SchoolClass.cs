namespace AssignmentSubmissionSystem.Api.Models.Entities;

public class SchoolClass
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!; // e.g. "Grade 10 - A", "BSc CSE - 3rd Year"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<StudentEnrollment> Enrollments { get; set; } = [];
    public ICollection<TeacherAssignment> TeacherAssignments { get; set; } = [];
    public ICollection<Assignment> Assignments { get; set; } = [];
}
