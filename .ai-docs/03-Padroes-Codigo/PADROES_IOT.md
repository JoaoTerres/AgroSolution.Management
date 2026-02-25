# Padrões de Código - Sistema IoT

---
**Versão:** 1.0  
**Data:** 12/02/2026  
**Status:** Ativo
---

## 📋 Padrão: Recepção de Dados IoT

### Arquitetura

```
IoTDataController (API)
    ↓
IReceiveIoTData (Caso de Uso)
    ↓
IoTDeviceValidatorFactory (Validação)
    ├→ IIoTDeviceValidator (Interface)
    ├→ TemperatureSensorValidator
    ├→ HumiditySensorValidator
    └→ PrecipitationSensorValidator
    ↓
IIoTDataRepository (Persistência)
    ↓
IoTData (Entidade)
```

### Fluxo de Recepção

```
1. HTTP POST /api/iot/data
   ├─ Corpo: ReceiveIoTDataDto
   │  ├─ PlotId: Guid (obrigatório)
   │  ├─ DeviceType: IoTDeviceType (obrigatório)
   │  ├─ RawData: string JSON (obrigatório)
   │  └─ Timestamp: DateTime? (opcional)

2. Controller valida DTO
   └─ Chama IReceiveIoTData.ExecuteAsync()

3. Caso de Uso valida:
   ├─ PlotId != empty
   ├─ RawData != empty
   ├─ DeviceType válido
   └─ JSON válido para tipo

4. Factory obtém validador correto
   └─ Validador específico valida JSON

5. Se válido:
   ├─ Cria entidade IoTData
   ├─ Persiste em BD
   └─ Retorna IoTDataReceivedDto

6. Se inválido:
   └─ Retorna Result<T>.Failure()

7. Controller mapeia para CustomResponse()
   └─ HTTP Response com status apropriado
```

---

## 🔍 Tipos de Dispositivos Suportados

### TemperatureSensor (Tipo 1)

**JSON Esperado:**
```json
{
  "value": 25.5,
  "unit": "C",
  "deviceId": "TEMP-001"
}
```

**Validações:**
- `value`: Float entre -60 e 60
- `unit`: "C" ou "F"
- `deviceId`: Identificador único (opcional)

**Exemplo Válido:**
```json
{"value": 22.3, "unit": "C"}
```

### HumiditySensor (Tipo 2)

**JSON Esperado:**
```json
{
  "value": 65.5,
  "unit": "%",
  "deviceId": "HUM-002"
}
```

**Validações:**
- `value`: Float entre 0 e 100
- `unit`: "%"
- `deviceId`: Identificador único (opcional)

**Exemplo Válido:**
```json
{"value": 45.0, "unit": "%"}
```

### PrecipitationSensor (Tipo 3)

**JSON Esperado:**
```json
{
  "value": 12.5,
  "unit": "mm",
  "deviceId": "PREC-003"
}
```

**Validações:**
- `value`: Float >= 0 e <= 500
- `unit`: "mm" ou "in"
- `deviceId`: Identificador único (opcional)

**Exemplo Válido:**
```json
{"value": 10.2, "unit": "mm"}
```

---

## 📦 DTOs

### ReceiveIoTDataDto (Entrada)
```csharp
{
  "plotId": "guid",              // Obrigatório
  "deviceType": 1,                // Obrigatório (1=Temp, 2=Umidade, 3=Precipitação)
  "rawData": "{...json...}",     // Obrigatório
  "timestamp": "2026-02-12T10:30:00Z"  // Opcional
}
```

### IoTDataReceivedDto (Saída)
```csharp
{
  "id": "guid",                   // ID de rastreamento
  "plotId": "guid",
  "deviceType": 1,
  "receivedAt": "2026-02-12T10:30:00Z",
  "status": "Recebido com sucesso..."
}
```

---

## 🛡️ Validações Implementadas

| Nível | Validação |
|-------|-----------|
| **DTO** | PlotId ≠ empty, RawData ≠ empty |
| **Tipo** | DeviceType deve estar em IoTDeviceType enum |
| **JSON** | Deve ser JSON válido |
| **Dispositivo** | Validador específico conforme DeviceType |

---

## 🗄️ Tabela IoTData

### Schema
```sql
CREATE TABLE iot_data (
  id UUID PRIMARY KEY,
  plot_id UUID NOT NULL,
  device_type INT NOT NULL,
  raw_data TEXT NOT NULL,
  device_timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
  received_at TIMESTAMP WITH TIME ZONE NOT NULL,
  processing_status INT NOT NULL,
  processing_queue_id TEXT,
  processing_started_at TIMESTAMP WITH TIME ZONE,
  processing_completed_at TIMESTAMP WITH TIME ZONE,
  error_message TEXT
);
```

### Índices
- `ix_iot_data_plot_id` - Para queries por talhão
- `ix_iot_data_processing_status` - Para fila de processamento
- `ix_iot_data_plot_timestamp` - Para queries por período
- `ix_iot_data_received_at` - Para ordenação por data

---

## 🔄 Estados de Processamento

```
Pending (1)
  ↓ (quando enviado para fila)
Queued (2)
  ├─ (processado com sucesso)
  → Processed (3)
  ├─ (falha)
  → Failed (4)
  └─ (descartado)
  → Discarded (5)
```

---

## 📝 Exemplos de Código

### ✅ Criar IoTData corretamente

```csharp
var iotData = new IoTData(
    plotId: Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
    deviceType: IoTDeviceType.TemperatureSensor,
    rawData: "{\"value\": 25.5, \"unit\": \"C\"}",
    deviceTimestamp: DateTime.UtcNow);

await repository.AddAsync(iotData);
```

### ✅ Usar Validador

```csharp
var factory = new IoTDeviceValidatorFactory();
var validator = factory.GetValidator(IoTDeviceType.TemperatureSensor);

if (validator.ValidateRawData(rawData))
{
    var extractedData = validator.ExtractData(rawData);
    // Processar dados
}
```

### ✅ Chamar Caso de Uso

```csharp
var dto = new ReceiveIoTDataDto
{
    PlotId = plotId,
    DeviceType = IoTDeviceType.HumiditySensor,
    RawData = "{\"value\": 65.5, \"unit\": \"%\"}",
    DeviceTimestamp = DateTime.UtcNow
};

var result = await receiveIoTData.ExecuteAsync(dto);

if (result.IsSuccess)
{
    var response = result.Data;
    Console.WriteLine($"Dados recebidos com ID: {response.Id}");
}
```

---

## ⚠️ Erros Comuns

| Erro | Causa | Solução |
|------|-------|--------|
| "ID do talhão é obrigatório" | PlotId = Guid.Empty | Use Guid.Parse() ou NewGuid() |
| "Dados do dispositivo são obrigatórios" | RawData vazio | Validar antes de enviar |
| "Tipo de dispositivo não suportado" | DeviceType inválido | Usar enum IoTDeviceType |
| "Formato de dados inválido" | JSON não válido para tipo | Verificar JSON conforme tipo |
| "Erro ao persistir dados" | Erro de BD | Verificar conexão |

---

## 🚀 Próximos Passos

1. RabbitMQ: Enfieirar dados para processamento
2. Worker: Processar dados da fila
3. Storage: Persistência em Data Lake
4. Analytics: Agregação de dados

---

**Última atualização:** 12/02/2026
