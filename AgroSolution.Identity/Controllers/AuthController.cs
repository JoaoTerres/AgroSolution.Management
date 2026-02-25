using AgroSolution.Identity.App.DTO;
using AgroSolution.Identity.App.Features.Login;
using AgroSolution.Identity.App.Features.RegisterProducer;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolution.Identity.Controllers;

[Route("api/auth")]
public class AuthController(
    IRegisterProducer registerProducer,
    ILogin            login) : BaseController
{
    /// <summary>Registra um novo produtor rural.</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterProducerDto dto)
    {
        var result = await registerProducer.ExecuteAsync(dto);
        return CustomResponse(result);
    }

    /// <summary>Autentica um produtor e retorna o JWT Bearer token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await login.ExecuteAsync(dto);
        return CustomResponse(result);
    }
}
