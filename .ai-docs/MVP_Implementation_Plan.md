**MVP Implementation Plan — AgroSolutions (AgroSolutions / 8NETT)**

Resumo
- Objetivo: partir do que já está implementado e documentado no repositório para entregar o MVP de uma plataforma IoT de agricultura de precisão conforme os requisitos funcionais e técnicos fornecidos.

Estado Atual (principais artefatos disponíveis)
- Documentação e padrões: `.ai-docs/BusinessRulesMapping.md`, `.ai-docs/03-Padroes-Codigo/PADROES_IOT.md`, `.ai-docs/04-Fluxos/FLUXOS_IOT.md`, `.ai-docs/01-Prompt-Guards/REGRAS.md`.
- Serviços/implementação: controllers e features em `AgroSolution.Api` e `AgroSolution.Core` (endpoints IoT, Property, Plot, validações IoT).
- Testes: existe `AgroSolution.Core.Tests` e adicionamos `AgroSolution.Api.Tests` (smoke test).
- CI inicial: `.github/workflows/ci.yml` criado para `dotnet restore/build/test`.

Escopo do Plano (MVP)
- Funcional mínimo (entregáveis):
  1. Autenticação por e-mail/senha (Produtor Rural).
  2. Cadastro de Propriedade e Talhões com cultura associada.
  3. API pública para ingestão simulada de sensores (umidade, temperatura, precipitação) por talhão.
  4. Dashboard com gráficos históricos e status por talhão (Normal / Alerta de Seca / Risco de Praga).
  5. Motor simples de alertas (regra exemplo: umidade <30% por >24h → Alertas de Seca).

- Não-funcionais obrigatórios:
  - Arquitetura orientada a microsserviços (identidade, propriedades, ingestão, análise/alertas).
  - Orquestração com Kubernetes (minikube/kind aceitável em local de desenvolvimento).
  - Observabilidade: Prometheus + Grafana (Zabbix opcional conforme contexto).
  - Mensageria: RabbitMQ ou Kafka para desacoplar ingestão e processamento.
  - CI/CD automatizado (GitHub Actions já presente; estender para coverage).

Roadmap e Fases (alto nível)
- Fase A — Discovery / Preparação (1 semana)
  - Revisão com stakeholders do `BusinessRulesMapping.md` e dos fluxos IoT.
  - Definição de escopo mínimo por serviço e contratos (API spec / OpenAPI).
  - Seleção de tecnologias (RabbitMQ vs Kafka, InfluxDB vs PostgreSQL/Timescale, Auth provider).

- Fase B — Infra & Scaffold (1-2 semanas)
  - Criar manifests helm/k8s para serviços (Deployment, Service, ConfigMap, Secret) e um cluster local (minikube/kind).
  - Provisionar observability (Prometheus + Grafana) e mensageria (RabbitMQ) no cluster.
  - Definir banco de dados para séries temporais (InfluxDB recomendado) — opcional (bônus).

- Fase C — Core Services & Persistence (2-3 semanas)
  - Serviço de Identidade: login, JWT issuance, middleware de autenticação.
  - Serviço de Propriedades: CRUD de propriedades e talhões.
  - Serviço de Ingestão: endpoint HTTP para ingestão simulada que publica mensagens no broker.
  - Serviço de Análise/Alertas: consumidor do broker, aplica regras e persiste alertas.

- Fase D — Dashboard & UX (1-2 semanas)
  - Frontend básico (ou SPA mínimo) que consome APIs para listar propriedades, talhões, gráficos e alertas.
  - Exposição de métricas e logs para Grafana.

- Fase E — Testes, Integração e Hardening (2 semanas)
  - Implementar testes unitários e de integração.
  - Testes E2E mínimos cobrindo ingestão → processamento → visualização de alerta.
  - Hardening de segurança e configuração de segredos.

- Fase F — Deploy e Evidências (1 semana)
  - Execução em ambiente local ou nuvem, demonstrar Kubernetes, Grafana/Prometheus, pipeline CI/CD.

Estimativas de esforço (sprint-oriented)
- Sprint 0 (setup): 1 semana — infra e decisões arquiteturais.
- Sprint 1: 2 semanas — identidade, propriedades (CRUD), DB básico.
- Sprint 2: 2 semanas — ingestão, mensageria, consumidor de alertas.
- Sprint 3: 2 semanas — dashboard e visualizações.
- Sprint 4: 1-2 semanas — testes, integração, documentação e entrega.

