# API IoT - Documentação Técnica

---
**Versão:** 1.0  
**Data:** 12/02/2026  
**Status:** Ativo
---

## 🎯 Endpoints

### 1. Receber Dados IoT

**POST** `/api/iot/data`

#### Request

```http
POST /api/iot/data HTTP/1.1
Host: api.agrosolution.local
Content-Type: application/json

{
  "plotId": "550e8400-e29b-41d4-a716-446655440000",
  "deviceType": 1,
  "rawData": "{\"value\": 25.5, \"unit\": \"C\"}",
  "timestamp": "2026-02-12T10:30:00Z"
}
```

#### Response (200 OK)

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "success": true,
  "data": {
    "id": "7e4f1f9c-3a5d-4b7c-8a9e-1b2c3d4e5f6a",
    "plotId": "550e8400-e29b-41d4-a716-446655440000",
    "deviceType": 1,
    "receivedAt": "2026-02-12T10:30:15Z",
    "status": "Recebido com sucesso. Aguardando processamento."
  }
}
```

#### Response (400 Bad Request)

```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "success": false,
  "errors": [
    "Formato de dados inválido para dispositivo TemperatureSensor. Verifique se o JSON contém os campos obrigatórios com tipos corretos."
  ]
}
```

#### Parâmetros

| Nome | Tipo | Obrigatório | Descrição |
|------|------|-----------|-----------|
| `plotId` | UUID | ✅ Sim | ID do talhão (Plot) |
| `deviceType` | Enum | ✅ Sim | Tipo de dispositivo (1=Temperatura, 2=Umidade, 3=Precipitação) |
| `rawData` | String | ✅ Sim | JSON com dados do dispositivo |
| `timestamp` | ISO 8601 | ❌ Não | Timestamp do dispositivo (padrão: agora) |

#### Validações

```
✓ plotId != null
✓ plotId != Guid.Empty
✓ deviceType ∈ {1, 2, 3}
✓ rawData != null
✓ rawData != empty
✓ rawData é JSON válido
✓ JSON válido para tipo específico
```

#### Códigos de Status

| Status | Descrição |
|--------|-----------|
| **200** | Sucesso - Dados recebidos e enfileirados |
| **400** | Validação falhou - JSON inválido, tipo não suportado |
| **500** | Erro servidor - Falha na persistência |

---

### 2. Health Check

**GET** `/api/iot/health`

#### Response (200 OK)

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "status": "healthy",
  "timestamp": "2026-02-12T10:30:15Z"
}
```

---

## 📚 Exemplos Completos

### Exemplo 1: Sensor de Temperatura

```bash
curl -X POST http://localhost:5000/api/iot/data \
  -H "Content-Type: application/json" \
  -d '{
    "plotId": "550e8400-e29b-41d4-a716-446655440000",
    "deviceType": 1,
    "rawData": "{\"value\": 22.3, \"unit\": \"C\", \"deviceId\": \"TEMP-001\"}"
  }'
```

**Resposta:**
```json
{
  "success": true,
  "data": {
    "id": "7e4f1f9c-3a5d-4b7c-8a9e-1b2c3d4e5f6a",
    "plotId": "550e8400-e29b-41d4-a716-446655440000",
    "deviceType": 1,
    "receivedAt": "2026-02-12T10:30:15Z",
    "status": "Recebido com sucesso. Aguardando processamento."
  }
}
```

### Exemplo 2: Sensor de Umidade

```bash
curl -X POST http://localhost:5000/api/iot/data \
  -H "Content-Type: application/json" \
  -d '{
    "plotId": "550e8400-e29b-41d4-a716-446655440000",
    "deviceType": 2,
    "rawData": "{\"value\": 65.5, \"unit\": \"%\", \"deviceId\": \"HUM-002\"}"
  }'
```

### Exemplo 3: Sensor de Precipitação

```bash
curl -X POST http://localhost:5000/api/iot/data \
  -H "Content-Type: application/json" \
  -d '{
    "plotId": "550e8400-e29b-41d4-a716-446655440000",
    "deviceType": 3,
    "rawData": "{\"value\": 12.5, \"unit\": \"mm\", \"deviceId\": \"PREC-003\"}"
  }'
```

