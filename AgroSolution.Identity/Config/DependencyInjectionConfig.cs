using AgroSolution.Identity.App.Features.Login;
using AgroSolution.Identity.App.Features.RegisterProducer;
using AgroSolution.Identity.Domain.Interfaces;
using AgroSolution.Identity.Infra.Repositories;
using AgroSolution.Identity.Infra.Services;

namespace AgroSolution.Identity.Config;

public static class DependencyInjectionConfig
{
    public static IServiceCollection ResolveDependencies(this IServiceCollection services)
    {
        // Repositórios
        services.AddScoped<IProducerRepository, ProducerRepository>();

        // Casos de Uso
        services.AddScoped<IRegisterProducer, RegisterProducer>();
        services.AddScoped<ILogin, Login>();

        // Serviços de Infra
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
