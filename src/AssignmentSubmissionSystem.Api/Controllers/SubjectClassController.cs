using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

public class SubjectController(ISubjectService service) : BaseApiController
{
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SubjectResponseDto>> Create(CreateSubjectDto dto) => Ok(await service.CreateAsync(dto));

    [HttpGet]
    public async Task<ActionResult<List<SubjectResponseDto>>> GetAll() => Ok(await service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SubjectResponseDto>> GetById(Guid id) => Ok(await service.GetByIdAsync(id));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
