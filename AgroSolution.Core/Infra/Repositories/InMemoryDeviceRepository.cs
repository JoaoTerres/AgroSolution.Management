using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AgroSolution.Core.Domain.Interfaces;

namespace AgroSolution.Core.Infra.Repositories;

/// <summary>
/// Implementação simples em memória de IDeviceRepository para testes locais.
/// Mapeia deviceId -> PlotId usando um dicionário interno.
/// </summary>
public class InMemoryDeviceRepository : IDeviceRepository
{
    private readonly ConcurrentDictionary<string, Guid> _map = new();

    public InMemoryDeviceRepository()
    {
        // Seed com um exemplo conhecido para testes
        _map["agri-sensor-node-042"] = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
    }

    public Task<Guid?> GetPlotIdByDeviceAsync(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return Task.FromResult<Guid?>(null);

        if (_map.TryGetValue(deviceId, out var plotId))
            return Task.FromResult<Guid?>(plotId);

        return Task.FromResult<Guid?>(null);
    }

    // Método auxiliar para registrar mapeamentos em runtime (opcional)
    public void Register(string deviceId, Guid plotId)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return;
        _map[deviceId] = plotId;
    }
}
