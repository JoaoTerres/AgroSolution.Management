using AgroSolution.Core.App.DTO;
using AgroSolution.Core.App.Features.AddPlot;
using AgroSolution.Core.App.Features.GetByIdPlot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolution.Api.Controllers;

[Authorize]
[Route("api/plots")]
public class PlotController(
    IAddPlot addPlot, 
    IGetByIdPlot getByIdPlot) : BaseController
{
    [HttpPost]
    public async Task<IActionResult> AddPlot([FromBody] CreatePlotDto dto)
    {
        var result = await addPlot.ExecuteAsync(dto);
        return CustomResponse(result);
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await getByIdPlot.ExecuteAsync(id);        
        return CustomResponse(result);
    }
}