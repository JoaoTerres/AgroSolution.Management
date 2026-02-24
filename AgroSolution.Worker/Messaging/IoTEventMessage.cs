using AgroSolution.Core.App.DTO;

namespace AgroSolution.Worker.Messaging;

/// <summary>
/// Envelope publicado no exchange RabbitMQ para cada registro IoTData.
/// Carrega os dados essenciais para que o consumer possa processar
/// de forma idempotente usando apenas o IoTDataId.
/// </summary>
public sealed class IoTEventMessage
{
    /// <summary>FK para IoTData.Id — chave de rastreamento e idempotência.</summary>
    public Guid IoTDataId { get; set; }

    /// <summary>ID do talhão associado.</summary>
    public Guid PlotId { get; set; }

    /// <summary>Tipo do sensor que gerou o dado.</summary>
    public IoTDeviceType DeviceType { get; set; }

    /// <summary>Payload bruto em JSON (mesmo valor persistido em IoTData.RawData).</summary>
    public string RawData { get; set; } = string.Empty;

    /// <summary>Timestamp original do dispositivo.</summary>
    public DateTime DeviceTimestamp { get; set; }

    /// <summary>Momento em que a mensagem foi publicada pelo Producer.</summary>
    public DateTime PublishedAt { get; set; }
}
