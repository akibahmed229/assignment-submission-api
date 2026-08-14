using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

public class SchoolClassController(ISchoolClassService service) : BaseApiController
{
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SchoolClassResponseDto>> Create(CreateSchoolClassDto dto) => Ok(await service.CreateAsync(dto));

    [HttpGet]
    // No role restriction beyond [Authorize] on the base -- Teachers and
    // Students both legitimately need to see the list of classes (e.g. to
    // populate a dropdown, or to know which class they belong to).
    public async Task<ActionResult<List<SchoolClassResponseDto>>> GetAll() => Ok(await service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SchoolClassResponseDto>> GetByID(Guid id) => Ok(await service.GetByIdAsync(id));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
