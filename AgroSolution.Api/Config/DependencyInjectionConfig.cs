using AgroSolution.Core.App.Features.AddPlot;
using AgroSolution.Core.App.Features.CreateProperty;
using AgroSolution.Core.App.Features.GetProperties;
using AgroSolution.Core.Domain.Interfaces;
using AgroSolution.Core.Infra.Repositories;

namespace AgroSolution.Api.Config;

public static class DependencyInjectionConfig
{
    public static IServiceCollection ResolveDependencies(this IServiceCollection services)
    {
        services.AddScoped<IPropertyRepository, PropertyRepository>();

        services.AddScoped<ICreateProperty, CreateProperty>();
        services.AddScoped<IAddPlot, AddPlot>();
        services.AddScoped<IGetProperties, GetProperties>();

        return services;
    }
}