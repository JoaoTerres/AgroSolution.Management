# 📋 Próximas Etapas - Roadmap Etapa 2 & 3

---
**Data:** 12/02/2026  
**Etapa Atual:** 1 (Concluída)  
**Próxima Etapa:** 2 (RabbitMQ)
---

## 🎯 Visão Geral

A Etapa 1 criou a API para **receber** dados IoT. Agora precisamos criar o sistema para **processar** esses dados de forma assíncrona usando RabbitMQ.

---

## 📋 Etapa 2: RabbitMQ Producer & Consumer

### Objetivo
Implementar fila de processamento de dados IoT com padrão Publisher/Subscriber.

### Componentes a Criar

#### 1️⃣ Producer Worker
**O que faz:**
- Lê dados com `Status = Pending` do BD
- Valida se talhão existe
- Publica em exchange RabbitMQ
- Marca como `Status = Queued`
- Armazena `ProcessingQueueId`

**Arquivos:**
```
Worker/
├── IoTDataProducerWorker.cs
├── Interfaces/
│   └── IIoTDataProducerWorker.cs
└── Services/
    └── RabbitMQPublisher.cs
```

**Classe Exemplo:**
```csharp
public interface IIoTDataProducerWorker
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

public class IoTDataProducerWorker : IIoTDataProducerWorker
{
    private readonly IIoTDataRepository _repository;
    private readonly IRabbitMQPublisher _publisher;
    private readonly IPropertyRepository _propertyRepository;
    
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // 1. Busca dados pendentes
        var pendingData = await _repository.GetPendingAsync(100);
        
        foreach (var data in pendingData)
        {
            try
            {
                // 2. Valida se talhão existe
                // 3. Publica em RabbitMQ (exchange: iot.{deviceType})
                // 4. Marca como Queued
                data.MarkAsQueued(jobId);
                await _repository.UpdateAsync(data);
            }
            catch (Exception ex)
            {
                data.MarkAsFailed(ex.Message);
                await _repository.UpdateAsync(data);
            }
        }
    }
}
```

#### 2️⃣ Consumer Worker (Genérico)
**O que faz:**
- Consome mensagens de uma fila
- Extrai dados usando validador específico
- Armazena em Data Lake (mock por agora)
- Marca como `Processed` ou `Failed`

**Arquivos:**
```
Worker/
├── IoTDataConsumerWorker.cs
├── Interfaces/
│   ├── IIoTDataConsumerWorker.cs
│   └── IIoTDataProcessor.cs
└── Processors/
    ├── TemperatureDataProcessor.cs
    ├── HumidityDataProcessor.cs
    └── PrecipitationDataProcessor.cs
```

**Classe Exemplo:**
```csharp
public interface IIoTDataConsumerWorker
{
    Task ExecuteAsync(string queue, CancellationToken cancellationToken);
}

public class IoTDataConsumerWorker : IIoTDataConsumerWorker
{
    private readonly IRabbitMQConsumer _consumer;
    private readonly IServiceProvider _serviceProvider;
    
    public async Task ExecuteAsync(string queue, CancellationToken cancellationToken)
    {
        await _consumer.ConsumeAsync(queue, async (message) =>
        {
            try
            {
                // 1. Deserializa mensagem
                var iotData = JsonSerializer.Deserialize<IoTData>(message);
                
                // 2. Obtém processador correto
                var processor = GetProcessor(iotData.DeviceType);
                
                // 3. Processa dados
                await processor.ProcessAsync(iotData);
                
                // 4. Marca como Processed
                iotData.MarkAsProcessed();
                await _repository.UpdateAsync(iotData);
            }
            catch (Exception ex)
            {
                iotData.MarkAsFailed(ex.Message);
                await _repository.UpdateAsync(iotData);
            }
        }, cancellationToken);
    }
}
```

#### 3️⃣ Configuração RabbitMQ
**O que fazer:**
- Criar exchanges por tipo de dispositivo
- Configurar filas com dead-letter queues
- Definir durabilidade e persistence
- Implementar retry policy

**Exchanges:**
```
Exchange: iot.events
  ├─ Routing Key: iot.temperature → Queue: iot_temperature
  ├─ Routing Key: iot.humidity → Queue: iot_humidity
  └─ Routing Key: iot.precipitation → Queue: iot_precipitation
```

**Classe Exemplo:**
```csharp
public interface IRabbitMQConfiguration
{
    void ConfigureExchanges();
    void ConfigureQueues();
    void ConfigureBindings();
}

public class RabbitMQConfiguration : IRabbitMQConfiguration
{
    public void ConfigureExchanges()
    {
        // Topic exchange para roteamento por tipo
        _channel.ExchangeDeclare(
            "iot.events", 
            "topic", 
            durable: true, 
            autoDelete: false);
    }
    
    public void ConfigureQueues()
    {
        // Queue para temperatura
        _channel.QueueDeclare(
            "iot_temperature",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                {"x-dead-letter-exchange", "iot.events.dlx"},
                {"x-message-ttl", 86400000} // 24h
            });
        
        // ... similar para humidity e precipitation
    }
    
    public void ConfigureBindings()
    {
        _channel.QueueBind(
            "iot_temperature",
            "iot.events",
            "iot.temperature");
        
        // ... similar para outros
    }
}
```

