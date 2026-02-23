using System.Text.Json;
using AgroSolution.Core.App.DTO;

namespace AgroSolution.Core.App.Validation;

/// <summary>
/// Interface para validadores de dados IoT por tipo de dispositivo
/// </summary>
public interface IIoTDeviceValidator
{
    /// <summary>
    /// Valida se o JSON é compatível com o tipo de dispositivo
    /// </summary>
    /// <param name="rawData">Dados brutos em JSON</param>
    /// <returns>True se válido, False caso contrário</returns>
    bool ValidateRawData(string rawData);

    /// <summary>
    /// Extrai os valores do JSON para o tipo de dispositivo
    /// </summary>
    /// <param name="rawData">Dados brutos em JSON</param>
    /// <returns>Dicionário com os dados extraídos</returns>
    Dictionary<string, object?> ExtractData(string rawData);

    /// <summary>
    /// Tipo de dispositivo que este validador suporta
    /// </summary>
    IoTDeviceType SupportedDeviceType { get; }
}

/// <summary>
/// Validador para sensores de temperatura
/// Espera JSON com propriedades: value (float), unit (string - "C" ou "F")
/// </summary>
public class TemperatureSensorValidator : IIoTDeviceValidator
{
    public IoTDeviceType SupportedDeviceType => IoTDeviceType.TemperatureSensor;

    public bool ValidateRawData(string rawData)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawData);
            var root = doc.RootElement;

            // Verifica se possui 'value' (float)
            if (!root.TryGetProperty("value", out var valueElement))
                return false;

            if (valueElement.ValueKind != JsonValueKind.Number)
                return false;

            // Extrai o valor para validar se é número válido
            if (!valueElement.TryGetSingle(out var temperature))
                return false;

            // Validação de range razoável para temperatura (-60°C a 60°C)
            if (temperature < -60 || temperature > 60)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    public Dictionary<string, object?> ExtractData(string rawData)
    {
        using var doc = JsonDocument.Parse(rawData);
        var root = doc.RootElement;

        var result = new Dictionary<string, object?>();

        if (root.TryGetProperty("value", out var valueElement) && valueElement.TryGetSingle(out var temperature))
            result["temperature"] = temperature;

        if (root.TryGetProperty("unit", out var unitElement))
            result["unit"] = unitElement.GetString() ?? "C";

        if (root.TryGetProperty("deviceId", out var deviceIdElement))
            result["deviceId"] = deviceIdElement.GetString();

        return result;
    }
}

/// <summary>
/// Validador para sensores de umidade
/// Espera JSON com propriedades: value (float 0-100), unit (string - "%")
/// </summary>
public class HumiditySensorValidator : IIoTDeviceValidator
{
    public IoTDeviceType SupportedDeviceType => IoTDeviceType.HumiditySensor;

    public bool ValidateRawData(string rawData)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawData);
            var root = doc.RootElement;

            // Verifica se possui 'value' (float)
            if (!root.TryGetProperty("value", out var valueElement))
                return false;

            if (valueElement.ValueKind != JsonValueKind.Number)
                return false;

            // Extrai o valor para validar se é número válido
            if (!valueElement.TryGetSingle(out var humidity))
                return false;

            // Validação de range: 0% a 100%
            if (humidity < 0 || humidity > 100)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    public Dictionary<string, object?> ExtractData(string rawData)
    {
        using var doc = JsonDocument.Parse(rawData);
        var root = doc.RootElement;

        var result = new Dictionary<string, object?>();

        if (root.TryGetProperty("value", out var valueElement) && valueElement.TryGetSingle(out var humidity))
            result["humidity"] = humidity;

        if (root.TryGetProperty("unit", out var unitElement))
            result["unit"] = unitElement.GetString() ?? "%";

        if (root.TryGetProperty("deviceId", out var deviceIdElement))
            result["deviceId"] = deviceIdElement.GetString();

        return result;
    }
}

/// <summary>
/// Validador para sensores de precipitação
/// Espera JSON com propriedades: value (float), unit (string - "mm" ou "in")
/// </summary>
public class PrecipitationSensorValidator : IIoTDeviceValidator
{
    public IoTDeviceType SupportedDeviceType => IoTDeviceType.PrecipitationSensor;

    public bool ValidateRawData(string rawData)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawData);
            var root = doc.RootElement;

            // Verifica se possui 'value' (float)
            if (!root.TryGetProperty("value", out var valueElement))
                return false;

            if (valueElement.ValueKind != JsonValueKind.Number)
                return false;

            // Extrai o valor para validar se é número válido
            if (!valueElement.TryGetSingle(out var precipitation))
                return false;

            // Validação: não pode ser negativo
            if (precipitation < 0)
                return false;

            // Validação razoável: até 500mm em uma leitura
            if (precipitation > 500)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    public Dictionary<string, object?> ExtractData(string rawData)
    {
        using var doc = JsonDocument.Parse(rawData);
        var root = doc.RootElement;

        var result = new Dictionary<string, object?>();

        if (root.TryGetProperty("value", out var valueElement) && valueElement.TryGetSingle(out var precipitation))
            result["precipitation"] = precipitation;

        if (root.TryGetProperty("unit", out var unitElement))
            result["unit"] = unitElement.GetString() ?? "mm";

        if (root.TryGetProperty("deviceId", out var deviceIdElement))
            result["deviceId"] = deviceIdElement.GetString();

        return result;
    }
}

