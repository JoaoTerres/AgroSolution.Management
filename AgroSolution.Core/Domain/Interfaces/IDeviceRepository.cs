using System;
using System.Threading.Tasks;

namespace AgroSolution.Core.Domain.Interfaces;

/// <summary>
/// Interface para repositório de dispositivos físicos (lookup deviceId -> plot)
/// </summary>
public interface IDeviceRepository
{
    /// <summary>
    /// Retorna o PlotId associado ao deviceId, ou null se não existir
    /// </summary>
    Task<Guid?> GetPlotIdByDeviceAsync(string deviceId);
}
