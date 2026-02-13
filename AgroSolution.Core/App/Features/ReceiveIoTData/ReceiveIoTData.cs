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

    public ReceiveIoTData(
        IIoTDataRepository repository,
        IoTDeviceValidatorFactory validatorFactory)
    {
        _repository = repository;
        _validatorFactory = validatorFactory;
    }

    public async Task<Result<IoTDataReceivedDto>> ExecuteAsync(ReceiveIoTDataDto dto)
    {
        // Validação 1: DTO não pode ser nulo
        if (dto == null)
            return Result<IoTDataReceivedDto>.Fail("Dados inválidos recebidos.");

        // Validação 2: PlotId não pode ser vazio
        if (dto.PlotId == Guid.Empty)
            return Result<IoTDataReceivedDto>.Fail("ID do talhão é obrigatório.");

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
                dto.PlotId,
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
