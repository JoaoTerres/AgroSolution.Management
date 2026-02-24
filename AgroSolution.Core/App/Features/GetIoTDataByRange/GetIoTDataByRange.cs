using AgroSolution.Core.App.Common;
using AgroSolution.Core.App.DTO;
using AgroSolution.Core.Domain.Interfaces;

namespace AgroSolution.Core.App.Features.GetIoTDataByRange;

public interface IGetIoTDataByRange
{
    Task<Result<IEnumerable<IoTDataResponseDto>>> ExecuteAsync(
        Guid plotId, DateTime from, DateTime to);
}

public sealed class GetIoTDataByRange : IGetIoTDataByRange
{
    private readonly IIoTDataRepository _repo;

    public GetIoTDataByRange(IIoTDataRepository repo) => _repo = repo;

    public async Task<Result<IEnumerable<IoTDataResponseDto>>> ExecuteAsync(
        Guid plotId, DateTime from, DateTime to)
    {
        if (plotId == Guid.Empty)
            return Result<IEnumerable<IoTDataResponseDto>>.Fail("PlotId inválido.");

        if (from >= to)
            return Result<IEnumerable<IoTDataResponseDto>>.Fail(
                "A data inicial deve ser anterior à data final.");

        if ((to - from).TotalDays > 90)
            return Result<IEnumerable<IoTDataResponseDto>>.Fail(
                "O intervalo máximo para consulta é de 90 dias.");

        var data = await _repo.GetByPlotIdAndDateRangeAsync(plotId, from, to);
        return Result<IEnumerable<IoTDataResponseDto>>.Ok(data.Select(IoTDataResponseDto.From));
    }
}
