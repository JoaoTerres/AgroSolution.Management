using AgroSolution.Core.Domain.Entities;

namespace AgroSolution.Core.Domain.Interfaces;

/// <summary>
/// Repositório para alertas agronômicos.
/// </summary>
public interface IAlertRepository
{
    /// <summary>Persiste um novo alerta.</summary>
    Task<bool> AddAsync(Alert alert);

    /// <summary>Retorna todos os alertas de um talhão, mais recentes primeiro.</summary>
    Task<IEnumerable<Alert>> GetByPlotIdAsync(Guid plotId);

    /// <summary>Retorna o alerta ativo mais recente de um dado tipo para um talhão. Null se não houver.</summary>
    Task<Alert?> GetActiveByPlotIdAndTypeAsync(Guid plotId, AlertType type);

    /// <summary>Atualiza um alerta existente (ex: resolução).</summary>
    Task<bool> UpdateAsync(Alert alert);
}
