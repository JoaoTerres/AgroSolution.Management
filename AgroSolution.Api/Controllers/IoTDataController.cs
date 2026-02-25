using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using AgroSolution.Core.App.DTO;
using AgroSolution.Core.App.Features.GetIoTDataByRange;
using AgroSolution.Core.App.Features.ReceiveIoTData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolution.Api.Controllers;

/// <summary>
/// Controller para recepção e consulta de dados de dispositivos IoT.
/// </summary>
[Route("api/iot")]
[ApiController]
public class IoTDataController(
    IReceiveIoTData receiveIoTData,
    IGetIoTDataByRange getIoTDataByRange) : BaseController
{
    /// <summary>
    /// Recebe dados de um dispositivo IoT
    /// </summary>
    /// <param name="dto">Dados do dispositivo contendo ID do talhão, tipo e JSON de dados</param>
    /// <returns>Confirmação de recepção com ID de rastreamento</returns>
    /// <response code="200">Dados recebidos e armazenados com sucesso</response>
    /// <response code="400">Validação de dados falhou</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost("data")]
    [Authorize]
    [Produces("application/json")]
    public async Task<IActionResult> ReceiveData()
    {
        // Ler corpo cru
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();

        // Parse JSON body to extract well-known fields without discarding rawBody
        Guid?   plotIdFromBody     = null;
        string? deviceIdFromBody   = null;
        IoTDeviceType? deviceTypeFromBody = null;

        try
        {
            using var doc  = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            // plotId
            if (root.TryGetProperty("plotId", out var pId) && pId.ValueKind == JsonValueKind.String
                && Guid.TryParse(pId.GetString(), out var parsedPlot))
                plotIdFromBody = parsedPlot;

            // device_id / deviceId
            if (root.TryGetProperty("device_id", out var dId) && dId.ValueKind == JsonValueKind.String)
                deviceIdFromBody = dId.GetString();
            else if (root.TryGetProperty("deviceId", out var dId2) && dId2.ValueKind == JsonValueKind.String)
                deviceIdFromBody = dId2.GetString();

            // deviceType from JSON body (int or string name)
            if (root.TryGetProperty("deviceType", out var dtProp))
            {
                if (dtProp.ValueKind == JsonValueKind.Number && Enum.IsDefined(typeof(IoTDeviceType), dtProp.GetInt32()))
                    deviceTypeFromBody = (IoTDeviceType)dtProp.GetInt32();
                else if (dtProp.ValueKind == JsonValueKind.String
                         && Enum.TryParse<IoTDeviceType>(dtProp.GetString(), ignoreCase: true, out var parsedEnum))
                    deviceTypeFromBody = parsedEnum;
            }
        }
        catch { /* malformed JSON — will be caught by use-case validator */ }

        // Determine device type: header > body > inference
        var deviceTypeHeader = Request.Headers["X-Device-Type"].FirstOrDefault();
        IoTDeviceType deviceType;
        if (!string.IsNullOrWhiteSpace(deviceTypeHeader) && int.TryParse(deviceTypeHeader, out var dt))
            deviceType = (IoTDeviceType)dt;
        else if (deviceTypeFromBody.HasValue)
            deviceType = deviceTypeFromBody.Value;
        else if (!string.IsNullOrWhiteSpace(rawBody) && rawBody.Contains("\"telemetry\"", StringComparison.OrdinalIgnoreCase))
            deviceType = IoTDeviceType.WeatherStationNode;
        else
            deviceType = IoTDeviceType.TemperatureSensor; // fallback mínimo

        var dto = new ReceiveIoTDataDto
        {
            PlotId     = plotIdFromBody,
            DeviceId   = deviceIdFromBody,
            DeviceType = deviceType,
            RawData    = rawBody
        };

        var result = await receiveIoTData.ExecuteAsync(dto);
        return CustomResponse(result);
    }

    /// <summary>
    /// Health check do endpoint IoT
    /// Permite verificar se o serviço está ativo
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Retorna dados IoT de um talhão em um intervalo de tempo (dashboard).
    /// </summary>
    /// <param name="plotId">ID do talhão.</param>
    /// <param name="from">Data/hora inicial (UTC, ISO 8601). Ex: 2026-02-01T00:00:00Z</param>
    /// <param name="to">Data/hora final (UTC, ISO 8601). Ex: 2026-02-02T00:00:00Z</param>
    /// <returns>Lista de registros IoT no intervalo.</returns>
    /// <response code="200">Dados retornados (pode ser lista vazia).</response>
    /// <response code="400">Parâmetros inválidos ou intervalo excede 90 dias.</response>
    [HttpGet("data/{plotId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByRange(
        Guid plotId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        // Npgsql 6+ requires DateTimeKind.Utc for timestamptz columns
        var fromUtc = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        var toUtc   = DateTime.SpecifyKind(to,   DateTimeKind.Utc);
        var result = await getIoTDataByRange.ExecuteAsync(plotId, fromUtc, toUtc);
        return CustomResponse(result);
    }
}
