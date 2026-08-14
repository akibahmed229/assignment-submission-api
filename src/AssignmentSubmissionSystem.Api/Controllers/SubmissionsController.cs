using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

public class SubmissionsController(ISubmissionService service) : BaseApiController
{
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<SubmissionResponseDto>> Update(Guid id, CreateSubmissionDto dto) => Ok(await service.SubmitAsync(id, dto));

    [HttpPost("{id:guid}/grade")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<SubmissionResponseDto>> Grade(Guid id, GradeSubmissionDto dto) => Ok(await service.GradeAsync(id, dto));

    [HttpGet("mine")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<List<SubmissionResponseDto>>> GetMine() => Ok(await service.GetMineAsync());
}
