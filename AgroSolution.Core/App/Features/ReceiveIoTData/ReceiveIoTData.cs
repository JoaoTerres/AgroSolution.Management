using AgroSolution.Core.App.DTO;
using AgroSolution.Core.App.Common;
using AgroSolution.Core.App.Validation;
using AgroSolution.Core.Domain.Entities;
using AgroSolution.Core.Domain.Interfaces;

namespace AgroSolution.Core.App.Features.ReceiveIoTData;

/// <summary>
/// Interface para o caso de uso de recepção de dados IoT
/// </summary>
public interface IReceiveIoTData
{
    /// <summary>
    /// Executa o recebimento e validação de dados IoT
    /// </summary>
    Task<Result<IoTDataReceivedDto>> ExecuteAsync(ReceiveIoTDataDto dto);
}

/// <summary>
/// Caso de uso que implementa a lógica de recebimento de dados IoT
/// 1. Valida o JSON conforme o tipo de dispositivo
/// 2. Persiste os dados brutos
/// 3. Retorna confirmação com ID para rastreamento
/// </summary>
public class ReceiveIoTData : IReceiveIoTData
{
    private readonly IIoTDataRepository _repository;
    private readonly IoTDeviceValidatorFactory _validatorFactory;
    private readonly IDeviceRepository _deviceRepository;

    public ReceiveIoTData(
        IIoTDataRepository repository,
        IoTDeviceValidatorFactory validatorFactory,
        IDeviceRepository deviceRepository)
    {
        _repository = repository;
        _validatorFactory = validatorFactory;
        _deviceRepository = deviceRepository;
    }

    public async Task<Result<IoTDataReceivedDto>> ExecuteAsync(ReceiveIoTDataDto dto)
    {
        // Validação 1: DTO não pode ser nulo
        if (dto == null)
            return Result<IoTDataReceivedDto>.Fail("Dados inválidos recebidos.");

        // Validação 2: Obter PlotId via deviceId caso não venha no DTO
        Guid plotId;
        if (dto.PlotId.HasValue && dto.PlotId.Value != Guid.Empty)
        {
            plotId = dto.PlotId.Value;
        }
        else
        {
            // Tentar obter deviceId do DTO ou do JSON bruto
            var deviceId = dto.DeviceId;
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(dto.RawData);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("device_id", out var d1) && d1.ValueKind == System.Text.Json.JsonValueKind.String)
                        deviceId = d1.GetString();
                    else if (root.TryGetProperty("deviceId", out var d2) && d2.ValueKind == System.Text.Json.JsonValueKind.String)
                        deviceId = d2.GetString();
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(deviceId))
                return Result<IoTDataReceivedDto>.Fail("Identificador do dispositivo (deviceId) é obrigatório quando PlotId não é informado.");

            // Lookup no repositório de dispositivos
            var foundPlot = await _deviceRepository.GetPlotIdByDeviceAsync(deviceId!);
            if (!foundPlot.HasValue || foundPlot.Value == Guid.Empty)
                return Result<IoTDataReceivedDto>.Fail("Dispositivo não encontrado para o deviceId informado.");

            plotId = foundPlot.Value;
        }

        // Validação 3: RawData não pode ser vazio
        if (string.IsNullOrWhiteSpace(dto.RawData))
            return Result<IoTDataReceivedDto>.Fail("Dados do dispositivo são obrigatórios.");

        // Validação 4: Tipo de dispositivo deve ser válido
        if (!_validatorFactory.HasValidator(dto.DeviceType))
            return Result<IoTDataReceivedDto>.Fail($"Tipo de dispositivo não suportado: {dto.DeviceType}");

        // Validação 5: Obter validador específico do dispositivo
        var validator = _validatorFactory.GetValidator(dto.DeviceType);

        // Validação 6: Validar JSON contra o tipo de dispositivo
        if (!validator.ValidateRawData(dto.RawData))
            return Result<IoTDataReceivedDto>.Fail(
                $"Formato de dados inválido para dispositivo {dto.DeviceType}. " +
                "Verifique se o JSON contém os campos obrigatórios com tipos corretos.");

        try
        {
                // Criar entidade de domínio
                var iotData = new IoTData(
                    plotId,
                    dto.DeviceType,
                    dto.RawData,
                    dto.DeviceTimestamp ?? DateTime.UtcNow);

            // Persistir dados
            var success = await _repository.AddAsync(iotData);

            if (!success)
                return Result<IoTDataReceivedDto>.Fail("Erro ao persistir dados no repositório.");

            // Retornar resposta de sucesso
            var response = new IoTDataReceivedDto
            {
                Id = iotData.Id,
                PlotId = iotData.PlotId,
                DeviceType = iotData.DeviceType,
                ReceivedAt = iotData.ReceivedAt,
                Status = "Recebido com sucesso. Aguardando processamento."
            };

            return Result<IoTDataReceivedDto>.Ok(response);
        }
        catch (Exception ex)
        {
            return Result<IoTDataReceivedDto>.Fail($"Erro ao processar dados: {ex.Message}");
        }
    }
}
