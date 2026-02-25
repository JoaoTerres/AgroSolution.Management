using AgroSolution.Core.App.Features.AddPlot;
using AgroSolution.Core.App.Features.AlertEngine;
using AgroSolution.Core.App.Features.CreateProperty;
using AgroSolution.Core.App.Features.GetAlerts;
using AgroSolution.Core.App.Features.GetByIdPlot;
using AgroSolution.Core.App.Features.GetIoTDataByRange;
using AgroSolution.Core.App.Features.GetProperties;
using AgroSolution.Core.App.Features.ReceiveIoTData;
using AgroSolution.Core.App.Validation;
using AgroSolution.Core.Domain.Interfaces;
using AgroSolution.Core.Infra.Data;
using AgroSolution.Core.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgroSolution.Api.Config;

public static class DependencyInjectionConfig
{
    public static IServiceCollection ResolveDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ManagementDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ManagementConnection")));

        // Repositórios
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IIoTDataRepository, IoTDataRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();

        // Casos de Uso - Property
        services.AddScoped<IGetByIdPlot, GetByIdPlot>();
        services.AddScoped<ICreateProperty, CreateProperty>();
        services.AddScoped<IGetProperties, GetProperties>();

        // Casos de Uso - Plot
        services.AddScoped<IAddPlot, AddPlot>();

        // Casos de Uso - IoT Data
        services.AddScoped<IReceiveIoTData, ReceiveIoTData>();
        services.AddScoped<IGetIoTDataByRange, GetIoTDataByRange>();

        // Casos de Uso - Alertas
        services.AddScoped<IGetAlerts, GetAlerts>();
        services.AddScoped<IAlertEngineService, AlertEngineService>();

        // Device repository (in-memory for local testing)
        services.AddScoped<IDeviceRepository, InMemoryDeviceRepository>();

        // Validadores IoT
        services.AddSingleton<IoTDeviceValidatorFactory>();

        return services;
    }
}