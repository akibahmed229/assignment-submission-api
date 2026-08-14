namespace AssignmentSubmissionSystem.Api.Models.Entities;

/// <summary>One row = "this Student belongs to this SchoolClass." A student can only ever see/submit assignments for classes they're enrolled in -- this table is what your authorization checks join against.</summary>
public class StudentEnrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudentId { get; set; }
    public User Student { get; set; } = default!;

    public Guid SchoolClassId { get; set; }
    public SchoolClass SchoolClass { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
