# AgroSolution.Management

**Versão:** 3.1 | **Atualizado:** 25/02/2026 | **Status:** Etapa 3 — Kubernetes + CI/CD

---

## Objetivo

Plataforma de gestão de propriedades agrícolas com:

- Cadastro de propriedades e talhões (parcelas)
- Recepção e processamento assíncrono de dados IoT via RabbitMQ
- Motor de alertas agronômicos automáticos (Seca, CalorExtremo, ChuvaIntensa)
- Dashboard de histórico de leituras por talhão
- Autenticação JWT via microserviço `AgroSolution.Identity`

---

## Stack

| Camada | Tecnologia |
|---|---|
| API | ASP.NET Core 9.0 / C# |
| Mensageria | RabbitMQ 3.13 |
| Banco | PostgreSQL 16 (Docker) |
| ORM | Entity Framework Core 9 + Npgsql |
| Auth | JWT HS256 — emitido por AgroSolution.Identity |
| Worker | .NET Generic Host Worker Service |
| Containers | Docker (imagens Alpine ~100 MB) |
| Orquestração | Kubernetes (minikube) — manifests em `k8s/` |
| Observabilidade | Prometheus + Grafana (`/metrics` via prometheus-net) |
| CI/CD | GitHub Actions — build → test → Docker GHCR → kubectl apply |

---

## Executar localmente (setup completo em 1 comando)

### Pré-requisitos

| Ferramenta | Versão mínima | Download |
|---|---|---|
| .NET SDK | 9.0 | https://dot.net/download |
| Docker Desktop | 4.x | https://docker.com |
| PowerShell | 5.1 (Windows) / 7+ (Mac/Linux) | incluso no Windows |

> **Docker Desktop** deve estar instalado; o script o inicia automaticamente se necessário.

### 1 comando — execução completa

```powershell
# 1. Clone o repositório
git clone https://github.com/JoaoTerres/AgroSolution.Management.git
cd AgroSolution.Management

# 2. Autorize scripts e execute o start
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\.scripts\Start-Local.ps1
```

O script realiza automaticamente:

| Etapa | O que faz |
|---|---|
| ✔ Pré-requisitos | Verifica .NET 9 SDK e Docker instalados |
| ✔ `.env` | Cria `.env` a partir de `.env.example` (na primeira execução) |
| ✔ Docker Desktop | Inicia o daemon se não estiver rodando |
| ✔ Infra | `docker compose up -d postgres rabbitmq` + aguarda healthcheck |
| ✔ Migrations | `dotnet ef database update` para Management e Identity |
| ✔ Build | `dotnet build` da solução completa |
| ✔ Serviços | Abre janelas separadas com Identity (5001), Api (5034) e Worker |
| ✔ Health wait | Aguarda `/health` retornar 200 em ambas as APIs |
| ✔ Smoke test | Valida o fluxo completo D-04 (11 passos) |
| ✔ Sumário | Exibe todas as URLs e comandos úteis |

### Opções do script

```powershell
# Reiniciar sem rebuild (código não mudou)
.\.scripts\Start-Local.ps1 -SkipBuild

# Subir apenas os serviços sem smoke test
.\.scripts\Start-Local.ps1 -SkipSmokeTest

# Combinados
.\.scripts\Start-Local.ps1 -SkipBuild -SkipSmokeTest
```

### URLs após o start

| Serviço | URL |
|---|---|
| API REST | http://localhost:5034 |
| API Swagger | http://localhost:5034/swagger |
| Identity | http://localhost:5001 |
| Identity Swagger | http://localhost:5001/swagger |
| Métricas Prometheus | http://localhost:5034/metrics |
| RabbitMQ Management | http://localhost:15672 (agro / agro123) |
| pgAdmin | http://localhost:54320 (admin@agrosolution.local / admin123) |

### Parar o ambiente

```powershell
# Para os containers Docker
docker compose down

# Para os processos .NET
Get-Process dotnet | Stop-Process -Force
```

---

## Quick Start (local) — passo a passo manual

Prefere executar etapa por etapa? Siga o guia abaixo.

### Pré-requisitos
- .NET 9 SDK
- Docker Desktop em execução

### 1. Infra (postgres + rabbitmq)
```powershell
cp .env.example .env          # ajuste senhas se necessário
docker compose up -d postgres rabbitmq
```

### 2. Migrations
```powershell
cd AgroSolution.Api;      dotnet ef database update; cd ..
cd AgroSolution.Identity; dotnet ef database update; cd ..
```

### 3. Iniciar serviços
```powershell
# Em terminais separados:
cd AgroSolution.Identity; dotnet run --urls http://localhost:5001
cd AgroSolution.Api;      dotnet run --urls http://localhost:5034
cd AgroSolution.Worker;   dotnet run
```

### 4. Testes unitários
```powershell
dotnet test
```

### 5. Smoke test (D-04 — 11 passos)
```powershell
.\.scripts\Invoke-SmokeTest.ps1
# Kubernetes: .\.scripts\Invoke-SmokeTest.ps1 -ApiUrl http://localhost:30080 -IdentityUrl http://localhost:30081
```

