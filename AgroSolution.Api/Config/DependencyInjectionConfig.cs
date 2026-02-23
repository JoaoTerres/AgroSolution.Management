using AgroSolution.Core.App.Features.AddPlot;
using AgroSolution.Core.App.Features.CreateProperty;
using AgroSolution.Core.App.Features.GetByIdPlot;
using AgroSolution.Core.App.Features.GetProperties;
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
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IGetByIdPlot, GetByIdPlot>();
        services.AddScoped<ICreateProperty, CreateProperty>();
        services.AddScoped<IAddPlot, AddPlot>();
        services.AddScoped<IGetProperties, GetProperties>();

        return services;
    }
}