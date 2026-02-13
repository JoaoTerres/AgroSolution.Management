using AgroSolution.Core.App.Common;
using AgroSolution.Core.App.DTO;
using AgroSolution.Core.Domain.Interfaces;

namespace AgroSolution.Core.App.Features.GetProperties;

public class GetProperties(IPropertyRepository repository) : IGetProperties
{
    public async Task<Result<IEnumerable<PropertyResponseDto>>> ExecuteAsync(Guid producerId)
    {
        if (producerId == Guid.Empty)
            return Result<IEnumerable<PropertyResponseDto>>.Fail("Identificação do produtor inválida.");

        var properties = await repository.GetByProducerIdAsync(producerId);
        
        var response = properties.Select(p => new PropertyResponseDto(
            p.Id, 
            p.Name, 
            p.Location, 
            p.Plots.Select(plt => new PlotResponseDto(
                plt.Id, 
                plt.Name, 
                plt.CropType, 
                plt.AreaInHectares)
            ).ToList()
        ));

        return Result<IEnumerable<PropertyResponseDto>>.Ok(response);
    }
}