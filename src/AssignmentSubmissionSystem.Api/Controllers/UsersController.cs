using AssignmentSubmissionSystem.Api.Data;
using AssignmentSubmissionSystem.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Api.Controllers;

public record UserSummaryDto(Guid Id, string FullName, string Email, Role Role);

[Authorize(Roles = "Admin")]
public class UsersController(AppDbContext db) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<List<UserSummaryDto>>> GetAll([FromQuery] Role? role)
    {
        var query = db.Users.AsQueryable();
        if (role.HasValue) query = query.Where(u => u.Role == role.Value);

        var users = await query
            .OrderBy(u => u.FullName)
            .Select(u => new UserSummaryDto(u.Id, u.FullName, u.Email, u.Role))
            .ToListAsync();

        return Ok(users);
    }
}
