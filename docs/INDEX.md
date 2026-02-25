# AgroSolution.Management - Índice de Documentação

**Atualizado:** 25/02/2026  
**Versão:** 3.0  
**Status:** Etapa 3 Em Andamento

---

## Estrutura da Documentação

| Pasta | Descrição |
|---|---|
| **00-README** | Visão geral do projeto e quick start |
| **01-Arquitetura** | Diagramas e padrões arquiteturais |
| **02-Especificacoes** | Requisitos funcionais e técnicos |
| **03-GuiasDesenvolvimento** | Convenções e guias de contribuição |
| **04-API** | Endpoints, schemas e exemplos |
| **05-Banco-de-Dados** | Modelos, migrations e scripts |
| **06-Testes** | Estratégia, cobertura e resultados |
| **07-Seguranca** | Autenticação JWT, autorização |
| **08-Deploy** | Docker, CI/CD, ambientes |
| **09-FAQ** | Troubleshooting e perguntas frequentes |

---

## Status das Entregas

| Feature | Status | Branch/Commit |
|---|---|---|
| FR-00 — Property/Plot CRUD | ✅ Concluído | `79701d3` |
| FR-00 — IoT data ingestion | ✅ Concluído | `79701d3` |
| FR-01 — Autenticação JWT (Identity) | ✅ Concluído | `fb65638` |
| FR-04 — Dashboard IoT por talhão | ✅ Concluído | `6d41432` |
| FR-05 — Motor de Alertas (Drought + ExtremeHeat + HeavyRain) | ✅ Concluído | `67ebe80` |
| GET /api/plots/{id} | ✅ Concluído | `1318a16` |
| TR-04 — RabbitMQ + Workers + Docker | ✅ Concluído | `2126e15` |
| CI/CD — GitHub Actions (build + test + coverage) | ✅ Ativo | `.github/workflows/ci.yml` |
| TR-05 — Docker build + push to GHCR no CI | ✅ Concluído | `3fcfe4d` |
| TR-02 — Kubernetes manifests (k8s/) | ✅ Concluído | `3fcfe4d` |
| TR-03 — Prometheus /metrics + Grafana dashboard | ✅ Concluído | `3fcfe4d` |
| D-01 — Architecture diagram | ❌ Não iniciado | — |

---

## Documentos disponíveis

- [README do Projeto](00-README/README.md)
- [API Endpoints](04-API/IOT-API.md)


---

## 📑 Estrutura da Documentação

Este projeto segue princípios de **Biblioteconomia e Organização de Conhecimento** para manter a documentação estruturada, versionada e facilmente navegável.

### 📂 Classificação Decimal (Baseada em Dewey Modificado)

| Pasta | Classificação | Descrição |
|-------|---------------|-----------|
| **00-README** | 000 | Leitura inicial, visão geral e indução |
| **01-Arquitetura** | 100 | Estrutura geral, padrões, diagramas |
| **02-Especificacoes** | 200 | Requisitos, casos de uso, funcionalidades |
| **03-GuiasDesenvolvimento** | 300 | Tutoriais, convenções, guidelines |
| **04-API** | 400 | Endpoints, schemas, exemplos |
| **05-Banco-de-Dados** | 500 | Modelos, migrações, scripts |
| **06-Testes** | 600 | Estratégia, cobertura, casos |
| **07-Seguranca** | 700 | Autenticação, autorização, vulnerabilidades |
| **08-Deploy** | 800 | Ambiente, CI/CD, produção |
| **09-FAQ** | 900 | Perguntas frequentes, troubleshooting |

---

## 🤖 Documentação para Agentes de IA

Pasta especial `.ai-docs` contém contextos e instruções para:

- **01-Prompt-Guards**: Validações e regras para requisições
- **02-Context**: Contexto de negócio e projeto
- **03-Padroes-Codigo**: Padrões obrigatórios
- **04-Fluxos**: Diagramas de fluxos de negócio

---

## 🗺️ Roadmap de Documentação

### Fase 1 - Fundação (v1.0)
- [x] Estrutura de pastas
- [ ] README principal
- [ ] Arquitetura base
- [ ] Especificações iniciais

### Fase 2 - Completude (v1.5)
- [ ] Guias de desenvolvimento
- [ ] Documentação de API
- [ ] Scripts de banco de dados

### Fase 3 - Maturidade (v2.0)
- [ ] Testes e cobertura
- [ ] Segurança e compliance
- [ ] Deploy e CI/CD

---

## 📖 Como Usar Esta Documentação

1. **Comece por:** [00-README/README.md](00-README/README.md)
2. **Entenda a arquitetura:** [01-Arquitetura](01-Arquitetura)
3. **Desenvolva seguindo:** [03-GuiasDesenvolvimento](03-GuiasDesenvolvimento)
4. **Para dúvidas:** [09-FAQ/FAQ.md](09-FAQ/FAQ.md)

---

## 🔄 Versionamento de Documentação

Cada arquivo deve incluir:
```
---
Versão: X.Y
Data: DD/MM/YYYY
Status: [Rascunho | Review | Aprovado]
---
```

---

## 📞 Contribuições

Ao adicionar documentação:
1. Siga a estrutura de pastas
2. Use versionamento nos headers
3. Mantenha links relativos
4. Valide markdown syntax

---

**Última atualização:** 12/02/2026
