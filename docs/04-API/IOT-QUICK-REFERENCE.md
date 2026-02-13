# Quick Reference - API IoT

---
**Versão:** 1.0  
**Data:** 12/02/2026
---

## 🚀 Início Rápido

### Endpoint
```
POST http://localhost:5000/api/iot/data
```

### Request Simples
```bash
curl -X POST http://localhost:5000/api/iot/data \
  -H "Content-Type: application/json" \
  -d '{
    "plotId": "550e8400-e29b-41d4-a716-446655440000",
    "deviceType": 1,
    "rawData": "{\"value\": 25.5, \"unit\": \"C\"}"
  }'
```

### Response
```json
{
  "success": true,
  "data": {
    "id": "7e4f1f9c-3a5d-4b7c-8a9e-1b2c3d4e5f6a",
    "plotId": "550e8400-e29b-41d4-a716-446655440000",
    "deviceType": 1,
    "receivedAt": "2026-02-12T10:30:15Z",
    "status": "Recebido com sucesso..."
  }
}
```

---

## 📋 JSON por Tipo

### Tipo 1: Temperatura
```json
{
  "value": 25.5,
  "unit": "C",
  "deviceId": "TEMP-001"
}
```
- `value`: -60 a 60 (float)

### Tipo 2: Umidade
```json
{
  "value": 65.5,
  "unit": "%",
  "deviceId": "HUM-002"
}
```
- `value`: 0 a 100 (float)

### Tipo 3: Precipitação
```json
{
  "value": 12.5,
  "unit": "mm",
  "deviceId": "PREC-003"
}
```
- `value`: 0 a 500 (float)

---

## 🔧 Código C#

### Injeta o UseCase
```csharp
[Inject]
private IReceiveIoTData _receiveIoTData;
```

### Usa o UseCase
```csharp
var dto = new ReceiveIoTDataDto
{
    PlotId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
    DeviceType = IoTDeviceType.TemperatureSensor,
    RawData = "{\"value\": 25.5, \"unit\": \"C\"}",
    DeviceTimestamp = DateTime.UtcNow
};

var result = await _receiveIoTData.ExecuteAsync(dto);

if (result.Success)
{
    Console.WriteLine($"ID: {result.Data.Id}");
}
else
{
    Console.WriteLine($"Erro: {result.ErrorMessage}");
}
```

---

## 🛡️ Validações Automáticas

```
✓ PlotId obrigatório
✓ DeviceType obrigatório (1, 2 ou 3)
✓ RawData não vazio
✓ JSON válido
✓ Valores no range correto
```

---

## 📊 Estados de Dados

```
Pending    → Recém recebido
Queued     → Na fila (RabbitMQ)
Processed  → Sucesso ✓
Failed     → Erro ❌
Discarded  → Ignorado
```

---

## ⚠️ Erros Comuns

| Erro | Solução |
|------|---------|
| "ID do talhão obrigatório" | Adicione `plotId` |
| "Tipo não suportado" | Use 1, 2 ou 3 |
| "Formato inválido" | Validar JSON do valor |
| "Valor fora do range" | Verificar limites |

---

## 🔍 Debug

### Health Check
```bash
curl http://localhost:5000/api/iot/health
```

### Ver Logs
```bash
# Verificar se dados foram salvos
SELECT * FROM iot_data ORDER BY received_at DESC LIMIT 5;
```

---

**Próxima Etapa:** RabbitMQ para processamento assincrono 🚀