### 6. Benchmark de carga
```powershell
.\.scripts\Invoke-LoadTest.ps1 -DurationSeconds 30 -Concurrency 10
# Salva resultados em benchmark/benchmark_<timestamp>.json
```

---

## Projetos na solução

| Projeto | Porta | Descrição |
|---|---|---|
| `AgroSolution.Api` | 5034 | API REST principal |
| `AgroSolution.Identity` | 5001 | Microserviço de autenticação JWT |
| `AgroSolution.Worker` | — | Producer + Consumer RabbitMQ |
| `AgroSolution.Core` | — | Domínio, casos de uso, infra |
| `AgroSolution.Core.Tests` | — | Testes unitários (46 casos) |

---

## Convenções

- Código: C# / inglês
- Documentação: Português
- Commits: Conventional Commits (`feat:`, `fix:`, `hotfix:`, `docs:`)
- Testes: xUnit + NSubstitute

---

## Status do Projeto

### Completo (Etapa 1 + 2 + 3)
- FR-01 Autenticação JWT (AgroSolution.Identity)
- FR-02 CRUD de Propriedades e Talhões
- FR-03 Ingestão de dados IoT via API
- FR-04 Dashboard de histórico (`GET /api/iot/data/{plotId}`)
- FR-05 Motor de alertas (Seca, CalorExtremo, ChuvaIntensa)
- TR-01 Microserviços (Api + Identity + Worker)
- TR-02 Kubernetes — 12 manifests em `k8s/` (incl. NetworkPolicy)
- TR-03 Observabilidade — Prometheus (`/metrics`) + Grafana
- TR-04 Mensageria RabbitMQ (Producer + Consumer + DLQ)
- TR-05 CI/CD — GitHub Actions: build → test → Docker GHCR → kubectl apply
- 46 testes unitários passando
- Smoke test 11/11 validado

### Em Progresso
- DataAnnotations / FluentValidation nos DTOs de input
- Diagrama de arquitetura (D-01)

### Não Iniciado
- Frontend SPA
- OPT-03: Integração com API climática

---

## Indice de documentacao

| Secao | Link |
|---|---|
| Arquitetura | [01-Arquitetura](../01-Arquitetura) |
| Especificacoes | [02-Especificacoes](../02-Especificacoes) |
| Guias de Desenvolvimento | [03-GuiasDesenvolvimento](../03-GuiasDesenvolvimento) |
| API Reference | [04-API](../04-API) |
| Banco de Dados | [05-Banco-de-Dados](../05-Banco-de-Dados) |
| Testes | [06-Testes](../06-Testes) |
| Seguranca | [07-Seguranca](../07-Seguranca) |
| Deploy / CI-CD | [08-Deploy](../08-Deploy) |
| FAQ | [09-FAQ](../09-FAQ) |

---

## Performance Medida (Benchmark)

> Valores **reais** coletados com `.scripts/Invoke-LoadTest.ps1`  
> Ambiente: docker-compose local, PostgreSQL + RabbitMQ em containers, Windows 12-core  
> Configuracao: **10 workers concorrentes x 30 segundos** — 3.497 escritas + 3.496 leituras

| Endpoint | RPS | Min ms | P50 ms | P95 ms | P99 ms | Sucesso |
|---|---|---|---|---|---|---|
| `POST /api/iot/data` (escrita IoT) | 106,9 | 3 | 11 | 50 | 82 | 100% |
| `GET /api/iot/data/{plotId}` (leitura) | 106,9 | 4 | 57 | 130 | 197 | 100% |

```powershell
# Reproduzir o benchmark:
.\.scripts\Invoke-LoadTest.ps1 -DurationSeconds 30 -Concurrency 10
```

## Limites de recursos (Kubernetes — servidor low-price 2 vCPU / 2 GB)

| Servico | CPU req | CPU lim | Mem req | Mem lim | Imagem base |
|---|---|---|---|---|---|
| Api | 75m | 200m | 96 Mi | 192 Mi | aspnet:9.0-alpine |
| Identity | 75m | 200m | 96 Mi | 192 Mi | aspnet:9.0-alpine |
| Worker | 50m | 150m | 64 Mi | 128 Mi | aspnet:9.0-alpine |
| PostgreSQL | 100m | 300m | 128 Mi | 256 Mi | postgres:16-alpine |
| RabbitMQ | 100m | 250m | 128 Mi | 240 Mi | rabbitmq:3.13-mgmt |
| Prometheus | 50m | 200m | 64 Mi | 192 Mi | prom/prometheus |
| Grafana | 50m | 150m | 64 Mi | 128 Mi | grafana/grafana |

> Imagens Alpine reduzem o tamanho de **~220 MB para ~100 MB** por servico.  
> Budget total: 500m CPU req / 640 Mi RAM req — compativel com 2 vCPU / 2 GB.

---

**Proximo passo:** Leia [Arquitetura](../01-Arquitetura) para entender a estrutura do projeto.
