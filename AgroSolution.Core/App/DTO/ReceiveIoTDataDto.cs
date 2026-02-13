using System.Text.Json.Serialization;

namespace AgroSolution.Core.App.DTO;

/// <summary>
/// Enum representando os tipos de dispositivos IoT suportados
/// </summary>
public enum IoTDeviceType
{
    /// <summary>Leitor de temperatura em Celsius</summary>
    TemperatureSensor = 1,
    
    /// <summary>Leitor de umidade em percentual</summary>
    HumiditySensor = 2,
    
    /// <summary>Leitor de precipitação em milímetros</summary>
    PrecipitationSensor = 3
}

/// <summary>
/// DTO para recepção de dados de dispositivos IoT
/// Contém o ID do talhão e os dados brutos em JSON
/// </summary>
public class ReceiveIoTDataDto
{
    /// <summary>
    /// ID do talhão (Plot) para o qual os dados se referem
    /// </summary>
    [JsonPropertyName("plotId")]
    public required Guid PlotId { get; set; }

    /// <summary>
    /// Tipo de dispositivo que enviou os dados
    /// </summary>
    [JsonPropertyName("deviceType")]
    public required IoTDeviceType DeviceType { get; set; }

    /// <summary>
    /// Dados brutos do dispositivo em formato JSON
    /// Será validado conforme o tipo de dispositivo
    /// </summary>
    [JsonPropertyName("data")]
    public required string RawData { get; set; }

    /// <summary>
    /// Timestamp opcional do dispositivo (ISO 8601)
    /// Se não fornecido, será preenchido com Now
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime? DeviceTimestamp { get; set; }
}

/// <summary>
/// DTO de resposta ao receber dados IoT
/// </summary>
public class IoTDataReceivedDto
{
    public required Guid Id { get; set; }
    public required Guid PlotId { get; set; }
    public required IoTDeviceType DeviceType { get; set; }
    public required DateTime ReceivedAt { get; set; }
    public string? Status { get; set; }
}
