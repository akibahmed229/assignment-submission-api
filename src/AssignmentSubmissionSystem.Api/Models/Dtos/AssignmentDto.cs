using System.ComponentModel.DataAnnotations;
using AssignmentSubmissionSystem.Api.Models.Enums;

namespace AssignmentSubmissionSystem.Api.Models.Dtos;

public class CreateAssignmentDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = default!;

    [Required]
    public string Description { get; set; } = default!;

    [Required]
    public DateTime Deadline { get; set; }

    [Range(1, 100)]
    public int MaxMarks { get; set; }

    [Required]
    public Guid SchoolClassId { get; set; }

    [Required]
    public Guid SubjectId { get; set; }
}

public record AssignmentResponseDto(
        Guid Id,
        string Title,
        string Description,
        int MaxMarks,
        DateTime Deadline,
        AssignmentStatus Status,
        Guid TeacherId,
        Guid SchoolClassId,
        Guid SubjectId,
        DateTime CreatedAt
);
