# OBJECTIVE — AgroSolution.Management Final Goal
<!-- MACHINE-READABLE: optimized for AI agent decision-making. Not human-facing. -->
<!-- LAST_UPDATE: 2026-02-23 | READ_PRIORITY: ALWAYS_FIRST alongside ARCH.md -->
<!-- PURPOSE: anchor every code change to a deliverable requirement; reject or flag work that moves away from these goals -->

---

## §1 PRODUCT_CONTEXT

```
CLIENT: AgroSolutions — agricultural cooperative modernizing via precision agriculture (Ag 4.0)
COURSE: 8NETT — students building MVP for academic + competitive evaluation
PROBLEM: field decisions based on farmer experience only → resource waste, below-potential productivity
SOLUTION: IoT + data analytics platform delivering real-time sensor data and automated alerts to farmers
STAKE: academic competition with prize for best solution; judges evaluate architecture + live demo
```

---

## §2 FUNCTIONAL_REQUIREMENTS

Each item maps directly to a deliverable that MUST be demonstrable.

```
FR-01: AUTHENTICATION
  Actor: Produtor Rural (Producer)
  Requirement: Login with email + password
  Output: JWT token
  Status: NOT_IMPLEMENTED (AgroSolution.Identity project does not exist yet)
  Target project: new AgroSolution.Identity microservice OR identity endpoint in Api
  Acceptance: producer can log in and receive JWT; subsequent API calls use Bearer token

FR-02: PROPERTY & PLOT MANAGEMENT
  Actor: Produtor Rural
  Requirement: Register property (farm) and define plots (talhões) with crop type
  Output: Property + Plot persisted, listed in dashboard
  Status: IMPLEMENTED (CreateProperty, AddPlot, GetProperties use cases + controllers)
  Gap: [Authorize] not active on controllers → needs JWT from FR-01

FR-03: SENSOR DATA INGESTION (simulated)
  Actor: IoT device or simulation script
  Requirement: API endpoint receives soil humidity, temperature, precipitation for a specific plot
  Output: IoTData persisted with Pending status
  Status: IMPLEMENTED (POST /api/iot/data → ReceiveIoTData use case)
  Gap: InMemoryDeviceRepository is dev-only; device→plot mapping needs persistence

FR-04: MONITORING DASHBOARD
  Actor: Produtor Rural
  Requirement:
    a) Historical sensor data charts per plot
    b) Plot status label: "Normal" | "Alerta de Seca" | "Risco de Praga"
  Status: NOT_IMPLEMENTED
  Required new work:
    - GET /api/iot/data/{plotId}?from=&to= (query API using IIoTDataRepository.GetByPlotIdAndDateRangeAsync ← ALREADY EXISTS)
    - Alert status computation (see FR-05)
    - Frontend (SPA or server-rendered) consuming these endpoints
  Decision note: backend query method exists; only controller endpoint + frontend missing

FR-05: ALERT ENGINE
  Actor: background processor
  Requirement: process sensor data and generate alerts
  EXAMPLE_RULE (mandatory): humidity < 30% for >24h consecutive → "Alerta de Seca"
  Output: Alert record per plot; displayed in dashboard
  Status: NOT_IMPLEMENTED
  Required new work:
    - Alert entity (domain)
    - Alert repository + DB migration
    - Consumer worker (Etapa 2 RabbitMQ consumer) OR scheduled background job
    - Rule engine evaluating IoTData time series per plot
    - GET /api/alerts/{plotId} endpoint
  Integration point: feeds into FR-04 dashboard status badge
```

---

## §3 TECHNICAL_REQUIREMENTS

These are mandatory for grading. Each must be demonstrable in the final presentation.

