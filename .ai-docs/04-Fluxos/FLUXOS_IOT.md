# Fluxos de Negócio - Sistema IoT

---
**Versão:** 1.0  
**Data:** 12/02/2026  
**Status:** Ativo
---

## 📊 Fluxo: Recepção de Dados IoT

### Diagrama

```
┌─────────────────────────┐
│ Dispositivo IoT         │
│ (Sensor de Temperatura) │
└──────────┬──────────────┘
           │
           │ POST /api/iot/data
           │ {
           │   "plotId": "550e8400...",
           │   "deviceType": 1,
           │   "rawData": "{\"value\": 25.5}",
           │   "timestamp": null
           │ }
           ▼
┌─────────────────────────┐
│ IoTDataController       │
│ ReceiveData()          │
└──────────┬──────────────┘
           │
           │ Valida DTO básico
           │ (PlotId, RawData)
           ▼
┌─────────────────────────┐
│ IReceiveIoTData         │
│ ExecuteAsync()         │
└──────────┬──────────────┘
           │
           ├─ Valida PlotId ≠ empty
           │
           ├─ Valida RawData ≠ empty
           │
           ├─ Valida DeviceType
           │
           ▼
┌─────────────────────────┐
│ IoTDeviceValidatorFactory
│ GetValidator()         │
└──────────┬──────────────┘
           │
           ├─ (DeviceType == 1)
           │   └─ TemperatureSensorValidator
           │       └─ ValidateRawData()
           │           ├─ JSON válido?
           │           ├─ value é float?
           │           └─ -60 ≤ value ≤ 60?
           │
           ├─ (DeviceType == 2)
           │   └─ HumiditySensorValidator
           │       └─ ValidateRawData()
           │
           └─ (DeviceType == 3)
               └─ PrecipitationSensorValidator
                   └─ ValidateRawData()
           │
           ▼ [VÁLIDO] ou [INVÁLIDO]
           │
       ┌───┴───┐
       │       │
       ▼       ▼
    VÁLIDO   INVÁLIDO
       │         │
       │         ├─ Return Failure
       │         │ "Formato inválido..."
       │         │
       │         ▼
       │    ┌──────────────┐
       │    │ Controller   │
       │    │ CustomResponse
       │    │ 400 Bad Req  │
       │    └──────────────┘
       │
       ▼
┌────────────────────────┐
│ Criar IoTData         │
│ new IoTData(          │
│   plotId,             │
│   deviceType,         │
│   rawData,            │
│   deviceTimestamp     │
│ )                     │
└──────────┬─────────────┘
           │
           │ Status = Pending
           │
           ▼
┌────────────────────────┐
│ IIoTDataRepository     │
│ AddAsync()            │
└──────────┬─────────────┘
           │
           ▼
┌────────────────────────┐
│ PostgreSQL             │
│ iot_data table        │
└──────────┬─────────────┘
           │
           ▼
┌────────────────────────┐
│ IoTDataReceivedDto     │
│ {                      │
│   id: "guid",          │
│   plotId: "guid",      │
│   deviceType: 1,       │
│   receivedAt: "ts",    │
│   status: "Sucesso"    │
│ }                      │
└──────────┬─────────────┘
           │
           ▼
┌────────────────────────┐
│ Controller             │
│ CustomResponse()       │
│ 200 OK                 │
└────────────────────────┘
```

---

## 🔀 Decisões de Negócio

### 1. Validação em Múltiplos Níveis

**Por quê?** Falha-rápido (fail-fast) em diferentes pontos:
- DTO: Estrutura básica
- Factory: Tipo de dispositivo
- Validador: Formato específico

**Benefícios:**
- Feedback imediato
- Erros claros
- Debugging facilitado

### 2. Armazenamento de RawData

**Por quê?** Manter JSON original para auditoria e reprocessamento

**Benefícios:**
- Rastreabilidade completa
- Reprocessamento sem perda
- Análise histórica
- Conformidade legal

### 3. Status de Processamento

