using System.Text;
using AgroSolution.Api.Config;
using AgroSolution.Api.Middlewares;
using AgroSolution.Core.Infra.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// ─── Authentication / JWT ──────────────────────────────────────────────────
var jwtSecret   = builder.Configuration["Jwt:SecretKey"]!;
var jwtIssuer   = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer           = true,
            ValidIssuer              = jwtIssuer,
            ValidateAudience         = true,
            ValidAudience            = jwtAudience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ─── MVC / Swagger / DI ────────────────────────────────────────────────────
builder.Services.AddControllers();builder.Services.AddHealthChecks();builder.Services.AddSwaggerConfiguration();
builder.Services.ResolveDependencies(builder.Configuration);

// ─── App pipeline ──────────────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

// ─── Prometheus HTTP metrics ────────────────────────────────────────────────
app.UseHttpMetrics();     // auto-instruments all HTTP requests
app.MapMetrics();         // exposes GET /metrics (prometheus-net)
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
    app.UseSwaggerConfiguration();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─── Auto-migrate on startup (dev only) ───────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ManagementDbContext>();
    db.Database.Migrate();
}

app.Run();