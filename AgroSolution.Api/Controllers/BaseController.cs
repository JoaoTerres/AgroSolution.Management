using System.Security.Claims;
using AgroSolution.Core.App.Common;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolution.Api.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected Guid AppUserId => User.Identity?.IsAuthenticated == true
        ? Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!)
        : Guid.Empty;

    protected IActionResult CustomResponse<T>(Result<T> result)
    {
        if (result.Success)
        {
            return Ok(new
            {
                success = true,
                data = result.Data
            });
        }

        // Mapeamento de erros de negócio para Status Codes HTTP
        return result.ErrorMessage switch
        {
            "Propriedade não encontrada." => NotFound(new { success = false, errors = new[] { result.ErrorMessage } }),
            "A propriedade informada não existe." => NotFound(new { success = false, errors = new[] { result.ErrorMessage } }),
            "Identificação do produtor inválida." => Unauthorized(new { success = false, errors = new[] { result.ErrorMessage } }),
            _ => BadRequest(new { success = false, errors = new[] { result.ErrorMessage } })
        };
    }
}