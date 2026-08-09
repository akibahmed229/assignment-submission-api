using System.ComponentModel.DataAnnotations;
using AssignmentSubmissionSystem.Api.Models.Enums;

namespace AssignmentSubmissionSystem.Api.Models.Dtos;

public class RegisterDto
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = default!;

    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required, MaxLength(200)]
    public string Password { get; set; } = default!;

    [Required]
    public Role Role { get; set; }
}
