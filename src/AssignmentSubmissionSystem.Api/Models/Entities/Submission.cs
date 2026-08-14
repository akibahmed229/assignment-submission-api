using AssignmentSubmissionSystem.Api.Models.Enums;

namespace AssignmentSubmissionSystem.Api.Models.Entities;

public class Submission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = default!;

    public Guid StudentId { get; set; }
    public User Student { get; set; } = default!;

    public string AnswerText { get; set; } = default!; // or a file URL if you add uploads later
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    public int? Marks { get; set; }
    public string? Feedback { get; set; }
    public Guid? GradedByTeacherId { get; set; }
    public DateTime? GradedAt { get; set; }
}
