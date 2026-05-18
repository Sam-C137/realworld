using Microsoft.AspNetCore.Mvc;

namespace RealWorldApi.Core.Abstractions;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "v1")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public abstract class BaseController: Controller;