using AgroSolution.Core.App.DTO;
using AgroSolution.Core.App.Features.CreateProperty;
using AgroSolution.Core.App.Features.GetProperties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolution.Api.Controllers;

[Authorize] 
[Route("api/properties")]
public class PropertyController(
    ICreateProperty createProperty,
    IGetProperties getProperties) : BaseController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePropertyDto dto)
    {
        var result = await createProperty.ExecuteAsync(dto, AppUserId);
        return CustomResponse(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await getProperties.ExecuteAsync(AppUserId);
        return CustomResponse(result);
    }
}