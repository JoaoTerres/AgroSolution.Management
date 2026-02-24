using AgroSolution.Identity.App.Common;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolution.Identity.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected IActionResult CustomResponse<T>(Result<T> result)
    {
        if (result.Success)
            return Ok(new { success = true, data = result.Data });

        return result.ErrorMessage switch
        {
            "E-mail ou senha inválidos." => Unauthorized(new { success = false, errors = new[] { result.ErrorMessage } }),
            _                            => BadRequest(new  { success = false, errors = new[] { result.ErrorMessage } })
        };
    }
}
