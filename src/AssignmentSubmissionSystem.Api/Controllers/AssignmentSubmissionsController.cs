using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

[Route("api/assignments/{assignmentId:guid}/submissions")]
public class AssignmentSubmissionsController(ISubmissionService service) : BaseApiController
{
    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<SubmissionResponseDto>> Submit(Guid assignmentId, CreateSubmissionDto dto) =>
        Ok(await service.SubmitAsync(assignmentId, dto));

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<List<SubmissionResponseDto>>> GetForAssignment(Guid assignmentId) => Ok(await service.GetForAssignmentAsync(assignmentId));
}
