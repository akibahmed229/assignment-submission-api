using System.ComponentModel.DataAnnotations;

namespace AssignmentSubmissionSystem.Api.Models.Dtos;

public class CreateStudentEnrollmentDto
{
    [Required]
    public Guid StudentId { get; set; }

    [Required]
    public Guid SchoolClassId { get; set; }
}

public record StudentEnrollmentResponseDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid SchoolClassId,
    string SchoolClassName
);
