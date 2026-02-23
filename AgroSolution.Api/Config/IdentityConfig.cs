using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AgroSolution.Api.Config;

public static class IdentityConfig
{
    public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var secretKey = configuration["JwtSettings:SecretKey"];
        
        if (string.IsNullOrEmpty(secretKey))
            throw new Exception("JwtSettings:SecretKey não foi encontrado no appsettings.");

        var key = Encoding.ASCII.GetBytes(secretKey);
        
        System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["JwtSettings:Audience"],
                     ClockSkew = TimeSpan.Zero 
                };

                // BLOCO DE LOGS PARA DEBUG
                x.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("\n--- [AUTH FAILED] ---");
                        Console.WriteLine($"Motivo: {context.Exception.Message}");
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            Console.WriteLine("O token está expirado.");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("\n--- [AUTH SUCCESS] ---");
                        Console.WriteLine($"Usuário: {context.Principal?.Identity?.Name}");
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Headers["Authorization"];
                        Console.WriteLine($"\n--- [TOKEN RECEIVED] ---");
                        Console.WriteLine($"Header: {token}");
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
}