```
TR-01: MICROSERVICES ARCHITECTURE
  Required services (minimum):
    svc-identity   → authentication, JWT issuance           STATUS: missing
    svc-management → properties, plots (current monolith)   STATUS: existing AgroSolution.Api
    svc-ingestion  → IoT data reception                     STATUS: embedded in svc-management (may extract)
    svc-analytics  → alert engine, data processing          STATUS: missing
  Decision guidance:
    - Current codebase is a single API → for MVP, split is logical not physical is acceptable
    - For grading, Kubernetes + separate deployments per service is the strongest evidence
    - Minimum viable split: extract Identity as separate project/container

TR-02: KUBERNETES ORCHESTRATION
  Requirement: app running in minikube/kind (local) OR cloud (AWS/Azure/GCP)
  Required artefacts:
    k8s/ OR helm/ directory with:
      Deployment.yaml per service
      Service.yaml per service
      ConfigMap.yaml (non-secret config)
      Secret.yaml (DB connection strings, JWT secret)
      Ingress.yaml (route external traffic)
  STATUS: NOT_IMPLEMENTED (no k8s/ directory exists)
  Decision note: minikube local is sufficient; no cloud account required

TR-03: OBSERVABILITY
  Requirement: Prometheus + Grafana (primary) OR Zabbix + Grafana
  Required artefacts:
    - /metrics endpoint exposed by API (AspNetCore.Diagnostics.HealthChecks OR prometheus-net)
    - prometheus.yml scrape config
    - Grafana dashboard JSON (at minimum: request count, error rate, IoT ingestion rate)
  STATUS: NOT_IMPLEMENTED
  Decision note: use prometheus-net NuGet for .NET metrics exposure; simplest path

TR-04: MESSAGING (mandatory for ingestion ↔ processing decoupling)
  Requirement: RabbitMQ or Kafka between ingestion and analytics services
  Minimum flow: IoTData.POST → publish to broker → consumer processes → update status + generate alert
  STATUS: NOT_IMPLEMENTED (Etapa 2 in ROADMAP-ETAPA2-3.md)
  Required new work: see ARCH.md §11 ROADMAP_STATE for planned components
  Decision note: RabbitMQ preferred (simpler Docker setup, already designed in ARCH.md §3)

TR-05: CI/CD PIPELINE
  Requirement: automated pipeline with GitHub Actions (or equivalent)
  STATUS: PARTIALLY_IMPLEMENTED
    ✅ Build + test + coverage enforcement (.github/workflows/ci.yml)
    ❌ Docker image build step missing
    ❌ Kubernetes deploy/rollout step missing
  Required additions to ci.yml:
    - docker build + push to registry (Docker Hub or GHCR)
    - kubectl apply OR helm upgrade --install
    - smoke test against deployed endpoint (optional but strong evidence)

TR-06: SOFTWARE ARCHITECTURE BEST PRACTICES
  Current compliance:
    ✅ Clean Architecture (Domain / App / Infra layers)
    ✅ Result<T> pattern (no business exceptions)
    ✅ Repository pattern with interfaces
    ✅ Use case pattern (each feature = 1 class)
    ✅ DI via constructor injection
    ❌ [Authorize] not enforced on controllers (needs FR-01 first)
    ❌ No input validation on DTOs (DataAnnotations or FluentValidation missing)
    ❌ No global exception handler in production config
    ❌ InMemoryDeviceRepository used in non-test code (must be replaced)
```

---

## §4 OPTIONAL_REQUIREMENTS (bonus, no grade impact)

```
OPT-01: NoSQL / Time-series DB
  MongoDB or InfluxDB for IoTData storage (replaces/augments EF Core IoTData table)
  Decision note: InfluxDB ideal for FR-04 charts; requires new repository implementation swapping IIoTDataRepository

OPT-02: SERVERLESS INGESTION
  API Gateway (AWS/Azure/Kong) + Functions (Lambda/Azure Functions) for FR-03 endpoint
  Decision note: high effort; skip unless time allows post-MVP

OPT-03: WEATHER API INTEGRATION
  External weather forecast API shown in FR-04 dashboard
  Decision note: low code effort (HTTP client call), high visual impact for demo
```

---

## §5 DELIVERABLES_CHECKLIST

```
D-01: ARCHITECTURE DIAGRAM
  Content required: service boundaries, messaging flow, k8s topology, DB, observability
  Status: missing (suggest creating docs/01-Arquitetura/solution-diagram.md or .png)

D-02: INFRASTRUCTURE DEMO
  Evidence required:
    - kubectl get pods (all services Running)
    - Grafana dashboard screencap with live metrics
    - Prometheus targets page showing services scraped

D-03: CI/CD DEMO
  Evidence required:
    - GitHub Actions pipeline passing (green) on PR merge
    - Docker image published to registry
    - kubectl rollout triggered by pipeline

D-04: MVP DEMO (live application)
  Scenario walkthrough for grading:
    STEP-1: Login (email + password) → receive JWT
    STEP-2: Create Property via POST /api/properties
    STEP-3: Add Plot via POST /api/plots
    STEP-4: POST /api/iot/data with humidity sensor payload (value: 25, plotId)
    STEP-5: POST /api/iot/data with humidity sensor payload (value: 20, same plotId, simulate 24h+)
    STEP-6: Alert engine triggers → alert record created
    STEP-7: GET /api/alerts/{plotId} returns "Alerta de Seca"
    STEP-8: Dashboard shows chart + alert badge
```

