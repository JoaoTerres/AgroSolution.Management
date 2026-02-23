using AgroSolution.Core.App.Common;
using AgroSolution.Core.App.DTO;
using AgroSolution.Core.Domain.Interfaces;

namespace AgroSolution.Core.App.Features.AddPlot;

public class AddPlot(IPropertyRepository repository) : IAddPlot
{
    public async Task<Result<Guid>> ExecuteAsync(CreatePlotDto dto)
    {
        var property = await repository.GetByIdAsync(dto.PropertyId);
    
        if (property == null) 
            return Result<Guid>.Fail("A propriedade informada não existe.");
        
        property.AddPlot(dto.Name, dto.CropType, dto.Area);
        
        
        
        await repository.SaveChangesAsync();

        return Result<Guid>.Ok(property.Plots.Last().Id);
    }
}