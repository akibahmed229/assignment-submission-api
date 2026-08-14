using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

public class AssignmentsController(IAssignmentService service) : BaseApiController
{
    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult<AssignmentResponseDto>> Create(CreateAssignmentDto dto) => Ok(await service.CreateAsync(dto));
    // Created as Draft by default (see Assignment entity). Publish is a
    // separate step -- keeps "still writing it" separate from "students
    // can see it now," which the brief explicitly calls out as a feature.

    [HttpPatch("{id:guid}/publish")]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult<AssignmentResponseDto>> Publish(Guid id) => Ok(await service.PublishAsync(id));

    [HttpGet("mine")]
    // Intentionally no Roles restriction -- the SERVICE decides what "mine"
    // means per role (Admin: everything, Teacher: what they created,
    // Student: what's published for their enrolled classes). Putting a
    // Roles list here would be redundant with logic that already lives
    // one layer down.
    public async Task<ActionResult<List<AssignmentResponseDto>>> GetMine() => Ok(await service.GetMineAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssignmentResponseDto>> GetById(Guid id) => Ok(await service.GetByIdAsync(id));
    // No Roles attribute here either -- EnsureViewableAsync in the service
    // throws ForbiddenAccessException if this student/teacher shouldn't
    // see this specific assignment. The controller doesn't need to know
    // the rule, just that the service enforces one.

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
