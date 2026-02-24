# ─────────────────────────────────────────────────────────────────────────────
# AgroSolution.Identity — Multi-stage Dockerfile
# Build:  docker build -f AgroSolution.Identity/Dockerfile -t agrosolution-identity .
#         (run from solution root)
# ─────────────────────────────────────────────────────────────────────────────

# ── Stage 1: Restore ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /src

COPY AgroSolution.Management.sln                                        ./
COPY AgroSolution.Identity/AgroSolution.Identity.csproj                 AgroSolution.Identity/

RUN dotnet restore AgroSolution.Identity/AgroSolution.Identity.csproj

# ── Stage 2: Build & Publish ──────────────────────────────────────────────────
FROM restore AS publish
WORKDIR /src

COPY AgroSolution.Identity/ AgroSolution.Identity/

RUN dotnet publish AgroSolution.Identity/AgroSolution.Identity.csproj \
    -c Release \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false

# ── Stage 3: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN addgroup --system agro && adduser --system --ingroup agro agro
USER agro

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8081
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8081
ENTRYPOINT ["dotnet", "AgroSolution.Identity.dll"]
