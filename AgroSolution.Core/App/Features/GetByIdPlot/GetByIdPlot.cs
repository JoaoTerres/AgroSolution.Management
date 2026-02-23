using AgroSolution.Core.App.Common;
using AgroSolution.Core.App.DTO;
using AgroSolution.Core.Domain.Interfaces;

namespace AgroSolution.Core.App.Features.GetByIdPlot;

public class GetByIdPlot : IGetByIdPlot
{
    private readonly IPropertyRepository _repository;

    public GetByIdPlot(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PlotResponseDto>> ExecuteAsync(Guid plotId)
    {
        var plot = await _repository.GetPlotByIdAsync(plotId);

        if (plot == null)
            return Result<PlotResponseDto>.Fail("Talhão não encontrado.");


        var response = new PlotResponseDto(
            plot.Id,
            plot.Name,
            plot.CropType,
            plot.AreaInHectares
        );

        return Result<PlotResponseDto>.Ok(response);
    }
}