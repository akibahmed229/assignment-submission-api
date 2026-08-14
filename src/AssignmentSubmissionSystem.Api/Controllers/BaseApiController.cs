using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // default: must be logged in. Individual actions narrow this further with Roles.
public abstract class BaseApiController : ControllerBase;

