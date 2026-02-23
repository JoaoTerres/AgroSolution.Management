using AgroSolution.Core.App.Common;
using AgroSolution.Core.App.DTO;

namespace AgroSolution.Core.App.Features.GetByIdPlot;

public interface IGetByIdPlot
{
    Task<Result<PlotResponseDto>> ExecuteAsync(Guid plotId);

}