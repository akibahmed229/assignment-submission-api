using System.Security.Claims;
using AssignmentSubmissionSystem.Api.Models.Enums;

namespace AssignmentSubmissionSystem.Api.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
    Role Role { get; }
}

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ??
        throw new InvalidOperationException("No HttpContext available.");

    public Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ?
        id : throw new UnauthorizedAccessException("Invalid or missing user id claim.");

    public Role Role => Enum.TryParse<Role>(User.FindFirstValue(ClaimTypes.Role), out var role)
        ?
        role : throw new UnauthorizedAccessException("Invalid or missing role claim.");
}
