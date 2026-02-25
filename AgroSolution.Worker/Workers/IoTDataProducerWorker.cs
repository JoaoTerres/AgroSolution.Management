using System.Text.Json;
using AgroSolution.Core.App.DTO;
using Microsoft.Extensions.Hosting;
using AgroSolution.Core.Domain.Interfaces;
using AgroSolution.Core.Infra.Messaging;
using AgroSolution.Worker.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AgroSolution.Worker.Workers;

/// <summary>
/// Background worker que faz polling na tabela IoTData (status = Pending)
/// e publica cada registro no exchange RabbitMQ com a routing key
/// correspondente ao tipo de dispositivo.
///
/// Fluxo por ciclo:
///   1. GetPendingAsync(batch)               → busca registros Pending no BD
///   2. Para cada registro:
///      a. Serializa em IoTEventMessage
///      b. BasicPublish() no exchange iot.events com routing key do tipo
///      c. MarkAsQueued(iotDataId)           → atualiza status no BD
/// </summary>
public sealed class IoTDataProducerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMQConnectionManager _connectionManager;
    private readonly RabbitMQSettings _rmq;
    private readonly ILogger<IoTDataProducerWorker> _logger;

    private IModel? _channel;

    public IoTDataProducerWorker(
        IServiceScopeFactory scopeFactory,
        RabbitMQConnectionManager connectionManager,
        IOptions<RabbitMQSettings> rmqOptions,
        ILogger<IoTDataProducerWorker> logger)
    {
        _scopeFactory      = scopeFactory;
        _connectionManager = connectionManager;
        _rmq               = rmqOptions.Value;
        _logger            = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Criar canal próprio do worker
        _channel = _connectionManager.CreateChannel();

        // Declaração idempotente do exchange principal (garante que exista mesmo
        // que as definitions.json ainda não tenham sido carregadas)
        _channel.ExchangeDeclare(
            exchange:    _rmq.ExchangeName,
            type:        ExchangeType.Topic,
            durable:     true,
            autoDelete:  false);

        _logger.LogInformation(
            "ProducerWorker started. Exchange={Exchange} Interval={Interval}ms Batch={Batch}",
            _rmq.ExchangeName, _rmq.ProducerPollingIntervalMs, _rmq.ProducerBatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAndPublishAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProducerWorker: unhandled error during poll cycle");
            }

            try
            {
                await Task.Delay(_rmq.ProducerPollingIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("ProducerWorker stopped.");
    }

    private async Task PollAndPublishAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IIoTDataRepository>();

        var pending = (await repo.GetPendingAsync(_rmq.ProducerBatchSize)).ToList();
        if (pending.Count == 0) return;

        var published = 0;
        var failed    = 0;

        foreach (var data in pending)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var routingKey = GetRoutingKey(data.DeviceType);

                var message = new IoTEventMessage
                {
                    IoTDataId       = data.Id,
                    PlotId          = data.PlotId,
                    DeviceType      = data.DeviceType,
                    RawData         = data.RawData,
                    DeviceTimestamp = data.DeviceTimestamp,
                    PublishedAt     = DateTime.UtcNow
                };

                var body  = JsonSerializer.SerializeToUtf8Bytes(message);
                var props = _channel!.CreateBasicProperties();
                props.Persistent  = true;
                props.MessageId   = data.Id.ToString();
                props.ContentType = "application/json";

                _channel.BasicPublish(
                    exchange:   _rmq.ExchangeName,
                    routingKey: routingKey,
                    basicProperties: props,
                    body:       body);

                // Marca como Queued APÓS publish bem-sucedido
                data.MarkAsQueued(data.Id.ToString());
                await repo.UpdateAsync(data);

                published++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "ProducerWorker: failed to publish IoTData {Id}", data.Id);
                failed++;
            }
        }

        _logger.LogInformation(
            "ProducerWorker: published={Published} failed={Failed} (batch of {Total})",
            published, failed, pending.Count);
    }

    /// <summary>
    /// Mapeia o tipo de dispositivo para a routing key configurada.
    /// Novos tipos de sensor: adicionar aqui e atualizar definitions.json.
    /// </summary>
    private string GetRoutingKey(IoTDeviceType deviceType) => deviceType switch
    {
        IoTDeviceType.TemperatureSensor   => _rmq.RoutingKeyTemperature,
        IoTDeviceType.HumiditySensor      => _rmq.RoutingKeyHumidity,
        IoTDeviceType.PrecipitationSensor => _rmq.RoutingKeyPrecipitation,
        IoTDeviceType.WeatherStationNode  => _rmq.RoutingKeyWeather,
        _ => _rmq.RoutingKeyTemperature
    };

    public override void Dispose()
    {
        try { _channel?.Close(); } catch { /* best-effort */ }
        _channel?.Dispose();
        base.Dispose();
    }
}
