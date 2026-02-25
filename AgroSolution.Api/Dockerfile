# ─────────────────────────────────────────────────────────────────────────────
# AgroSolution.Api — Multi-stage Dockerfile
# Build:  docker build -f AgroSolution.Api/Dockerfile -t agrosolution-api .
#         (run from solution root so COPY can access AgroSolution.Core)
# ─────────────────────────────────────────────────────────────────────────────

# ── Stage 1: Restore (maximise layer cache) ───────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS restore
WORKDIR /src

# Copy only .csproj + sln first — this layer is cached unless deps change.
COPY AgroSolution.Management.sln                            ./
COPY AgroSolution.Core/AgroSolution.Core.csproj             AgroSolution.Core/
COPY AgroSolution.Api/AgroSolution.Api.csproj               AgroSolution.Api/

RUN dotnet restore AgroSolution.Api/AgroSolution.Api.csproj

# ── Stage 2: Build & Publish ──────────────────────────────────────────────────
# Extend restore stage so obj/project.assets.json is available for --no-restore
FROM restore AS publish

# Copy all source files after restore cache is established.
COPY AgroSolution.Core/ AgroSolution.Core/
COPY AgroSolution.Api/  AgroSolution.Api/

RUN dotnet publish AgroSolution.Api/AgroSolution.Api.csproj \
    -c Release \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false

# ── Stage 3: Runtime (alpine — ~100 MB vs ~220 MB debian) ───────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Non-root user (Alpine/BusyBox syntax: -S = system)
RUN addgroup -S agro && adduser -S -G agro agro
USER agro

COPY --from=publish /app/publish .

# ASP.NET Core listens on HTTP 8080 inside the container.
# Use Kestrel directly; no HTTPS termination (offloaded to reverse proxy/k8s ingress).
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "AgroSolution.Api.dll"]
