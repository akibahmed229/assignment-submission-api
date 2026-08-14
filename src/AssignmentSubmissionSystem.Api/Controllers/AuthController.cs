using AssignmentSubmissionSystem.Api.Models.Dtos;
using AssignmentSubmissionSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        => Ok(await authService.RegisterAsync(dto));
    // [ApiController] already returned 400 automatically if `dto` failed
    // its DataAnnotations checks -- this line never even runs in that case.

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        => Ok(await authService.LoginAsync(dto));
}
