using Microsoft.OpenApi.Models;

namespace AgroSolution.Identity.Config;

public static class SwaggerConfig
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "AgroSolution Identity API",
                Description = "Microsserviço de autenticação e gestão de produtores rurais",
                Contact     = new OpenApiContact { Name = "Equipe AgroSolution", Email = "suporte@agrosolution.com" }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgroSolution Identity v1");
        });

        return app;
    }
}
