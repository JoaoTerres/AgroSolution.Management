using AgroSolution.Core.App.Features.AddPlot;
using AgroSolution.Core.App.Features.CreateProperty;
using AgroSolution.Core.App.Features.GetProperties;
using AgroSolution.Core.App.Features.ReceiveIoTData;
using AgroSolution.Core.App.Validation;
using AgroSolution.Core.Domain.Interfaces;
using AgroSolution.Core.Infra.Repositories;

namespace AgroSolution.Api.Config;

public static class DependencyInjectionConfig
{
    public static IServiceCollection ResolveDependencies(this IServiceCollection services)
    {
        // Repositórios
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IIoTDataRepository, IoTDataRepository>();

        // Casos de Uso - Property
        services.AddScoped<ICreateProperty, CreateProperty>();
        services.AddScoped<IGetProperties, GetProperties>();

        // Casos de Uso - Plot
        services.AddScoped<IAddPlot, AddPlot>();

        // Casos de Uso - IoT Data
        services.AddScoped<IReceiveIoTData, ReceiveIoTData>();

        // Validadores IoT
        services.AddSingleton<IoTDeviceValidatorFactory>();

        return services;
    }
}