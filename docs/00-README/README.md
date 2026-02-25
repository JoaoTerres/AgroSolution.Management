# AgroSolution.Management

**Versão:** 2.0 | **Atualizado:** 24/02/2026 | **Status:** Etapa 2 Concluída

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
| CI | GitHub Actions |

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

### ✅ Completo
- Estrutura base (Layered Architecture)
- Entidades de domínio (Property, Plot)
- Repositórios e contexto EF Core

### ⚠️ Em Progresso
- Configuração de Program.cs
- Autenticação JWT
- Validações em DTOs

### ❌ Não Iniciado
- Testes unitários
- Frontend
- CI/CD Pipeline

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

**Próximo passo:** Leia [Arquitetura](../01-Arquitetura) para entender a estrutura do projeto.