Estratégia de Testes — objetivo: 100% de cobertura no pipeline
- Abordagem:
  1. Unit tests: cobrir regras de domínio e validações (em `AgroSolution.Core`).
  2. Integration tests: endpoints e flows (API ↔ broker ↔ consumer) usando infra mockada com containers (testcontainers) ou cluster local configurado para CI.
  3. E2E tests: um cenário de ingestão simulada que garante geração de alerta (usar testes isolados em pipeline com dependências levantadas via docker-compose ou k8s em runner).

- Ferramentas e configuração CI:
  - Usar `coverlet` para coleta de coverage e gerar relatório `cobertura`/`opencover`.
  - Adicionar ao workflow GitHub Actions passos para:
    * Executar `dotnet test` com `--collect:"XPlat Code Coverage"` ou `--logger:trx` e `Coverlet` args.
    * Gerar relatório HTML/LCOV e publicar artifact.
    * Validar threshold de coverage e falhar pipeline se abaixo de 100% — isso pode ser feito com `coverlet` parâmetro `--threshold` ou com um pequeno script que analisa o XML do coverage e retorna não-zero se <100.
  - Exemplo (snippet a adicionar ao workflow):

```yaml
- name: Test with coverage
  run: |
    dotnet test ./AgroSolution.Core.Tests/AgroSolution.Core.Tests.csproj \
      /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./coverage/ \
      --no-build
    # Fail if coverage < 100% (pseudocode — implementar script para checar cobertura)
    dotnet test ./AgroSolution.Api.Tests/AgroSolution.Api.Tests.csproj \
      /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./coverage/
```

- Observação operacional: 100% de coverage é uma meta muito rígida; para alcançá-la é necessário garantir que todas as linhas (inclusive tratamento de erros e caminhos alternativos) tenham testes. Recomendo um plano incremental: primeiro 80–90% como marco, depois trabalhar casos de borda até 100%. Se realmente quiser 100% já como gating, aceitaremos que alguns testes e mocks adicionais serão necessários (elevado custo).

Alterações de CI propostas
- Atualizar `.github/workflows/ci.yml` com etapas de coverage collection e validação de threshold; publicar relatórios como artefatos.
- Opcional: enviar relatórios para Codecov ou SonarCloud para análise contínua.

Mapeamento rápido entre requisitos e locais no repositório (início)
- Autenticação: implementar `Service: Identity` (ainda não presente — criar novo projeto `AgroSolution.Identity`).
- Cadastro Propriedade/Talhões: `AgroSolution.Api/Controllers/PropertyController.cs` e `AgroSolution.Core/App/Features/CreateProperty`.
- Ingestão sensores: `AgroSolution.Api/Controllers/IoTDataController.cs`, `AgroSolution.Core/App/Features/ReceiveIoTData`.
- Motor de alertas: criar serviço consumidor com lógica baseada em `AgroSolution.Core/App/Validation/IoTDeviceValidator.cs` (reaproveitar validações) e persistir alertas em novo store.

Riscos e Mitigações
- Risco: exigência 100% coverage aumenta esforço. Mitigação: priorizar cobertura em lógica de negócio crítica, usar testes parametrizados e gerar mocks reutilizáveis.
- Risco: infra complexa (Kubernetes + observability) pode consumir tempo. Mitigação: start local com `kind`/`minikube` e manifests simplificados; demonstrar com screenshots/recordings.

Próximos Passos Imediatos (para executar agora)
1. Validar este plano com stakeholders (revisão do `BusinessRulesMapping.md`).
2. Confirmar tecnologias (RabbitMQ/Kafka, InfluxDB opcional).
3. Atualizar workflow CI para coletar coverage e adicionar validação de threshold (posso aplicar a modificação automaticamente se aprovar).
4. Priorizar criação do `Identity Service` e testes unitários em `AgroSolution.Core`.

Arquivo gerado: `.ai-docs/MVP_Implementation_Plan.md`

--
_Se concordar, procedo com: (A) atualizar o workflow CI para coletar e validar coverage (incluir script de threshold), e (B) criar o scaffold do `AgroSolution.Identity` com testes iniciais._
