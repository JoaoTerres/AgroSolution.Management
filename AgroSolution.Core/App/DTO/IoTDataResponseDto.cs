using AgroSolution.Core.Domain.Entities;
using AgroSolution.Core.App.DTO;

namespace AgroSolution.Core.App.DTO;

/// <summary>DTO de resposta para o endpoint de dashboard de dados IoT.</summary>
public sealed class IoTDataResponseDto
{
    public Guid   Id              { get; init; }
    public Guid   PlotId          { get; init; }
    public string DeviceType      { get; init; } = string.Empty;
    public string RawData         { get; init; } = string.Empty;
    public DateTime DeviceTimestamp { get; init; }
    public DateTime ReceivedAt    { get; init; }
    public string ProcessingStatus { get; init; } = string.Empty;

    public static IoTDataResponseDto From(IoTData d) => new()
    {
        Id               = d.Id,
        PlotId           = d.PlotId,
        DeviceType       = d.DeviceType.ToString(),
        RawData          = d.RawData,
        DeviceTimestamp  = d.DeviceTimestamp,
        ReceivedAt       = d.ReceivedAt,
        ProcessingStatus = d.ProcessingStatus.ToString()
    };
}
