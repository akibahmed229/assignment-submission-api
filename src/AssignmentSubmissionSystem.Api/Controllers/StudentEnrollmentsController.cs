using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

public class StudentEnrollmentsController(IStudentEnrollmentService service) : BaseApiController
{
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StudentEnrollmentResponseDto>> Create(CreateStudentEnrollmentDto dto) => Ok(await service.CreateAsync(dto));

    [HttpGet("class/{schoolClassId:guid}")]
    [Authorize(Roles = "Admin,Teacher")] // Teachers need class rosters; Students don't need everyone else's enrollment
    public async Task<ActionResult<List<StudentEnrollmentResponseDto>>> GetByClass(Guid schoolClassId) => Ok(await service.GetByClassAsync(schoolClassId));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
