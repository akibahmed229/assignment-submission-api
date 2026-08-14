namespace AssignmentSubmissionSystem.Api.Models.Entities;

public class Subject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!; // e.g. "Mathematics"
    public string? Code { get; set; } // e.g. "MATH101", optional
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TeacherAssignment> TeacherAssignments { get; set; } = [];
    public ICollection<Assignment> Assignments { get; set; } = [];
}
