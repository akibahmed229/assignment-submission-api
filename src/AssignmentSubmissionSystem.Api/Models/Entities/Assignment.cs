using AssignmentSubmissionSystem.Api.Models.Enums;

namespace AssignmentSubmissionSystem.Api.Models.Entities;

public class Assignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime Deadline { get; set; }
    public int MaxMarks { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;

    public Guid TeacherId { get; set; } // who created it
    public User Teacher { get; set; } = default!;

    public Guid SchoolClassId { get; set; }
    public SchoolClass SchoolClass { get; set; } = default!;

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }

    public ICollection<Submission> Submissions { get; set; } = [];
}
