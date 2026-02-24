# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore (layer-cache friendly – copy csproj files first)
COPY AgroSolution.Core/AgroSolution.Core.csproj          AgroSolution.Core/
COPY AgroSolution.Worker/AgroSolution.Worker.csproj      AgroSolution.Worker/
RUN dotnet restore AgroSolution.Worker/AgroSolution.Worker.csproj

# Build & publish
COPY AgroSolution.Core/   AgroSolution.Core/
COPY AgroSolution.Worker/ AgroSolution.Worker/
RUN dotnet publish AgroSolution.Worker/AgroSolution.Worker.csproj \
        -c Release \
        -o /app/publish \
        --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Non-root user for security (same convention as Api + Identity)
RUN addgroup --system --gid 1001 agro \
 && adduser  --system --uid 1001 --ingroup agro agro

COPY --from=build /app/publish .
RUN chown -R agro:agro /app

USER agro

ENTRYPOINT ["dotnet", "AgroSolution.Worker.dll"]
