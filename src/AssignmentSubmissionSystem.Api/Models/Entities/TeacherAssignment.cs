namespace AssignmentSubmissionSystem.Api.Models.Entities;

/// <summary>One row = "this Teacher teaches this Subject for this SchoolClass." Enforced unique per (TeacherId, SchoolClassId, SubjectId) at the DB level -- see AppDbContext.</summary>
public class TeacherAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TeacherId { get; set; }
    public User Teacher { get; set; } = default!;

    public Guid SchoolClassId { get; set; }
    public SchoolClass SchoolClass { get; set; } = default!;

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