### Estrutura de Diretórios

```
AgroSolution.Core/
├── App/
│   ├── Services/
│   │   ├── RabbitMQ/
│   │   │   ├── IRabbitMQPublisher.cs
│   │   │   ├── IRabbitMQConsumer.cs
│   │   │   ├── IRabbitMQConfiguration.cs
│   │   │   ├── RabbitMQPublisher.cs
│   │   │   ├── RabbitMQConsumer.cs
│   │   │   └── RabbitMQConfiguration.cs
│   │   └── Processing/
│   │       ├── IIoTDataProcessor.cs
│   │       ├── TemperatureProcessor.cs
│   │       ├── HumidityProcessor.cs
│   │       └── PrecipitationProcessor.cs
│   └── Workers/
│       ├── IIoTDataProducerWorker.cs
│       ├── IoTDataProducerWorker.cs
│       ├── IIoTDataConsumerWorker.cs
│       └── IoTDataConsumerWorker.cs
```

### NuGet Packages Necessários

```xml
<PackageReference Include="RabbitMQ.Client" Version="6.x" />
```

---

## 📋 Etapa 3: Data Lake & Analytics (Futuro)

### Objetivo
Armazenar dados processados e criar visualizações.

### Componentes

#### 1️⃣ Data Lake Storage
- Armazenar dados processados em S3/Blob Storage
- Particionamento por data e talhão
- Formato Parquet para eficiência

#### 2️⃣ Analytics
- Agregações por talhão/período
- Alertas automáticos (ex: temp > 35°C)
- Dashboard com métricas

#### 3️⃣ API Analytics
- Endpoints para consultar histórico
- Estatísticas por período
- Comparação com outras fazendas

---

## ⏱️ Timeline Estimada

| Fase | Duração | Componentes |
|------|---------|------------|
| **Etapa 1** | ✅ Concluída | API + Validadores |
| **Etapa 2a** | 3 dias | Producer Worker |
| **Etapa 2b** | 3 dias | Consumer Workers |
| **Etapa 2c** | 2 dias | RabbitMQ Config |
| **Etapa 3a** | 4 dias | Data Lake |
| **Etapa 3b** | 3 dias | Analytics |
| **Etapa 4** | 5 dias | Testes + CI/CD |

---

## 🔍 Como Iniciar Etapa 2

### 1. Criar Nova Branch
```bash
git checkout -b feature/rabbitmq-producer-consumer
```

### 2. Instalar RabbitMQ Localmente
```bash
# Windows com Docker
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# User: guest, Password: guest
# Management UI: http://localhost:15672
```

### 3. Adicionar Pacote NuGet
```bash
dotnet add AgroSolution.Core package RabbitMQ.Client --version 6.x
```

### 4. Criar Interfaces Base
- `IRabbitMQPublisher` - Publicar mensagens
- `IRabbitMQConsumer` - Consumir mensagens
- `IRabbitMQConfiguration` - Configurar exchanges/queues

### 5. Implementar Producer
- Buscar dados pendentes
- Publicar em exchange
- Marcar como queued

### 6. Implementar Consumer
- Consumir de fila
- Processar dados
- Atualizar status

### 7. Testes
- Teste unitário do producer
- Teste unitário do consumer
- Teste de integração completo

---

## 📝 Documentação para Etapa 2

Após completar, criar em `.ai-docs/03-Padroes-Codigo/`:
- `PADROES_RABBITMQ.md` - Padrões de fila

E em `docs/04-API/`:
- `RABBITMQ-SETUP.md` - Como configurar
- `RABBITMQ-TROUBLESHOOTING.md` - Troubleshooting

---

## 💡 Dicas para Implementação

### 1. Idempotência
Operações devem ser seguras se executadas múltiplas vezes:
```csharp
// ✅ Bom - idempotente
if (iotData.ProcessingStatus == Pending)
{
    iotData.MarkAsQueued(jobId);
}

// ❌ Ruim - pode duplicar
iotData.MarkAsQueued(jobId); // Executa sempre
```

### 2. Dead Letter Queue
Mensagens com erro vão para DLQ para análise:
```
iot.events (Main)
  └─ (erro 3x) ──→ iot.events.dlx (Dead Letter)
```

### 3. Retry Policy
```
1ª tentativa: Imediata
2ª tentativa: 30 segundos depois
3ª tentativa: 5 minutos depois
DLQ: Após 3 falhas
```

### 4. Monitoring
```csharp
_logger.LogInformation($"Processados {count} dados");
_metrics.RecordQueueSize(queueSize);
_metrics.RecordProcessingTime(duration);
```

---

## ✅ Checklist Pre-Etapa 2

- [ ] RabbitMQ instalado e rodando
- [ ] .NET packages adicionados
- [ ] Branch criada
- [ ] Documentação lida
- [ ] Padrões entendidos
- [ ] Time alinhado

---

## 📞 Referências

- [RabbitMQ Tutorials](https://www.rabbitmq.com/getstarted.html)
- [RabbitMQ .NET Client](https://github.com/rabbitmq/rabbitmq-dotnet-client)
- [Padrão Pub/Sub](https://www.rabbitmq.com/tutorials/tutorial-five-dotnet.html)

---

**Próximo Passo:** Aguardar instruções para iniciar Etapa 2 🚀
