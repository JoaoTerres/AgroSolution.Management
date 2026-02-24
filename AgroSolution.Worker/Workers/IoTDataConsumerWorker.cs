using System.Text;
using System.Text.Json;
using AgroSolution.Core.App.Features.AlertEngine;
using AgroSolution.Core.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using AgroSolution.Core.Infra.Messaging;
using AgroSolution.Worker.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AgroSolution.Worker.Workers;

/// <summary>
/// Background worker que consome as 4 filas de eventos IoT e efetua
/// o processamento de cada mensagem (persiste estado no BD).
///
/// Fluxo por mensagem:
///   1. Deserializa IoTEventMessage do body JSON
///   2. Busca IoTData por Id no BD  (GetByIdAsync)
///   3. MarkAsProcessed() + UpdateAsync()   → Ack
///   4. Em caso de exceção:
///        MarkAsFailed(errorMessage)         → Nack sem requeue
///        (mensagem roteada para iot.dead-letter pelo RabbitMQ)
///
/// Canal separado por fila:
///   BasicQos por canal garante fair dispatch independente entre filas.
///   AsyncEventingBasicConsumer requer DispatchConsumersAsync=true na
///   ConnectionFactory (configurado em RabbitMQConnectionManager).
/// </summary>
public sealed class IoTDataConsumerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMQConnectionManager _connectionManager;
    private readonly RabbitMQSettings _rmq;
    private readonly ILogger<IoTDataConsumerWorker> _logger;

    private readonly List<IModel> _channels = new();

    public IoTDataConsumerWorker(
        IServiceScopeFactory scopeFactory,
        RabbitMQConnectionManager connectionManager,
        IOptions<RabbitMQSettings> rmqOptions,
        ILogger<IoTDataConsumerWorker> logger)
    {
        _scopeFactory      = scopeFactory;
        _connectionManager = connectionManager;
        _rmq               = rmqOptions.Value;
        _logger            = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Cada fila recebe um canal dedicado para QoS e ack isolados
        var queues = new[]
        {
            _rmq.QueueTemperature,
            _rmq.QueueHumidity,
            _rmq.QueuePrecipitation,
            _rmq.QueueWeather
        };

        foreach (var queue in queues)
        {
            StartQueueConsumer(queue);
        }

        _logger.LogInformation(
            "ConsumerWorker started. Consuming {Count} queue(s): {Queues}",
            queues.Length, string.Join(", ", queues));

        // Bloqueia até o host solicitar shutdown — o consumo é orientado a eventos
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);

        _logger.LogInformation("ConsumerWorker stopped.");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Internals
    // ──────────────────────────────────────────────────────────────────────────

    private void StartQueueConsumer(string queueName)
    {
        var channel = _connectionManager.CreateChannel();
        channel.BasicQos(prefetchSize: 0, prefetchCount: _rmq.ConsumerPrefetchCount, global: false);
        _channels.Add(channel);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += (_, ea) => HandleMessageAsync(channel, queueName, ea);

        channel.BasicConsume(
            queue:       queueName,
            autoAck:     false,   // ack manual: só após processamento bem-sucedido
            consumer:    consumer);
    }

    private async Task HandleMessageAsync(IModel channel, string queueName, BasicDeliverEventArgs ea)
    {
        var body = ea.Body.ToArray();
        IoTEventMessage? message = null;

        try
        {
            message = JsonSerializer.Deserialize<IoTEventMessage>(body);

            if (message is null)
            {
                _logger.LogWarning(
                    "ConsumerWorker [{Queue}]: null message — discarding (ack).", queueName);
                channel.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IIoTDataRepository>();

            var data = await repo.GetByIdAsync(message.IoTDataId);

            if (data is null)
            {
                // Registro não existe (possível reentrega após limpeza do BD) — descarta
                _logger.LogWarning(
                    "ConsumerWorker [{Queue}]: IoTData {Id} not found — discarding (ack).",
                    queueName, message.IoTDataId);
                channel.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            if (data.ProcessingStatus == Core.Domain.Entities.IoTProcessingStatus.Processed)
            {
                // Idempotência: mensagem entregue mais de uma vez
                _logger.LogDebug(
                    "ConsumerWorker [{Queue}]: IoTData {Id} already processed — ack (idempotent).",
                    queueName, data.Id);
                channel.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            data.MarkAsProcessed();
            await repo.UpdateAsync(data);

            // Avalia regras de alerta após processamento bem-sucedido
            try
            {
                var alertEngine = scope.ServiceProvider.GetRequiredService<IAlertEngineService>();
                await alertEngine.EvaluateAsync(data.PlotId, data.DeviceType);
            }
            catch (Exception alertEx)
            {
                // Falha no motor de alertas não deve impedir o ack da mensagem
                _logger.LogError(alertEx,
                    "ConsumerWorker [{Queue}]: AlertEngine failed for IoTData {Id} — continuing.",
                    queueName, data.Id);
            }

            channel.BasicAck(ea.DeliveryTag, multiple: false);

            _logger.LogDebug(
                "ConsumerWorker [{Queue}]: IoTData {Id} processed successfully.",
                queueName, data.Id);
        }
        catch (Exception ex)
        {
            var iotId = message?.IoTDataId.ToString() ?? "(unknown)";
            _logger.LogError(ex,
                "ConsumerWorker [{Queue}]: error processing IoTData {Id} — nacking to DLQ.",
                queueName, iotId);

            // Best-effort: marcar como falhado no BD
            if (message is not null)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IIoTDataRepository>();
                    var data = await repo.GetByIdAsync(message.IoTDataId);
                    if (data is not null)
                    {
                        data.MarkAsFailed(ex.Message);
                        await repo.UpdateAsync(data);
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx,
                        "ConsumerWorker [{Queue}]: failed to mark IoTData {Id} as Failed.",
                        queueName, message.IoTDataId);
                }
            }

            // requeue=false → RabbitMQ roteia para o dead-letter exchange
            channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override void Dispose()
    {
        foreach (var ch in _channels)
        {
            try { ch.Close(); }  catch { /* best-effort */ }
            try { ch.Dispose(); } catch { /* best-effort */ }
        }
        base.Dispose();
    }
}
