using System.Text.Json;
using AgroSolution.Core.App.DTO;
using AgroSolution.Core.Domain.Entities;
using AgroSolution.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgroSolution.Core.App.Features.AlertEngine;

/// <summary>
/// Serviço que avalia regras de alerta agronômico após cada lote de dados IoT
/// processado pelo ConsumerWorker.
///
/// Regras implementadas:
///   DroughtRule   — umidade &lt; 30 % em TODAS as leituras dos últimos 24 h
/// </summary>
public interface IAlertEngineService
{
    /// <summary>
    /// Avalia todas as regras pertinentes para o talhão e tipo de dispositivo
    /// que acabou de ter dados processados.
    /// </summary>
    Task EvaluateAsync(Guid plotId, IoTDeviceType deviceType, CancellationToken ct = default);
}

public sealed class AlertEngineService : IAlertEngineService
{
    // Threshold configurável — candidato a IOptions no futuro
    private const float HumidityDroughtThreshold = 30f;
    private const int   DroughtWindowHours        = 24;
    private const int   DroughtMinReadings        = 2; // mínimo de leituras para disparar

    private readonly IIoTDataRepository  _iotRepo;
    private readonly IAlertRepository    _alertRepo;
    private readonly ILogger<AlertEngineService> _logger;

    public AlertEngineService(
        IIoTDataRepository iotRepo,
        IAlertRepository   alertRepo,
        ILogger<AlertEngineService> logger)
    {
        _iotRepo   = iotRepo;
        _alertRepo = alertRepo;
        _logger    = logger;
    }

    public async Task EvaluateAsync(Guid plotId, IoTDeviceType deviceType, CancellationToken ct = default)
    {
        // Só avalia regras de seca quando há dados de umidade
        if (deviceType is IoTDeviceType.HumiditySensor or IoTDeviceType.WeatherStationNode)
        {
            await EvaluateDroughtRuleAsync(plotId, ct);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DroughtRule
    // ──────────────────────────────────────────────────────────────────────────

    private async Task EvaluateDroughtRuleAsync(Guid plotId, CancellationToken ct)
    {
        var windowStart = DateTime.UtcNow.AddHours(-DroughtWindowHours);
        var windowEnd   = DateTime.UtcNow;

        // Busca todas as leituras de umidade nas últimas 24h
        var readings = (await _iotRepo.GetByPlotIdAndDateRangeAsync(plotId, windowStart, windowEnd))
            .Where(d => d.DeviceType is IoTDeviceType.HumiditySensor or IoTDeviceType.WeatherStationNode
                     && d.ProcessingStatus == IoTProcessingStatus.Processed)
            .ToList();

        if (readings.Count < DroughtMinReadings)
        {
            // Dados insuficientes — não concluir nada
            return;
        }

        // Extrai valores de umidade de cada leitura
        var humidityValues = readings
            .Select(r => TryExtractHumidity(r.RawData))
            .Where(h => h.HasValue)
            .Select(h => h!.Value)
            .ToList();

        if (humidityValues.Count < DroughtMinReadings)
            return;

        var allBelowThreshold = humidityValues.All(h => h < HumidityDroughtThreshold);
        var existingAlert     = await _alertRepo.GetActiveByPlotIdAndTypeAsync(plotId, AlertType.Drought);

        if (allBelowThreshold && existingAlert is null)
        {
            var avg = humidityValues.Average();
            var alert = new Alert(
                plotId,
                AlertType.Drought,
                $"Seca detectada: umidade média de {avg:F1}% nas últimas {DroughtWindowHours}h " +
                $"(limiar: {HumidityDroughtThreshold}%). " +
                $"Leituras analisadas: {humidityValues.Count}.");

            await _alertRepo.AddAsync(alert);
            _logger.LogWarning("AlertEngine: DROUGHT alert created for plot {PlotId} — avg humidity {Avg:F1}%",
                plotId, avg);
        }
        else if (!allBelowThreshold && existingAlert is not null)
        {
            existingAlert.Resolve();
            await _alertRepo.UpdateAsync(existingAlert);
            _logger.LogInformation("AlertEngine: DROUGHT alert resolved for plot {PlotId}", plotId);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static float? TryExtractHumidity(string rawData)
    {
        try
        {
            using var doc  = JsonDocument.Parse(rawData);
            var       root = doc.RootElement;

            // HumiditySensor: {"value": 42.5, "unit": "%"}
            if (root.TryGetProperty("value", out var val) && val.TryGetSingle(out var h))
                return h;

            // WeatherStationNode: {"telemetry": {"humidity": 42.5, ...}}
            if (root.TryGetProperty("telemetry", out var telemetry) &&
                telemetry.TryGetProperty("humidity", out var hProp) &&
                hProp.TryGetSingle(out var h2))
                return h2;

            return null;
        }
        catch
        {
            return null;
        }
    }
}
