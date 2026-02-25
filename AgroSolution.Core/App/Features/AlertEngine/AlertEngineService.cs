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
///   DroughtRule     — umidade &lt; 30 % em TODAS as leituras dos últimos 24 h
///   ExtremeHeatRule — temperatura &gt; 38 °C em TODAS as leituras das últimas 6 h (mín. 3)
///   HeavyRainRule   — precipitação acumulada ≥ 50 mm nas últimas 6 h
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
    // Thresholds configuráveis — candidatos a IOptions no futuro

    // DroughtRule
    private const float HumidityDroughtThreshold = 30f;
    private const int   DroughtWindowHours        = 24;
    private const int   DroughtMinReadings        = 2;

    // ExtremeHeatRule
    private const float ExtremeHeatThreshold  = 38f;
    private const int   HeatWindowHours       = 6;
    private const int   HeatMinReadings       = 3;

    // HeavyRainRule
    private const float HeavyRainThresholdMm  = 50f;
    private const int   RainWindowHours       = 6;

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
        if (deviceType is IoTDeviceType.HumiditySensor or IoTDeviceType.WeatherStationNode)
            await EvaluateDroughtRuleAsync(plotId, ct);

        if (deviceType is IoTDeviceType.TemperatureSensor or IoTDeviceType.WeatherStationNode)
            await EvaluateExtremeHeatRuleAsync(plotId, ct);

        if (deviceType is IoTDeviceType.PrecipitationSensor or IoTDeviceType.WeatherStationNode)
            await EvaluateHeavyRainRuleAsync(plotId, ct);
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
    // ExtremeHeatRule
    // ──────────────────────────────────────────────────────────────────────────

    private async Task EvaluateExtremeHeatRuleAsync(Guid plotId, CancellationToken ct)
    {
        var windowStart = DateTime.UtcNow.AddHours(-HeatWindowHours);
        var windowEnd   = DateTime.UtcNow;

        var readings = (await _iotRepo.GetByPlotIdAndDateRangeAsync(plotId, windowStart, windowEnd))
            .Where(d => d.DeviceType is IoTDeviceType.TemperatureSensor or IoTDeviceType.WeatherStationNode
                     && d.ProcessingStatus == IoTProcessingStatus.Processed)
            .ToList();

        if (readings.Count < HeatMinReadings)
            return;

        var tempValues = readings
            .Select(r => TryExtractTemperature(r.RawData))
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .ToList();

        if (tempValues.Count < HeatMinReadings)
            return;

        var allAboveThreshold = tempValues.All(t => t > ExtremeHeatThreshold);
        var existingAlert     = await _alertRepo.GetActiveByPlotIdAndTypeAsync(plotId, AlertType.ExtremeHeat);

        if (allAboveThreshold && existingAlert is null)
        {
            var avg = tempValues.Average();
            var alert = new Alert(
                plotId,
                AlertType.ExtremeHeat,
                $"Calor extremo detectado: temperatura média de {avg:F1}°C nas últimas {HeatWindowHours}h " +
                $"(limiar: {ExtremeHeatThreshold}°C). " +
                $"Leituras analisadas: {tempValues.Count}.");

            await _alertRepo.AddAsync(alert);
            _logger.LogWarning("AlertEngine: EXTREME_HEAT alert created for plot {PlotId} — avg temp {Avg:F1}°C",
                plotId, avg);
        }
        else if (!allAboveThreshold && existingAlert is not null)
        {
            existingAlert.Resolve();
            await _alertRepo.UpdateAsync(existingAlert);
            _logger.LogInformation("AlertEngine: EXTREME_HEAT alert resolved for plot {PlotId}", plotId);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // HeavyRainRule
    // ──────────────────────────────────────────────────────────────────────────

    private async Task EvaluateHeavyRainRuleAsync(Guid plotId, CancellationToken ct)
    {
        var windowStart = DateTime.UtcNow.AddHours(-RainWindowHours);
        var windowEnd   = DateTime.UtcNow;

        var readings = (await _iotRepo.GetByPlotIdAndDateRangeAsync(plotId, windowStart, windowEnd))
            .Where(d => d.DeviceType is IoTDeviceType.PrecipitationSensor or IoTDeviceType.WeatherStationNode
                     && d.ProcessingStatus == IoTProcessingStatus.Processed)
            .ToList();

        if (readings.Count == 0)
            return;

        var precipValues = readings
            .Select(r => TryExtractPrecipitation(r.RawData))
            .Where(p => p.HasValue)
            .Select(p => p!.Value)
            .ToList();

        if (precipValues.Count == 0)
            return;

        var totalPrecip   = precipValues.Sum();
        var existingAlert = await _alertRepo.GetActiveByPlotIdAndTypeAsync(plotId, AlertType.HeavyRain);

        if (totalPrecip >= HeavyRainThresholdMm && existingAlert is null)
        {
            var alert = new Alert(
                plotId,
                AlertType.HeavyRain,
                $"Chuva intensa detectada: precipitação acumulada de {totalPrecip:F1} mm nas últimas {RainWindowHours}h " +
                $"(limiar: {HeavyRainThresholdMm} mm). " +
                $"Leituras analisadas: {precipValues.Count}.");

            await _alertRepo.AddAsync(alert);
            _logger.LogWarning("AlertEngine: HEAVY_RAIN alert created for plot {PlotId} — total precip {Total:F1} mm",
                plotId, totalPrecip);
        }
        else if (totalPrecip < HeavyRainThresholdMm && existingAlert is not null)
        {
            existingAlert.Resolve();
            await _alertRepo.UpdateAsync(existingAlert);
            _logger.LogInformation("AlertEngine: HEAVY_RAIN alert resolved for plot {PlotId}", plotId);
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

    private static float? TryExtractTemperature(string rawData)
    {
        try
        {
            using var doc  = JsonDocument.Parse(rawData);
            var       root = doc.RootElement;

            // TemperatureSensor: {"value": 39.0, "unit": "C"}
            if (root.TryGetProperty("value", out var val) && val.TryGetSingle(out var t))
                return t;

            // WeatherStationNode: {"telemetry": {"temperature_air": 39.0, ...}}
            if (root.TryGetProperty("telemetry", out var telemetry) &&
                telemetry.TryGetProperty("temperature_air", out var tProp) &&
                tProp.TryGetSingle(out var t2))
                return t2;

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static float? TryExtractPrecipitation(string rawData)
    {
        try
        {
            using var doc  = JsonDocument.Parse(rawData);
            var       root = doc.RootElement;

            // PrecipitationSensor: {"value": 55.0, "unit": "mm"}
            if (root.TryGetProperty("value", out var val) && val.TryGetSingle(out var p))
                return p;

            // WeatherStationNode: {"telemetry": {"precipitation_mm": 55.0, ...}}
            if (root.TryGetProperty("telemetry", out var telemetry) &&
                telemetry.TryGetProperty("precipitation_mm", out var pProp) &&
                pProp.TryGetSingle(out var p2))
                return p2;

            return null;
        }
        catch
        {
            return null;
        }
    }
}
