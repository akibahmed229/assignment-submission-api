using System.ComponentModel.DataAnnotations;

namespace AssignmentSubmissionSystem.Api.Models.Dtos;

public class CreateSubjectDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = default!;

    [MaxLength(20)]
    public string? Code { get; set; }
}

public record SubjectResponseDto(Guid Id, string Name, string? Code, DateTime CreatedAt);
