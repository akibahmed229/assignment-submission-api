using System.ComponentModel.DataAnnotations;
using AssignmentSubmissionSystem.Api.Models.Enums;

namespace AssignmentSubmissionSystem.Api.Models.Dtos;

public class CreateSubmissionDto
{
    [Required]
    public string AnswerText { get; set; } = default!;
}

public class GradeSubmissionDto
{
    [Required, Range(1, 1000)]
    public int Marks { get; set; }

    public string? Feedback { get; set; }
}

public record SubmissionResponseDto(
        Guid Id,
        Guid AssignmentId,
        Guid StudentId,
        string AnswerText,
        DateTime SubmittedAt,
        SubmissionStatus Status,
        int? Marks,
        string? Feedback,
        DateTime? GradedAt
);
