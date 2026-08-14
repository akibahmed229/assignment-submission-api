namespace AssignmentSubmissionSystem.Api.Models.Enums;

// Submitted -> Graded is the normal path. Late is set by the service layer
// when SubmittedAt > Assignment.Deadline, not chosen by the student.
public enum SubmissionStatus
{
    Submitted,
    Late,
    Graded
}
