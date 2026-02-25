using AgroSolution.Core.Infra.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AgroSolution.Worker.Messaging;

/// <summary>
/// Gerencia o ciclo de vida de uma única <see cref="IConnection"/> RabbitMQ
/// compartilhada entre Producer e Consumer workers.
///
/// Registrar como Singleton: uma conexão TCP por processo é o padrão
/// recomendado pelo RabbitMQ. Canais (IModel) são criados por worker.
/// </summary>
public sealed class RabbitMQConnectionManager : IDisposable
{
    private readonly IConnection _connection;
    private bool _disposed;

    public RabbitMQConnectionManager(
        IOptions<RabbitMQSettings> options,
        ILogger<RabbitMQConnectionManager> logger)
    {
        var s = options.Value;

        var factory = new ConnectionFactory
        {
            HostName             = s.Host,
            Port                 = s.Port,
            UserName             = s.Username,
            Password             = s.Password,
            VirtualHost          = s.VirtualHost,
            // Required for AsyncEventingBasicConsumer used by ConsumerWorker
            DispatchConsumersAsync = true,
            // Automatic recovery reconecta após queda de rede
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval  = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection("agrosolution-worker");
        logger.LogInformation(
            "RabbitMQ: connected to {Host}:{Port} vhost={VHost}",
            s.Host, s.Port, s.VirtualHost);
    }

    /// <summary>Cria um novo canal (IModel). Cada worker deve ter o seu.</summary>
    public IModel CreateChannel() => _connection.CreateModel();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _connection.Close(); } catch { /* best-effort */ }
        _connection.Dispose();
    }
}
