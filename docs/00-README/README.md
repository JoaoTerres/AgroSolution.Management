# AgroSolution.Management

**Versão:** 3.0 | **Atualizado:** 25/02/2026 | **Status:** Etapa 3 — Kubernetes + CI/CD

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

## Quick Start (local)

### Pré-requisitos
- .NET 9 SDK
- Docker Desktop

### 1. Subir infra
```bash
cp .env.example .env          # ajuste senhas se necessário
docker compose up -d          # postgres + rabbitmq
```

### 2. Rodar a API
```bash
dotnet run --project AgroSolution.Api
# Swagger: https://localhost:7xxx/swagger
```

### 3. Rodar o Worker
```bash
dotnet run --project AgroSolution.Worker
```

### 4. Rodar testes
```bash
dotnet test
```

### 5. Smoke test (valida fluxo D-04 completo)
```powershell
.scripts\Invoke-SmokeTest.ps1
# Kubernetes:
.scripts\Invoke-SmokeTest.ps1 -ApiUrl http://localhost:30080 -IdentityUrl http://localhost:30081
```

### 6. Benchmark de carga
```powershell
# Rápido (10 s, 5 workers)
.scripts\Invoke-LoadTest.ps1 -DurationSeconds 10 -Concurrency 5

# Completo (60 s, 20 workers)
.scripts\Invoke-LoadTest.ps1 -DurationSeconds 60 -Concurrency 20

# Salva resultados em benchmark/benchmark_<timestamp>.json
```

---

## Projetos na solução

| Projeto | Porta | Descrição |
|---|---|---|
| `AgroSolution.Api` | 7xxx/8080 | API REST principal |
| `AgroSolution.Identity` | 7xxx/8081 | Microserviço de autenticação |
| `AgroSolution.Worker` | — | Producer + Consumer RabbitMQ |
| `AgroSolution.Core` | — | Domínio, casos de uso, infra |

---

## Convenções

- Código: C# / inglês
- Documentação: Português
- Commits: Conventional Commits (`feat:`, `fix:`, `hotfix:`, `docs:`)
- Testes: xUnit + NSubstitute


## 🎯 Objetivo do Projeto

**AgroSolution.Management** é uma plataforma de gestão de propriedades agrícolas e seus talhões (parcelas), fornecendo:

- 📍 Cadastro e gestão de propriedades
- 📊 Organização de talhões por propriedade
- 🔐 Controle de acesso por produtor
- 📈 Base para futuras funcionalidades analíticas

---

## ⚡ Quick Start

### Pré-requisitos
- .NET 9.0
- PostgreSQL 14+
- Git

### Setup Local
```bash
# Clone o repositório
git clone <repo-url>
cd AgroSolution.Management

# Restaure dependências
dotnet restore

# Configure o banco de dados
# (Ver docs/05-Banco-de-Dados)

# Execute a API
dotnet run --project AgroSolution.Api
```

---

## 📚 Índice Rápido

| Seção | Descrição | Link |
|-------|-----------|------|
| Arquitetura | Estrutura e padrões | [01-Arquitetura](../01-Arquitetura) |
| Especificações | Requisitos e funcionalidades | [02-Especificacoes](../02-Especificacoes) |
| Desenvolvimento | Guias e convenções | [03-GuiasDesenvolvimento](../03-GuiasDesenvolvimento) |
| API | Endpoints e schemas | [04-API](../04-API) |
| Banco de Dados | Modelos e migrações | [05-Banco-de-Dados](../05-Banco-de-Dados) |
| Testes | Estratégia e cobertura | [06-Testes](../06-Testes) |
| Segurança | Autenticação e autorização | [07-Seguranca](../07-Seguranca) |
| Deploy | Ambientes e CI/CD | [08-Deploy](../08-Deploy) |
| FAQ | Dúvidas frequentes | [09-FAQ](../09-FAQ) |

---

## 🏗️ Stack Tecnológico

```
Frontend:     (Não iniciado)
API:          ASP.NET Core 9.0 / C#
Banco:        PostgreSQL 14+
ORM:          Entity Framework Core 9.0
Autenticação: JWT Bearer
```

