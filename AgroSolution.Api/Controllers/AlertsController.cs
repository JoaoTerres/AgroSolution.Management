using AgroSolution.Core.App.Features.GetAlerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolution.Api.Controllers;

/// <summary>
/// Gerencia alertas agronômicos gerados pelo motor de regras.
/// </summary>
[Route("api/alerts")]
[ApiController]
[Authorize]
public class AlertsController(IGetAlerts getAlerts) : BaseController
{
    /// <summary>
    /// Retorna todos os alertas de um talhão, mais recentes primeiro.
    /// </summary>
    /// <param name="plotId">ID do talhão.</param>
    /// <returns>Lista de alertas (ativos e resolvidos).</returns>
    /// <response code="200">Lista retornada com sucesso (pode ser vazia).</response>
    /// <response code="400">PlotId inválido.</response>
    [HttpGet("{plotId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByPlotId(Guid plotId)
    {
        var result = await getAlerts.ExecuteAsync(plotId);
        return CustomResponse(result);
    }
}