---

## §6 IMPLEMENTATION_PRIORITY

Use this ordering when deciding what to build next in any session.

```
PRIORITY_1 (blocks everything else):
  → FR-01: Authentication (JWT) — blocks [Authorize], dashboard ownership, demo flow
  STATUS: ✅ DONE

PRIORITY_2 (core MVP functionality):
  → TR-04: RabbitMQ messaging (Etapa 2) — required for TR-01 grading and FR-05
  → FR-05: Alert engine consumer + rules
  → Alert entity + repo + migration
  STATUS: ✅ DONE (Drought + ExtremeHeat + HeavyRain rules, 46 tests)

PRIORITY_3 (next — Etapa 3 start):
  → TR-02: Kubernetes manifests ← NEXT ACTION
      Create k8s/ directory with Deployment+Service+ConfigMap+Secret+Ingress per service
      Services: agrosolution-api, agrosolution-identity, agrosolution-worker
      Infrastructure: postgres, rabbitmq (StatefulSet or external)
  → TR-03: Prometheus /metrics + Grafana dashboard
      Add prometheus-net NuGet to AgroSolution.Api
      Expose /metrics endpoint
      Add prometheus.yml + Grafana dashboard JSON to docker-compose
  → TR-05: Docker build + k8s deploy step in ci.yml

PRIORITY_4 (grading polish):
  → D-01: Architecture diagram (Mermaid or draw.io) in docs/01-Arquitetura/
  → Frontend: minimal SPA or Swagger-based demo for D-04
  → Input validation on DTOs (DataAnnotations or FluentValidation)

PRIORITY_5 (bonus):
  → Replace InMemoryDeviceRepository with DB-backed
  → OPT-03: Weather API integration
```

---

## §7 ALERT_ENGINE_SPEC

Detailed spec for FR-05 to guide implementation.

```
RULE: Drought Alert ("Alerta de Seca")
  CONDITION: HumiditySensor readings for a PlotId where value < 30.0f
             AND continuous window ≥ 24 hours
             (no reading ≥ 30.0f breaks the window)
  TRIGGER_POINT: when consumer processes a HumiditySensor IoTData record
  OUTPUT: Alert { PlotId, Type="AlertaDeSeca", TriggeredAt, Message }

RULE: Pest Risk ("Risco de Praga")  ← spec open, design required
  CONDITION: TBD (suggest: temperature + humidity combination threshold)
  OUTPUT: Alert { PlotId, Type="RiscoDePraga", TriggeredAt, Message }

PLOT_STATUS_RESOLUTION (for dashboard badge):
  IF active Alert.Type == "AlertaDeSeca"  → status = "Alerta de Seca"
  IF active Alert.Type == "RiscoDePraga"  → status = "Risco de Praga"
  ELSE                                    → status = "Normal"

ALERT_STATE:
  Active   = triggered, not resolved
  Resolved = subsequent readings return to normal range

IMPLEMENTATION_PATH:
  1. Alert entity: Id, PlotId, Type(enum), TriggeredAt, ResolvedAt?, Message, IsActive
  2. IAlertRepository: AddAsync, GetActiveByPlotIdAsync, ResolveAsync
  3. AlertRepository (EF) + migration
  4. IAlertRule interface: Evaluate(IEnumerable<IoTData> window) → Alert?
  5. DroughtAlertRule implements IAlertRule
  6. AlertEngineService: inject rules + IAlertRepository, called by Consumer Worker
  7. GET /api/alerts/{plotId} endpoint
  8. PlotStatusService: aggregates active alerts → returns status string
```

---

## §8 MICROSERVICE_SPLIT_GUIDE

