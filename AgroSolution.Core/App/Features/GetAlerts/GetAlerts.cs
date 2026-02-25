using AgroSolution.Core.App.Common;
using AgroSolution.Core.App.DTO;
using AgroSolution.Core.Domain.Interfaces;

namespace AgroSolution.Core.App.Features.GetAlerts;

public interface IGetAlerts
{
    Task<Result<IEnumerable<AlertResponseDto>>> ExecuteAsync(Guid plotId);
}

public sealed class GetAlerts : IGetAlerts
{
    private readonly IAlertRepository _repo;

    public GetAlerts(IAlertRepository repo) => _repo = repo;

    public async Task<Result<IEnumerable<AlertResponseDto>>> ExecuteAsync(Guid plotId)
    {
        if (plotId == Guid.Empty)
            return Result<IEnumerable<AlertResponseDto>>.Fail("PlotId inválido.");

        var alerts = await _repo.GetByPlotIdAsync(plotId);
        return Result<IEnumerable<AlertResponseDto>>.Ok(alerts.Select(AlertResponseDto.From));
    }
}
