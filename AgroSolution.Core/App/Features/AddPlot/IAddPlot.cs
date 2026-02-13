using AgroSolution.Core.App.Common;
using AgroSolution.Core.App.DTO;

namespace AgroSolution.Core.App.Features.AddPlot;

public interface IAddPlot
{
    Task<Result<Guid>> ExecuteAsync(CreatePlotDto dto);
}