using AgroSolution.Core.App.DTO;
using AgroSolution.Core.App.Features.AddPlot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolution.Api.Controllers;

[Authorize]
[Route("api/plots")]
public class PlotController(IAddPlot addPlot) : BaseController
{
    [HttpPost]
    public async Task<IActionResult> AddPlot([FromBody] CreatePlotDto dto)
    {
        var result = await addPlot.ExecuteAsync(dto);
        return CustomResponse(result);
    }
}