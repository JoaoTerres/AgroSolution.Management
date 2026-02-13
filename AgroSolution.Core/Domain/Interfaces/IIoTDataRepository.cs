using AgroSolution.Core.Domain.Entities;

namespace AgroSolution.Core.Domain.Interfaces;

/// <summary>
/// Interface para repositório de dados IoT
/// </summary>
public interface IIoTDataRepository
{
    /// <summary>
    /// Adiciona um novo registro de dados IoT
    /// </summary>
    Task<bool> AddAsync(IoTData iotData);

    /// <summary>
    /// Obtém um registro por ID
    /// </summary>
    Task<IoTData?> GetByIdAsync(Guid id);

    /// <summary>
    /// Obtém todos os dados de um talhão
    /// </summary>
    Task<IEnumerable<IoTData>> GetByPlotIdAsync(Guid plotId);

    /// <summary>
    /// Obtém dados pendentes de processamento
    /// </summary>
    Task<IEnumerable<IoTData>> GetPendingAsync(int limit = 100);

    /// <summary>
    /// Atualiza o status de processamento
    /// </summary>
    Task<bool> UpdateAsync(IoTData iotData);

    /// <summary>
    /// Obtém dados de um talhão em um período
    /// </summary>
    Task<IEnumerable<IoTData>> GetByPlotIdAndDateRangeAsync(Guid plotId, DateTime startDate, DateTime endDate);
}
