using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

public class TeacherAssignmentsController(ITeacherAssignmentService service, ICurrentUserService currentUser) : BaseApiController
{

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TeacherAssignmentResponseDto>> Create(CreateTeacherAssignmentDto dto) => Ok(await service.CreateAsync(dto));

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<TeacherAssignmentResponseDto>>> GetAll() => Ok(await service.GetAllAsync());

    [HttpGet("mine")]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult<List<TeacherAssignmentResponseDto>>> GetMine() => Ok(await service.GetMineAsync(currentUser.UserId));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