**Por quê?** Preparação para sistema assíncrono com RabbitMQ

**Estados:**
```
Pending → Queued → Processed ✓
              ↓
           Failed
              ↓
          Discarded
```

---

## 📈 Fluxo: Processamento Futuro (RabbitMQ)

```
[Pending Data]
    │
    ▼
[Worker ReadPending]
    │
    ├─ Busca dados com Status=Pending
    ├─ Marca como Queued
    │ (ProcessingQueueId = job-id)
    │
    ▼
[RabbitMQ Producer]
    │
    ├─ Enfileira para exchange
    │ (topic: iot.{deviceType})
    │
    ▼
[RabbitMQ Queue]
    │
    │ (iot.temperature)
    │ (iot.humidity)
    │ (iot.precipitation)
    │
    ▼
[Consumer Worker]
    │
    ├─ Consome mensagem
    ├─ Processa dados
    ├─ Armazena em Data Lake
    │
    ▼
[Update IoTData]
    │
    ├─ Se sucesso: Status = Processed
    ├─ Se falha: Status = Failed
    │           ErrorMessage = motivo
    │
    ▼
[Notificação] (opcional)
```

---

## 🎯 Invariantes de Negócio

| Invariante | Garantia | Implementação |
|-----------|----------|-----------------|
| PlotId válido | Dados pertencem ao talhão correto | NOT NULL em BD |
| DeviceType suportado | Apenas tipos conhecidos | Enum validado |
| JSON válido | Dados legíveis | Parser + Validador |
| Timestamp consistente | Auditoria temporal | Sempre UTC |
| Status consistente | Rastreamento correto | Transação atômica |

---

## 🔄 Estados Possíveis de IoTData

### Estado: Pending
- **Quando:** Recém-recebido
- **Ações:** Nenhuma ainda
- **Transição:** → Queued (worker lê)

### Estado: Queued
- **Quando:** Enviado para processamento
- **Dados:** ProcessingQueueId, ProcessingStartedAt preenchidos
- **Transição:** → Processed ou Failed

### Estado: Processed
- **Quando:** Processamento bem-sucedido
- **Dados:** ProcessingCompletedAt preenchido
- **Ações:** Dados armazenados, analytics atualizado

### Estado: Failed
- **Quando:** Erro durante processamento
- **Dados:** ErrorMessage preenchido, ProcessingCompletedAt
- **Ações:** Log, retry policy, notificação

### Estado: Discarded
- **Quando:** Descartado propositalmente
- **Motivo:** Duplicado, inválido tardio, etc.

---

## 💡 Casos de Uso Específicos

### Cenário 1: Sensor Defeitoso

```
Sensor envia: {"value": 25.5, "unit": "C"}
↓
Recebido: Status=Pending
↓
Depois recebe: {"value": 999.0, "unit": "C"}
↓
Validação falha: 999 > 60
↓
Response: 400 Bad Request
↓
Usuário: Dispositivo corrompido → Substitui
```

### Cenário 2: Rede Instável

```
Sensor envia: {"value": 25.5}
↓
Recebido: Status=Pending ✓
↓
Retorna: HTTP 200
↓
Sensor recebe ACK: Pode enviar próximo
↓
Fila: Processa depois
```

### Cenário 3: Reprocessamento

```
Dado em: Status=Failed, ErrorMessage="BD indisponível"
↓
Admin: Reinicia worker
↓
Worker: SELECT * WHERE Status IN (Failed, Pending)
↓
Reprocessa dados
↓
Status: Processed ✓
```

---

## 📊 Métricas Rastreadas

| Métrica | Cálculo | Uso |
|---------|---------|-----|
| Taxa Recepção | Count(Pending) / tempo | Monitoramento |
| Taxa Sucesso | Count(Processed) / Count(Total) | Alertas |
| Latência | ReceivedAt → ProcessedAt | SLA |
| Fila Pendente | Count(Queued) | Capacidade |

---

**Última atualização:** 12/02/2026
