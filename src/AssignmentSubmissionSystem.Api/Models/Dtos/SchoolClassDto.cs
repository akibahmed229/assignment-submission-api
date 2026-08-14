using System.ComponentModel.DataAnnotations;

namespace AssignmentSubmissionSystem.Api.Models.Dtos;

public class CreateSchoolClassDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = default!;
}

public record SchoolClassResponseDto(Guid Id, string Name, DateTime CreatedAt);
