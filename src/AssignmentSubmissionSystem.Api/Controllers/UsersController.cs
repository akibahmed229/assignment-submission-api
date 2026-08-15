using AssignmentSubmissionSystem.Api.Data;
using AssignmentSubmissionSystem.Api.Exceptions;
using AssignmentSubmissionSystem.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Api.Controllers;

public record UpdateUserStatusDto(bool IsActive);

public record UserSummaryDto(Guid Id, string FullName, string Email, Role Role, bool IsActive);

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
            .Select(u => new UserSummaryDto(u.Id, u.FullName, u.Email, u.Role, u.IsActive))
            .ToListAsync();

        return Ok(users);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<UserSummaryDto>> UpdateStatus(Guid id, UpdateUserStatusDto dto)
    {
        var user = await db.Users.FindAsync(id) ??
            throw new NotFoundException($"User {id} was not found.");

        user.IsActive = dto.IsActive;
        await db.SaveChangesAsync();

        return Ok(new UserSummaryDto(user.Id, user.FullName, user.Email, user.Role, user.IsActive));
    }
}