### Exemplo 4: Erro de Validação

```bash
curl -X POST http://localhost:5000/api/iot/data \
  -H "Content-Type: application/json" \
  -d '{
    "plotId": "550e8400-e29b-41d4-a716-446655440000",
    "deviceType": 1,
    "rawData": "{\"value\": 999.0}"
  }'
```

**Resposta (400):**
```json
{
  "success": false,
  "errors": [
    "Formato de dados inválido para dispositivo TemperatureSensor. Verifique se o JSON contém os campos obrigatórios com tipos corretos."
  ]
}
```

---

## 🔐 Segurança

### Acesso à Rede

Endpoint `/api/iot/data` deve estar **restrito a rede fechada**:

```
✓ Não expor na internet
✓ Usar VPN/Tunnel para acesso remoto
✓ Firewall: apenas IPs de sensores
✓ Rate limiting por IP de origem
```

### Dados em Trânsito

```
✓ TLS 1.2+ obrigatório (https)
✓ Certificado válido
✓ Encryption no BD
```

### Autenticação (Futuro)

```
[Planejado para próxima fase]
- API Key por sensor
- Bearer Token
- Mutual TLS
```

---

## ⚡ Performance

### Timeouts

| Operação | Timeout |
|----------|---------|
| Recepção HTTP | 30s |
| Persistência BD | 5s |
| Total | 35s |

### Throughput Esperado

```
Estimado: 1000 requisições/segundo
Cada requisição: ~5ms (rede + BD)
Payload: ~200 bytes
```

### Otimizações

```
✓ Índices em: PlotId, ProcessingStatus
✓ Batch insert (futuro)
✓ Connection pooling
✓ Query caching
```

---

## 🚨 Tratamento de Erros

### Erros de Validação (400)

```json
{
  "success": false,
  "errors": [
    "ID do talhão é obrigatório."
  ]
}
```

### Erro de Formato (400)

```json
{
  "success": false,
  "errors": [
    "Formato de dados inválido para dispositivo TemperatureSensor. Verifique se o JSON contém os campos obrigatórios com tipos corretos."
  ]
}
```

### Erro de Servidor (500)

```json
{
  "success": false,
  "errors": [
    "Erro ao persistir dados no repositório."
  ]
}
```

---

## 📋 Retry Policy

### Cliente deve retry:

```
Status 500: Exponential backoff
  - 1ª tentativa: 1s
  - 2ª tentativa: 2s
  - 3ª tentativa: 4s
  - Max: 3 tentativas

Status 400, 404: NÃO retry
```

### Servidor garantias

```
✓ Idempotente: Mesmo JSON não duplica
  (Validação de UUID)
✓ Atomicidade: Tudo ou nada
✓ Durabilidade: Persiste em BD
```

---

## 🔧 Troubleshooting

### "Tipo de dispositivo não suportado"

**Causa:** DeviceType inválido

**Solução:**
```
✓ Usar apenas: 1, 2, 3
✓ Validar antes de enviar
```

### "Formato de dados inválido"

**Causa:** JSON não atende schema

**Solução:**
```
✓ TemperatureSensor: {"value": float, ...}
✓ HumiditySensor: {"value": 0-100 float, ...}
✓ PrecipitationSensor: {"value": float>=0, ...}
✓ Testar JSON em https://jsonlint.com
```

### "Erro ao persistir dados"

**Causa:** BD indisponível ou erro de constraint

**Solução:**
```
✓ Verificar conexão PostgreSQL
✓ Verificar PlotId existe
✓ Retry com backoff
```

---

## 📊 Monitoramento

### Métricas Recomendadas

```
- Requisições/segundo
- Taxa sucesso (200 vs 4xx/5xx)
- Latência média
- Dados em fila pendente
- Taxa rejeição
```

### Alertas

```
⚠️ Taxa erro > 5%
⚠️ Fila pendente > 10000
⚠️ Latência > 1s
⚠️ BD indisponível
```

---

**Última atualização:** 12/02/2026