```
WHEN splitting the current monolith into microservices:

svc-identity (new project):
  Owns: login endpoint, JWT issuance, Producer entity (email, password hash)
  DB: separate schema/db (identity-db)
  Does NOT reference AgroSolution.Core

svc-management (current AgroSolution.Api + Core):
  Owns: Property, Plot, GetProperties, AddPlot, CreateProperty
  Validates JWT from svc-identity (shared JWT secret in config)
  DB: management-db (current ManagementDbContext)

svc-ingestion (may stay in svc-management for MVP, extract later):
  Owns: POST /api/iot/data → publishes to RabbitMQ only (no direct DB write in pure split)
  For MVP: acceptable to keep writing IoTData directly (simpler)

svc-analytics (new project: AgroSolution.Worker or AgroSolution.Analytics):
  Owns: RabbitMQ consumer, AlertEngine, Alert entity + repo
  DB: analytics-db OR same management-db (simpler for MVP)
  Exposes: GET /api/alerts/{plotId}

SHARED_CONTRACT: IoTDataMessage (serialized payload via RabbitMQ)
  Must NOT reference internal domain types; use a plain DTO class mirrored in both services
```

---

## §9 DECISION_RULES_OBJECTIVE

```
OBJ_RULE_01: every code change must advance at least one FR or TR item
  → if a proposed change doesn't map to §2 or §3, question its priority

OBJ_RULE_02: demo scenario (D-04) is the north star
  → when in doubt which feature to build, ask "does this unblock D-04 steps?"

OBJ_RULE_03: FR-01 (auth) is the single most blocking item
  → do not heavily invest in frontend or k8s before JWT auth is working end-to-end

OBJ_RULE_04: alert engine rule must use existing IIoTDataRepository.GetByPlotIdAndDateRangeAsync
  → do NOT re-query raw DB; the method already exists in the repo interface (ARCH.md §5)

OBJ_RULE_05: TR-02 (k8s) only needs minikube; do not over-engineer cloud setup
  → one Deployment + one Service per microservice is sufficient for grading evidence

OBJ_RULE_06: observability minimum viable = one Grafana dashboard with IoT ingestion rate metric
  → implement /metrics endpoint in API first; Grafana dashboard is just JSON config

OBJ_RULE_07: CI/CD minimum viable = pipeline builds Docker image + kubectl apply (even if local registry)
  → current ci.yml already has test/coverage; missing: docker build + deploy steps only

OBJ_RULE_08: optional requirements (OPT-01/02/03) are tie-breakers only
  → complete D-01 through D-04 before touching optional items

OBJ_RULE_09: humidity < 30% threshold is the ONLY alert rule required for passing grade
  → "Risco de Praga" can be a stub (logged, not fully implemented) for MVP
```

---

## §10 STATUS_SNAPSHOT

Current state of the project mapped to deliverables (as of 2026-02-25):

```
FR-01 Authentication          → ✅ COMPLETE (AgroSolution.Identity — POST /api/auth/register + /login, JWT HS256)
FR-02 Property/Plot CRUD      → ✅ COMPLETE (includes GET /api/plots/{id})
FR-03 IoT Ingestion API       → ✅ COMPLETE (InMemoryDevice is acceptable for demo)
FR-04 Dashboard               → ✅ COMPLETE (GET /api/iot/data/{plotId}?from=&to=, 90-day guard, [Authorize])
FR-05 Alert Engine            → ✅ COMPLETE
                                   DroughtRule:     humidity<30% for 24h (min 2 readings)
                                   ExtremeHeatRule: temp>38°C all readings in 6h (min 3 readings)
                                   HeavyRainRule:   cumulative precip≥50mm in 6h
                                   GET /api/alerts/{plotId} with AlertResponseDto

TR-01 Microservices           → ✅ COMPLETE (Identity + Api + Worker as standalone services; Dockerfiles for all 3)
TR-02 Kubernetes              → ✅ COMPLETE (k8s/ directory, 11 manifests, minikube guide)
TR-03 Observability           → ✅ COMPLETE (/metrics via prometheus-net + Grafana IoT dashboard)
TR-04 Messaging (RabbitMQ)    → ✅ COMPLETE (docker-compose + topology + Producer + Consumer + AlertEngine hook)
TR-05 CI/CD                   → ⚠️ PARTIAL (docker build+push to GHCR ✅; kubectl rollout step ❌)
TR-06 Best Practices          → ⚠️ PARTIAL (arch OK; [Authorize] active; DTO annotation validation missing)

D-01 Architecture Diagram     → ❌ NOT STARTED
D-02 Infrastructure Demo      → ⚠️ PARTIAL (docker-compose up -d starts all infra ✅; app containers via override ✅)
D-03 CI/CD Demo               → ⚠️ PARTIAL
D-04 MVP Demo                 → ✅ UNBLOCKED (all 8 demo steps implementable; 46/46 tests passing)

TEST_BASELINE (2026-02-25):
  AgroSolution.Core.Tests: 46 passing (ReceiveIoTData: 19, AlertEngine: 15, GetAlerts: 4, GetIoTDataByRange: 6,
                                        GetByIdPlot: 2)
  AgroSolution.Api.Tests:  smoke only, no threshold
  Coverage: ≥55% scoped to ReceiveIoTData + Validation namespaces
```

