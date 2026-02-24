using System;
using System.IO;
using System.Linq;
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
    [Produces("application/json")]
    public async Task<IActionResult> ReceiveData()
    {
        // Ler corpo cru
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();

        // Tentar obter tipo do header `X-Device-Type` (int), caso contrário inferir
        var deviceTypeHeader = Request.Headers["X-Device-Type"].FirstOrDefault();
        IoTDeviceType deviceType;
        if (!string.IsNullOrWhiteSpace(deviceTypeHeader) && int.TryParse(deviceTypeHeader, out var dt))
        {
            deviceType = (IoTDeviceType)dt;
        }
        else
        {
            // Inferir simples: se contém 'telemetry' -> WeatherStationNode
            if (!string.IsNullOrWhiteSpace(rawBody) && rawBody.Contains("\"telemetry\"", StringComparison.OrdinalIgnoreCase))
                deviceType = IoTDeviceType.WeatherStationNode;
            else
                deviceType = IoTDeviceType.TemperatureSensor; // fallback mínimo
        }

        // Montar DTO: RawData deve conter o JSON completo
        var dto = new ReceiveIoTDataDto
        {
            DeviceType = deviceType,
            RawData = rawBody
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
        var result = await getIoTDataByRange.ExecuteAsync(plotId, from, to);
        return CustomResponse(result);
    }
}