---

## 📋 Status do Projeto

### ✅ Completo (Etapa 1 + 2 + 3)
- FR-01 Autenticação JWT (AgroSolution.Identity)
- FR-02 CRUD de Propriedades e Talhões
- FR-03 Ingestão de dados IoT via API
- FR-04 Dashboard de histórico (GET /api/iot/data/{plotId})
- FR-05 Motor de alertas (Seca, CalorExtremo, ChuvaIntensa)
- TR-01 Microserviços (Api + Identity + Worker)
- TR-02 Kubernetes — 11 manifests em `k8s/`
- TR-03 Observabilidade — Prometheus + Grafana
- TR-04 Mensageria RabbitMQ (Producer + Consumer)
- TR-05 CI/CD — build → test → Docker GHCR → kubectl apply
- 46 testes unitários passando

### ⚠️ Em Progresso
- DataAnnotations / FluentValidation nos DTOs de input
- Diagrama de arquitetura (D-01)

### ❌ Não Iniciado
- Frontend SPA
- OPT-03: Integração com API climática

---

## 🤝 Convenções

- **Linguagem de código:** C#
- **Linguagem de documentação:** Português
- **Formato:** Markdown
- **Versionamento:** Semântico

---

## 📞 Referências Úteis

- [Documentação .NET 9.0](https://learn.microsoft.com/dotnet)
- [Entity Framework Core](https://learn.microsoft.com/ef)
- [ASP.NET Core Security](https://learn.microsoft.com/aspnet/core/security)

---

## 🚀 Performance Medida (Benchmark)

> Valores **reais** coletados com `.scripts/Invoke-LoadTest.ps1`  
> Ambiente: docker-compose local, PostgreSQL + RabbitMQ em containers, Windows 12-core  
> Configuração: **10 workers concorrentes × 30 segundos** — 3 497 escritas + 3 496 leituras

| Endpoint                              | RPS    | Min ms | P50 ms | P95 ms | P99 ms | Max ms | Sucesso |
|---------------------------------------|--------|--------|--------|--------|--------|--------|---------|
| `POST /api/iot/data` (escrita IoT)    | 106,9  | 3      | 11     | 50     | 82     | 884    | 100%    |
| `GET  /api/iot/data/{plotId}` (leitura)| 106,9 | 4      | 57     | 130    | 197    | 812    | 100%    |

> Resultados JSON por execução salvos automaticamente em `benchmark/benchmark_<timestamp>.json`.  
> Para reproduzir:
> ```powershell
> .scripts\Invoke-LoadTest.ps1 -DurationSeconds 30 -Concurrency 10
> # Exibe tabela e snippet Markdown; salva JSON em benchmark/
> ```

### Limites de recursos (Kubernetes — servidor low-price 2 vCPU / 2 GB)

| Serviço    | CPU req | CPU lim | Mem req | Mem lim | Imagem base       |
|------------|---------|---------|---------|---------|-------------------|
| Api        | 75m     | 200m    | 96 Mi   | 192 Mi  | aspnet:9.0-alpine |
| Identity   | 75m     | 200m    | 96 Mi   | 192 Mi  | aspnet:9.0-alpine |
| Worker     | 50m     | 150m    | 64 Mi   | 128 Mi  | aspnet:9.0-alpine |
| PostgreSQL | 100m    | 300m    | 128 Mi  | 256 Mi  | postgres:16-alpine|
| RabbitMQ   | 100m    | 250m    | 128 Mi  | 240 Mi  | rabbitmq:3.13-mgmt|
| Prometheus | 50m     | 200m    | 64 Mi   | 192 Mi  | prom/prometheus   |
| Grafana    | 50m     | 150m    | 64 Mi   | 128 Mi  | grafana/grafana   |

> Imagens Alpine reduzem o tamanho final de **~220 MB → ~100 MB** por serviço.  
> Budget total: 500m CPU req / 640 Mi RAM req — compatível com 2 vCPU / 2 GB com folga para o SO.

---

**Próximo passo:** Leia [Arquitetura](../01-Arquitetura) para entender a estrutura do projeto.