### Infrastructure additions (2026-02-24 — Phase 4)

```
docker-compose.yml                → pgAdmin service added (dpage/pgadmin4:latest, port 54320)
docker/pgadmin/servers.json       → pre-registered Management DB + Identity DB connections
.env.example                      → PGADMIN_*, MANAGEMENT_CONNECTION, IDENTITY_CONNECTION, JWT_* vars
AgroSolution.Api/Dockerfile       → multi-stage build, non-root user agro, port 8080
AgroSolution.Identity/Dockerfile  → multi-stage build, non-root user agro, port 8081
docker-compose.override.yml.example → full containerization (all 4 services) with env var injection
.dockerignore                     → excludes bin/obj/tests/docs from build context
```

### Worker implementation (2026-02-24 — Phase 5)

```
AgroSolution.Worker/              → new .NET Worker Service project (Microsoft.NET.Sdk.Worker)
  Workers/IoTDataProducerWorker   → polls GetPendingAsync() → publishes to iot.events exchange
  Workers/IoTDataConsumerWorker   → subscribes to 4 queues → MarkAsProcessed / MarkAsFailed + DLQ
  Messaging/RabbitMQConnectionManager → singleton IConnection, DispatchConsumersAsync=true
  Messaging/IoTEventMessage       → message envelope DTO (IoTDataId, PlotId, DeviceType, RawData)
  Config/DependencyInjectionConfig → wires DbContext, repos, RabbitMQ settings, both workers
  Dockerfile                      → multi-stage build, non-root user agro
docker-compose.override.yml.example → updated to include agrosolution-worker service
```

### Alert Engine + Dashboard (2026-02-24 — Phase 6)

```
AgroSolution.Core/Domain/Entities/Alert.cs              → Alert entity + AlertType enum (Drought, ExtremeHeat, HeavyRain)
AgroSolution.Core/Domain/Interfaces/IAlertRepository.cs → Add/GetByPlotId/GetActiveByPlotIdAndType/Update
AgroSolution.Core/Infra/Repositories/AlertRepository.cs → EF implementation
AgroSolution.Core/Infra/Data/Mappings/AlertMapping.cs   → table: alerts, indexes on plot_id and (plot_id, type, is_active)
AgroSolution.Core/Infra/Data/Migrations/..002_AddAlerts → ALTER TABLE: create alerts table
AgroSolution.Core/App/Features/AlertEngine/
  AlertEngineService.cs   → IAlertEngineService + DroughtRule (all readings < 30% in 24h window, min 2 readings)
AgroSolution.Core/App/Features/GetAlerts/GetAlerts.cs   → IGetAlerts → Result<IEnumerable<AlertResponseDto>>
AgroSolution.Core/App/Features/GetIoTDataByRange/
  GetIoTDataByRange.cs    → IGetIoTDataByRange → validates from<to, max 90 days
AgroSolution.Core/App/DTO/AlertResponseDto.cs           → From(Alert) factory
AgroSolution.Core/App/DTO/IoTDataResponseDto.cs         → From(IoTData) factory
AgroSolution.Api/Controllers/AlertsController.cs        → GET /api/alerts/{plotId:guid} [Authorize]
AgroSolution.Api/Controllers/IoTDataController.cs       → added GET /api/iot/data/{plotId:guid}?from=&to= [Authorize]
AgroSolution.Api/Config/DependencyInjectionConfig.cs    → +IAlertRepository, +IAlertEngineService, +GetAlerts, +GetIoTDataByRange
AgroSolution.Worker/Config/DependencyInjectionConfig.cs → +IAlertRepository + IAlertEngineService
AgroSolution.Worker/Workers/IoTDataConsumerWorker.cs    → calls AlertEngineService.EvaluateAsync after MarkAsProcessed
AgroSolution.Core.Tests/Features/AlertAndDashboardTests.cs → 16 new tests (AlertEngine, GetAlerts, GetIoTDataByRange)
```