/// <summary>
/// Validador para Estação Meteorológica (WeatherStationNode)
/// Espera JSON com estrutura complexa contendo `device_id`, `telemetry` e subcampos
/// </summary>
public class WeatherStationValidator : IIoTDeviceValidator
{
    public IoTDeviceType SupportedDeviceType => IoTDeviceType.WeatherStationNode;

    public bool ValidateRawData(string rawData)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawData);
            var root = doc.RootElement;

            // Must contain device_id
            if (!root.TryGetProperty("device_id", out var deviceIdElement) && !root.TryGetProperty("deviceId", out deviceIdElement))
                return false;

            if (deviceIdElement.ValueKind != JsonValueKind.String)
                return false;

            // telemetry object required
            if (!root.TryGetProperty("telemetry", out var telemetry))
                return false;

            // temperature_air (number)
            if (telemetry.TryGetProperty("temperature_air", out var tempElem))
            {
                if (tempElem.ValueKind != JsonValueKind.Number || !tempElem.TryGetSingle(out var temp))
                    return false;
                if (temp < -60 || temp > 60) return false;
            }

            // humidity_air (0-100)
            if (telemetry.TryGetProperty("humidity_air", out var humElem))
            {
                if (humElem.ValueKind != JsonValueKind.Number || !humElem.TryGetSingle(out var hum))
                    return false;
                if (hum < 0 || hum > 100) return false;
            }

            // pressure reasonable range
            if (telemetry.TryGetProperty("pressure", out var presElem))
            {
                if (presElem.ValueKind != JsonValueKind.Number || !presElem.TryGetSingle(out var pres))
                    return false;
                if (pres < 300 || pres > 1100) return false;
            }

            // precipitation_mm (>=0 <=500)
            if (telemetry.TryGetProperty("precipitation_mm", out var precElem))
            {
                if (precElem.ValueKind != JsonValueKind.Number || !precElem.TryGetSingle(out var prec))
                    return false;
                if (prec < 0 || prec > 500) return false;
            }

            // wind_speed_kmh (>=0)
            if (telemetry.TryGetProperty("wind_speed_kmh", out var windElem))
            {
                if (windElem.ValueKind != JsonValueKind.Number || !windElem.TryGetSingle(out var wind))
                    return false;
                if (wind < 0 || wind > 400) return false;
            }

            // soil_moisture_1 (0-100)
            if (telemetry.TryGetProperty("soil_moisture_1", out var soilElem))
            {
                if (soilElem.ValueKind != JsonValueKind.Number || !soilElem.TryGetSingle(out var soil))
                    return false;
                if (soil < 0 || soil > 100) return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public Dictionary<string, object?> ExtractData(string rawData)
    {
        using var doc = JsonDocument.Parse(rawData);
        var root = doc.RootElement;

        var result = new Dictionary<string, object?>();

        if (root.TryGetProperty("device_id", out var dev) || root.TryGetProperty("deviceId", out dev))
            result["deviceId"] = dev.GetString();

        if (root.TryGetProperty("timestamp", out var ts) && ts.ValueKind == JsonValueKind.String)
            result["timestamp"] = ts.GetString();

        if (root.TryGetProperty("telemetry", out var telemetry) && telemetry.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in telemetry.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetSingle(out var f))
                    result[prop.Name] = f;
                else if (prop.Value.ValueKind == JsonValueKind.String)
                    result[prop.Name] = prop.Value.GetString();
                else
                    result[prop.Name] = prop.Value.ToString();
            }
        }

        return result;
    }
}

/// <summary>
/// Factory para obter o validador apropriado para cada tipo de dispositivo
/// </summary>
public class IoTDeviceValidatorFactory
{
    private readonly Dictionary<IoTDeviceType, IIoTDeviceValidator> _validators;

    public IoTDeviceValidatorFactory()
    {
        _validators = new Dictionary<IoTDeviceType, IIoTDeviceValidator>
        {
            { IoTDeviceType.TemperatureSensor, new TemperatureSensorValidator() },
            { IoTDeviceType.HumiditySensor, new HumiditySensorValidator() },
            { IoTDeviceType.PrecipitationSensor, new PrecipitationSensorValidator() },
            { IoTDeviceType.WeatherStationNode, new WeatherStationValidator() }
        };
    }

    /// <summary>
    /// Obtém o validador para o tipo de dispositivo especificado
    /// </summary>
    public IIoTDeviceValidator GetValidator(IoTDeviceType deviceType)
    {
        if (_validators.TryGetValue(deviceType, out var validator))
            return validator;

        throw new ArgumentException($"Nenhum validador encontrado para o tipo de dispositivo: {deviceType}");
    }

    /// <summary>
    /// Verifica se existe validador para o tipo especificado
    /// </summary>
    public bool HasValidator(IoTDeviceType deviceType) => _validators.ContainsKey(deviceType);
}
