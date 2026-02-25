using AgroSolution.Core.App.DTO;
using AgroSolution.Core.Domain;

namespace AgroSolution.Core.Domain.Entities;

/// <summary>
/// Entidade que representa dados recebidos de um dispositivo IoT
/// Armazena os dados brutos e metadados da recepção
/// </summary>
public class IoTData
{
    public IoTData() { }

    public IoTData(Guid plotId, IoTDeviceType deviceType, string rawData, DateTime deviceTimestamp)
    {
        ValidateConstruction(plotId, deviceType, rawData);

        Id = Guid.NewGuid();
        PlotId = plotId;
        DeviceType = deviceType;
        RawData = rawData;
        DeviceTimestamp = deviceTimestamp == default ? DateTime.UtcNow : deviceTimestamp;
        ReceivedAt = DateTime.UtcNow;
        ProcessingStatus = IoTProcessingStatus.Pending;
    }

    /// <summary>
    /// ID único dos dados recebidos
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID do talhão para o qual os dados se referem
    /// </summary>
    public Guid PlotId { get; set; }

    /// <summary>
    /// Tipo de dispositivo que enviou os dados
    /// </summary>
    public IoTDeviceType DeviceType { get; set; }

    /// <summary>
    /// Dados brutos em formato JSON do dispositivo
    /// </summary>
    public string RawData { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp do dispositivo (quando o dado foi coletado)
    /// </summary>
    public DateTime DeviceTimestamp { get; set; }

    /// <summary>
    /// Timestamp de recepção no servidor
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// Status atual do processamento do dado
    /// </summary>
    public IoTProcessingStatus ProcessingStatus { get; set; }

    /// <summary>
    /// ID da fila/processo que está processando este dado
    /// </summary>
    public string? ProcessingQueueId { get; set; }

    /// <summary>
    /// Data em que o processamento foi iniciado
    /// </summary>
    public DateTime? ProcessingStartedAt { get; set; }

    /// <summary>
    /// Data em que o processamento foi concluído
    /// </summary>
    public DateTime? ProcessingCompletedAt { get; set; }

    /// <summary>
    /// Mensagem de erro caso o processamento tenha falhado
    /// </summary>
    public string? ErrorMessage { get; set; }

    private void ValidateConstruction(Guid plotId, IoTDeviceType deviceType, string rawData)
    {
        AssertValidation.NotEmpty(plotId, nameof(plotId));
        AssertValidation.NotNull(rawData, nameof(rawData));
        AssertValidation.IsValidEnum(deviceType, nameof(deviceType));
    }

    /// <summary>
    /// Marca o dado como enviado para processamento
    /// </summary>
    public void MarkAsQueued(string queueId)
    {
        AssertValidation.NotNull(queueId, nameof(queueId));
        ProcessingStatus = IoTProcessingStatus.Queued;
        ProcessingQueueId = queueId;
        ProcessingStartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca o dado como processado com sucesso
    /// </summary>
    public void MarkAsProcessed()
    {
        ProcessingStatus = IoTProcessingStatus.Processed;
        ProcessingCompletedAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    /// <summary>
    /// Marca o dado como falhado no processamento
    /// </summary>
    public void MarkAsFailed(string errorMessage)
    {
        AssertValidation.NotNull(errorMessage, nameof(errorMessage));
        ProcessingStatus = IoTProcessingStatus.Failed;
        ProcessingCompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Enum representando os possíveis estados de processamento
/// </summary>
public enum IoTProcessingStatus
{
    /// <summary>Recebido mas ainda não processado</summary>
    Pending = 1,

    /// <summary>Enviado para fila de processamento</summary>
    Queued = 2,

    /// <summary>Processado com sucesso</summary>
    Processed = 3,

    /// <summary>Falha no processamento</summary>
    Failed = 4,

    /// <summary>Ignorado/Descartado</summary>
    Discarded = 5
}
