using AgroSolution.Core.App.DTO;
using AgroSolution.Core.App.Features.ReceiveIoTData;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolution.Api.Controllers;

/// <summary>
/// Controller para recepção de dados de dispositivos IoT
/// Endpoints para integração com sensores de temperatura, umidade e precipitação
/// </summary>
[Route("api/iot")]
[ApiController]
public class IoTDataController(IReceiveIoTData receiveIoTData) : BaseController
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
    public async Task<IActionResult> ReceiveData([FromBody] ReceiveIoTDataDto dto)
    {
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
}
