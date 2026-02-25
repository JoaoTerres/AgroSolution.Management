namespace AgroSolution.Core.Infra.Messaging;

/// <summary>
/// Strongly-typed configuration for RabbitMQ.
/// Bind in DI with: services.Configure&lt;RabbitMQSettings&gt;(config.GetSection(RabbitMQSettings.SectionName));
/// Inject with: IOptions&lt;RabbitMQSettings&gt;
///
/// All values are backed by appsettings.json → "RabbitMQ" section,
/// and overridable by environment variables (RABBITMQ__Host, etc.)
/// </summary>
public sealed class RabbitMQSettings
{
    public const string SectionName = "RabbitMQ";

    // ── Connection ──────────────────────────────────────────────────────────
    public string Host        { get; set; } = "localhost";
    public int    Port        { get; set; } = 5672;
    public string Username    { get; set; } = "guest";
    public string Password    { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    // ── Exchange ────────────────────────────────────────────────────────────
    /// <summary>Main topic exchange. Must match definitions.json.</summary>
    public string ExchangeName { get; set; } = "iot.events";

    /// <summary>Dead-letter exchange for failed/rejected messages.</summary>
    public string DeadLetterExchangeName { get; set; } = "iot.dead-letter";

    // ── Routing Keys (Producer → must match bindings in definitions.json) ──
    public string RoutingKeyTemperature   { get; set; } = "iot.temperature";
    public string RoutingKeyHumidity      { get; set; } = "iot.humidity";
    public string RoutingKeyPrecipitation { get; set; } = "iot.precipitation";
    public string RoutingKeyWeather       { get; set; } = "iot.weather";

    // ── Queue Names (Consumer → must match queues in definitions.json) ─────
    public string QueueTemperature   { get; set; } = "queue.temperature";
    public string QueueHumidity      { get; set; } = "queue.humidity";
    public string QueuePrecipitation { get; set; } = "queue.precipitation";
    public string QueueWeather       { get; set; } = "queue.weather";
    public string QueueDeadLetter    { get; set; } = "queue.dead-letter";

    // ── Producer behaviour ──────────────────────────────────────────────────
    /// <summary>Polling interval (ms) for the producer worker to fetch Pending IoTData records.</summary>
    public int ProducerPollingIntervalMs { get; set; } = 5_000;

    /// <summary>Max records fetched per polling cycle. Maps to IIoTDataRepository.GetPendingAsync(limit).</summary>
    public int ProducerBatchSize { get; set; } = 100;

    // ── Consumer behaviour ──────────────────────────────────────────────────
    /// <summary>Number of messages the consumer prefetches (QoS). Keep low for fair dispatch.</summary>
    public ushort ConsumerPrefetchCount { get; set; } = 10;
}
