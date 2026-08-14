using System.ComponentModel.DataAnnotations;

namespace AssignmentSubmissionSystem.Api.Models.Dtos;

public class CreateTeacherAssignmentDto
{
    [Required]
    public Guid TeacherId { get; set; }

    [Required]
    public Guid SchoolClassId { get; set; }

    [Required]
    public Guid SubjectId { get; set; }
}

public record TeacherAssignmentResponseDto(
        Guid Id,
        Guid TeacherId,
        string TeacherName,
        Guid SchoolClassId,
        string SchoolClassName,
        Guid SubjectId,
        string SubjectName
);
