# API Reference

**Versão:** 2.0 | **Atualizado:** 24/02/2026

Base URL (local): `https://localhost:<porta>`  
Todos os endpoints marcados com  requerem `Authorization: Bearer <jwt>`.

---

## Autenticação (AgroSolution.Identity)

### POST /api/auth/register
Cadastra um novo produtor.

**Body**
```json
{
  "name": "João Silva",
  "email": "joao@fazenda.com",
  "password": "MinhaS3nh@Forte"
}
```

**201 Created**
```json
{ "id": "uuid", "name": "João Silva", "email": "joao@fazenda.com" }
```

---

### POST /api/auth/login
Autentica e retorna JWT.

**Body**
```json
{ "email": "joao@fazenda.com", "password": "MinhaS3nh@Forte" }
```

**200 OK**
```json
{ "token": "eyJhbGci...", "expiresAt": "2026-02-25T10:00:00Z" }
```

---

## Propriedades

### POST /api/properties 
Cria uma propriedade.

**Body**
```json
{ "name": "Fazenda São João", "location": "Mato Grosso" }
```

### GET /api/properties 
Lista todas as propriedades do produtor autenticado.

---

## Talhões

### POST /api/plots 
Cria um talhão vinculado a uma propriedade.

**Body**
```json
{
  "name": "Talhão Norte",
  "cropType": "Soja",
  "areaInHectares": 45.5,
  "propertyId": "uuid"
}
```

### GET /api/plots/{id} 
Retorna um talhão pelo ID.

---

## Dados IoT

### POST /api/iot/data 
Recebe dados brutos de um dispositivo.

**Headers**
```
X-Device-Type: 1   (1=TemperatureSensor, 2=HumiditySensor, 3=PrecipitationSensor, 4=WeatherStationNode)
Content-Type: application/json
```

**Body (HumiditySensor)**
```json
{ "plotId": "uuid", "value": 22.5, "unit": "%" }
```

**Body (WeatherStationNode)**
```json
{
  "plotId": "uuid",
  "telemetry": { "temperature": 34.1, "humidity": 18.0, "precipitation": 0 }
}
```

**200 OK**
```json
{ "success": true, "data": { "id": "uuid", "status": "Pending" } }
```

---

### GET /api/iot/data/{plotId}?from=&to= 
Retorna leituras de um talhão em um intervalo de tempo.

**Query params**
| Param | Tipo | Obrigatório | Obs |
|---|---|---|---|
| `from` | `DateTime` | sim | ISO 8601 |
| `to` | `DateTime` | sim | ISO 8601 |

Limite: máximo 90 dias por consulta.

**200 OK**
```json
[
  {
    "id": "uuid",
    "plotId": "uuid",
    "deviceType": 2,
    "rawData": "{\"value\":22.5}",
    "receivedAt": "2026-02-24T08:00:00Z",
    "processingStatus": "Processed"
  }
]
```

**400 Bad Request**  intervalo inválido ou maior que 90 dias.

---

## Alertas

### GET /api/alerts/{plotId} 
Retorna todos os alertas (ativos e resolvidos) de um talhão.

**200 OK**
```json
[
  {
    "id": "uuid",
    "plotId": "uuid",
    "type": "Drought",
    "message": "Todas as leituras de umidade abaixo de 30% nas últimas 24h.",
    "triggeredAt": "2026-02-24T06:00:00Z",
    "resolvedAt": null,
    "isActive": true
  }
]
```

**Tipos de alerta**

| Valor | Nome | Regra |
|---|---|---|
| `1` | `Drought` | Todas leituras de umidade < 30% nas últimas 24h (mín. 2 leituras) |
| `2` | `ExtremeHeat` | (planejado) |
| `3` | `HeavyRain` | (planejado) |

---

## Códigos de erro comuns

| HTTP | Significado |
|---|---|
| `400` | Dados inválidos ou regra de negócio violada |
| `401` | Token ausente ou expirado |
| `404` | Recurso não encontrado |
| `500` | Erro interno (ver logs) |