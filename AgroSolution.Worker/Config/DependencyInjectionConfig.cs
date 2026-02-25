using AgroSolution.Core.App.Features.AlertEngine;
using AgroSolution.Core.Domain.Interfaces;
using AgroSolution.Core.Infra.Data;
using AgroSolution.Core.Infra.Messaging;
using AgroSolution.Core.Infra.Repositories;
using AgroSolution.Worker.Messaging;
using AgroSolution.Worker.Workers;
using Microsoft.EntityFrameworkCore;

namespace AgroSolution.Worker.Config;

public static class DependencyInjectionConfig
{
    /// <summary>
    /// Registra todos os serviços necessários para o Worker host.
    /// </summary>
    public static IServiceCollection AddWorkerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF Core (scoped — criado a cada job cycle via IServiceScopeFactory) ──
        services.AddDbContext<ManagementDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ManagementConnection")));

        // ── Repositórios (scoped via DbContext) ──────────────────────────────
        services.AddScoped<IIoTDataRepository, IoTDataRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();

        // ── Serviços de domínio ───────────────────────────────────────────────
        services.AddScoped<IAlertEngineService, AlertEngineService>();

        // ── RabbitMQ settings ────────────────────────────────────────────────
        services.Configure<RabbitMQSettings>(
            configuration.GetSection(RabbitMQSettings.SectionName));

        // ── ConnectionManager (Singleton — uma conexão TCP por processo) ─────
        services.AddSingleton<RabbitMQConnectionManager>();

        // ── Background workers ───────────────────────────────────────────────
        services.AddHostedService<IoTDataProducerWorker>();
        services.AddHostedService<IoTDataConsumerWorker>();

        return services;
    }
}
