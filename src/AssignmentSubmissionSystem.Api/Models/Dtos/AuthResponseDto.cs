
using AssignmentSubmissionSystem.Api.Models.Enums;

namespace AssignmentSubmissionSystem.Api.Models.Dtos;

public record AuthResponseDto(
        Guid UserId,
        string FullName,
        string Email,
        Role Role,
        string Token
);
