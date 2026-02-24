# ARCH — AgroSolution.Management Architecture Reference
<!-- MACHINE-READABLE: optimized for AI agent fast-lookup and decision-making. Not human-facing. -->
<!-- LAST_UPDATE: 2026-02-23 | STATUS: current | READ_PRIORITY: ALWAYS_FIRST -->

---

## §1 SOLUTION_MAP

```
SOLUTION: AgroSolution.Management.sln
ROOT: c:\Users\Guilherme\source\repos\AgroBusinessSolution\AgroSolution.Management\

PROJECTS:
  [API]          AgroSolution.Api              → AgroSolution.Api\          ← port 5000/7000
  [IDENTITY]     AgroSolution.Identity         → AgroSolution.Identity\     ← port 5001/7001 | FR-01 ✅
  [CORE]         AgroSolution.Core             → AgroSolution.Core\
  [TEST_UNIT]    AgroSolution.Core.Tests       → AgroSolution.Core.Tests\
  [TEST_SMOKE]   AgroSolution.Api.Tests        → AgroSolution.Api.Tests\
  [TEST_INT]     AgroSolution.IntegrationTests → AgroSolution.IntegrationTests\  ← EMPTY/SKIPPED in CI

COUPLING RULES (enforced, never violate):
  Api      → Core         ✅ allowed (via interfaces only)
  Identity → Core         ❌ FORBIDDEN (Identity is standalone, own domain)
  Core     → Api          ❌ FORBIDDEN
  Core     → Infra        ✅ allowed (Infra lives inside Core project)
  Tests    → Core         ✅ allowed
  Tests    → Api          ✅ allowed (smoke only, no coverage threshold)

LAYER INTERNAL STRUCTURE (Core project):
  Domain/              ← entities, interfaces, AssertValidation, DomainException
  App/Common/          ← Result<T>
  App/DTO/             ← input/output DTOs
  App/Features/        ← use cases (interface + implementation in same folder)
  App/Validation/      ← IoT validators
  Infra/Data/          ← ManagementDbContext + EF Mappings
  Infra/Repositories/  ← concrete implementations
  Infra/Messaging/     ← RabbitMQSettings (typed config, no implementation yet)

LAYER INTERNAL STRUCTURE (Identity project — mirrors Core conventions):
  Domain/              ← Producer entity, AssertValidation, DomainException
  App/Common/          ← Result<T>
  App/DTO/             ← LoginDto, RegisterProducerDto, TokenResponseDto
  App/Features/        ← Login, RegisterProducer (same pattern as Core)
  Infra/Data/          ← IdentityDbContext + ProducerMapping
  Infra/Repositories/  ← ProducerRepository
  Infra/Services/      ← IPasswordHasher (PBKDF2-SHA256), IJwtTokenService (HS256)

FRAMEWORK: .NET 9 | ORM: EF Core (code-first) | DB: PostgreSQL | Auth: JWT Bearer (shared secret between Identity + Api)
JWT_SHARED_SECRET: appsettings.Development.json → Jwt:SecretKey (same value in both projects)
```

---

## §2 ENTITY_REGISTRY

### Property
```
FILE: AgroSolution.Core\Domain\Entities\Property.cs
DBSET: ManagementDbContext.Properties
CONSTRUCTOR: Property(string name, string location, Guid producerId)
PROPERTIES:
  Id           Guid          private set
  Name         string        private set
  Location     string        private set  ← NOTE: schema differs from CONTEXTO.md which says City+State; actual impl uses single Location string
  ProducerId   Guid          private set
  Plots        IReadOnlyCollection<Plot>  via private List<Plot> _plots

INVARIANTS (throw DomainException):
  Name     → NotEmpty
  Location → NotEmpty
  ProducerId → NotGuidEmpty

METHODS:
  AddPlot(string name, string cropType, decimal area) → creates Plot internally
  Validate() → called in constructor

BUSINESS RULES:
  - Name must be unique per ProducerId (enforced in CreateProperty use case, NOT entity)
  - ProducerId maps to JWT sub claim (AppUserId) — no Producer entity in this project (future Identity microservice)
```

### Plot
```
FILE: AgroSolution.Core\Domain\Entities\Plot.cs
DBSET: ManagementDbContext.Plots
CONSTRUCTOR: Plot(Guid propertyId, string name, string cropType, decimal area)
PROPERTIES:
  Id              Guid     private set
  PropertyId      Guid     private set
  Name            string   private set
  CropType        string   private set
  AreaInHectares  decimal  private set

INVARIANTS (throw DomainException):
  PropertyId     → NotGuidEmpty
  Name           → NotEmpty
  CropType       → NotEmpty
  AreaInHectares → >0

BUSINESS RULES:
  - Name must be unique per PropertyId (enforced in AddPlot use case)
  - Plot is created ONLY via Property.AddPlot() — never instantiate directly from outside use cases
```

### IoTData
```
FILE: AgroSolution.Core\Domain\Entities\IoTData.cs
DBSET: ManagementDbContext.IoTData
CONSTRUCTOR: IoTData(Guid plotId, IoTDeviceType deviceType, string rawData, DateTime deviceTimestamp)
PROPERTIES:
  Id                    Guid
  PlotId                Guid
  DeviceType            IoTDeviceType (enum)
  RawData               string  ← raw JSON from device
  DeviceTimestamp       DateTime
  ReceivedAt            DateTime  ← set in constructor, UTC
  ProcessingStatus      IoTProcessingStatus (enum)
  ProcessingQueueId     string?
  ProcessingStartedAt   DateTime?
  ProcessingCompletedAt DateTime?
  ErrorMessage          string?

STATUS_TRANSITIONS:
  Pending(1) → Queued(2)    via MarkAsQueued(queueId)
  Queued(2)  → Processed(3) via MarkAsProcessed()
  *          → Failed(4)    via MarkAsFailed(errorMessage)
  *          → Discarded(5)  ← no method yet, manual set

INVARIANTS (constructor validation):
  plotId     → NotEmpty
  rawData    → NotNull
  deviceType → IsValidEnum
```

---

## §3 ENUM_REGISTRY

### IoTDeviceType
```
FILE: AgroSolution.Core\App\DTO\ReceiveIoTDataDto.cs
  TemperatureSensor   = 1  → validator: TemperatureSensorValidator
  HumiditySensor      = 2  → validator: HumiditySensorValidator
  PrecipitationSensor = 3  → validator: PrecipitationSensorValidator
  WeatherStationNode  = 4  → validator: WeatherStationValidator
  [NEXT_VALUE]        = 5  ← use this for new device types

ROUTING_KEY_MAP (planned RabbitMQ Etapa 2):
  TemperatureSensor   → iot.temperature
  HumiditySensor      → iot.humidity
  PrecipitationSensor → iot.precipitation
  WeatherStationNode  → iot.weather (compound, fan-out to 4 queues)
  exchange: iot.events (topic)
```

### IoTProcessingStatus
```
FILE: AgroSolution.Core\Domain\Entities\IoTData.cs (bottom)
  Pending=1, Queued=2, Processed=3, Failed=4, Discarded=5
```

---

## §4 FEATURE_REGISTRY

### CreateProperty
```
FILES:
  interface: AgroSolution.Core\App\Features\CreateProperty\ICreateProperty.cs
  impl:      AgroSolution.Core\App\Features\CreateProperty\CreateProperty.cs
SIGNATURE: ExecuteAsync(CreatePropertyDto dto, Guid producerId) → Result<Guid>
DEPENDENCIES: IPropertyRepository
INPUT_DTO: CreatePropertyDto  (AgroSolution.Core\App\DTO\CreatePropertyDto.cs)
OUTPUT: Result<Guid> (property.Id on success)
BUSINESS_RULE: Name unique per producerId (case-insensitive)
CONTROLLER: PropertyController → POST /api/properties
DI_KEY: ICreateProperty → CreateProperty (Scoped)
```

### GetProperties
```
FILES:
  interface: AgroSolution.Core\App\Features\GetProperties\IGetProperties.cs  (inferred)
  impl:      AgroSolution.Core\App\Features\GetProperties\GetProperties.cs
DEPENDENCIES: IPropertyRepository
OUTPUT: Result<IEnumerable<PropertyResponseDto>>
CONTROLLER: PropertyController → GET /api/properties
DI_KEY: IGetProperties → GetProperties (Scoped)
```

### AddPlot
```
FILES:
  interface: AgroSolution.Core\App\Features\AddPlot\IAddPlot.cs
  impl:      AgroSolution.Core\App\Features\AddPlot\AddPlot.cs
SIGNATURE: ExecuteAsync(CreatePlotDto dto) → Result<Guid>
DEPENDENCIES: IPropertyRepository (load property → AddPlot → Update → SaveChanges)
INPUT_DTO: CreatePlotDto  (AgroSolution.Core\App\DTO\CreatePlotDto.cs)
OUTPUT: Result<Guid> (plot.Id on success)
BUSINESS_RULES:
  - Property must exist (else "A propriedade informada não existe.")
  - Plot name unique per property (case-insensitive)
CONTROLLER: PlotController → POST /api/plots
DI_KEY: IAddPlot → AddPlot (Scoped)
```

### ReceiveIoTData
```
FILES:
  interface+impl: AgroSolution.Core\App\Features\ReceiveIoTData\ReceiveIoTData.cs
SIGNATURE: ExecuteAsync(ReceiveIoTDataDto dto) → Result<IoTDataReceivedDto>
DEPENDENCIES: IIoTDataRepository, IoTDeviceValidatorFactory, IDeviceRepository
INPUT_DTO: ReceiveIoTDataDto
  - PlotId?       Guid?         optional, resolved via DeviceId if absent
  - DeviceId?     string?       optional, read from dto OR from rawData JSON (device_id/deviceId)
  - DeviceType    IoTDeviceType required
  - RawData       string        required (full JSON body)
  - DeviceTimestamp? DateTime?

RESOLUTION_FLOW:
  1. dto == null              → Fail
  2. PlotId provided          → use it
  3. PlotId absent            → extract deviceId from dto.DeviceId OR rawData JSON (keys: device_id | deviceId)
  4. deviceId absent          → Fail "deviceId obrigatório"
  5. IDeviceRepository lookup → Fail if not found
  6. IIoTDeviceValidator validate rawData → Fail if invalid
  7. new IoTData(plotId, deviceType, rawData, timestamp) → AddAsync → return IoTDataReceivedDto

CONTROLLER: IoTDataController → POST /api/iot/data (raw body, X-Device-Type header OR auto-infer)
DI_KEY: IReceiveIoTData → ReceiveIoTData (Scoped)
TESTED: ✅ AgroSolution.Core.Tests\Features\ReceiveIoTDataTests.cs (19 tests, coverage ~59% scoped)
```

---

## §5 REPOSITORY_REGISTRY

### IPropertyRepository
```
FILE: AgroSolution.Core\Domain\Interfaces\IPropertyRepository.cs
IMPL: AgroSolution.Core\Infra\Repositories\PropertyRepository.cs
METHODS:
  AddAsync(Property)
  GetByIdAsync(Guid) → Property?
  GetByProducerIdAsync(Guid) → IEnumerable<Property>
  Update(Property)             ← SYNC (no await)
  SaveChangesAsync()
DI: Scoped
```

### IIoTDataRepository
```
FILE: AgroSolution.Core\Domain\Interfaces\IIoTDataRepository.cs
IMPL: AgroSolution.Core\Infra\Repositories\IoTDataRepository.cs
METHODS:
  AddAsync(IoTData) → bool        ← SaveChanges inside
  GetByIdAsync(Guid) → IoTData?
  GetByPlotIdAsync(Guid) → IEnumerable<IoTData>
  GetPendingAsync(int limit=100) → IEnumerable<IoTData>  ← USED BY future Producer Worker
  UpdateAsync(IoTData) → bool     ← SaveChanges inside
  GetByPlotIdAndDateRangeAsync(Guid, DateTime, DateTime) → IEnumerable<IoTData>
DI: Scoped
NOTE: AddAsync and UpdateAsync already call SaveChanges internally — do NOT call SaveChanges externally
```

### IDeviceRepository
```
FILE: AgroSolution.Core\Domain\Interfaces\IDeviceRepository.cs
IMPL: AgroSolution.Core\Infra\Repositories\InMemoryDeviceRepository.cs  ← IN-MEMORY, dev/test only
METHODS:
  GetPlotIdByDeviceAsync(string deviceId) → Guid?
DI: Scoped
FUTURE: replace with DB-backed repo for production
```

---

## §6 VALIDATION_SUBSYSTEM

### IoTDeviceValidatorFactory
```
FILE: AgroSolution.Core\App\Validation\IoTDeviceValidator.cs
PATTERN: dictionary lookup by IoTDeviceType, throws ArgumentException if type not registered
DI: Singleton
ADD_VALIDATOR: 1) implement IIoTDeviceValidator, 2) add to _validators dict in factory constructor
```

### Validator Rules
```
TemperatureSensorValidator (type=1):
  required JSON fields: value (number)
  optional: unit (string, default "C"), deviceId (string)
  range: value ∈ [-60, 60] °C

HumiditySensorValidator (type=2):
  required: value (number)
  optional: unit (default "%"), deviceId
  range: value ∈ [0, 100]

PrecipitationSensorValidator (type=3):
  required: value (number)
  optional: unit (default "mm"), deviceId
  range: value ∈ [0, 500]

WeatherStationValidator (type=4):
  required: device_id OR deviceId (string), telemetry (object)
  optional telemetry fields (all validated if present):
    temperature_air   ∈ [-60, 60]
    humidity_air      ∈ [0, 100]
    pressure          ∈ [300, 1100]
    precipitation_mm  ∈ [0, 500]
    wind_speed_kmh    ∈ [0, 400]
    soil_moisture_1   ∈ [0, 100]
```

---

## §7 API_SURFACE

### BaseController
```
FILE: AgroSolution.Api\Controllers\BaseController.cs
AppUserId: Guid from JWT claim NameIdentifier (Guid.Empty if not authenticated)
CustomResponse<T>(Result<T>):
  Success  → 200 { success:true, data: T }
  "Propriedade não encontrada." | "A propriedade informada não existe." → 404
  "Identificação do produtor inválida." → 401
  default  → 400 { success:false, errors:[message] }
NOTE: to add new HTTP error mappings → edit switch in BaseController.CustomResponse
```

### Endpoints
```
POST /api/iot/data           IoTDataController.ReceiveData()   → IReceiveIoTData
  body: raw JSON (device payload)
  header: X-Device-Type (int, optional)
  type inference: "telemetry" in body → WeatherStationNode, else TemperatureSensor fallback

GET  /api/properties         PropertyController               → IGetProperties
POST /api/properties         PropertyController               → ICreateProperty
POST /api/plots              PlotController                   → IAddPlot
```

### DependencyInjectionConfig
```
FILE: AgroSolution.Api\Config\DependencyInjectionConfig.cs
Add ALL new use cases, repos and services here. Pattern:
  services.AddScoped<IInterface, Implementation>();
  services.AddSingleton<SingletonService>();
```

---

## §8 DATABASE

```
CONTEXT: ManagementDbContext (AgroSolution.Core\Infra\Data\ManagementDbContext.cs)
DBSETS: Properties, Plots, IoTData
MAPPINGS: AgroSolution.Core\Infra\Data\Mappings\ (fluent EF config, applied via ApplyConfigurationsFromAssembly)
MIGRATIONS: prefix sequentially (001_, 002_, ...)
CONNECTION: appsettings.Development.json (not committed; absent in appsettings.json)
```

---

## §9 CI_PIPELINE

```
FILE: .github\workflows\ci.yml
TRIGGER: push to main | feat/** | PR to main

STEPS:
  1. checkout
  2. setup dotnet 9
  3. dotnet restore (solution)
  4. dotnet build Release --no-restore
  5. Test - Core (coverage + threshold)
       project: AgroSolution.Core.Tests
       /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
       /p:Include=[AgroSolution.Core]AgroSolution.Core.App.Features.ReceiveIoTData.*,
                  [AgroSolution.Core]AgroSolution.Core.App.Validation.*
       output: coverage/core.cobertura.xml
  6. Test - Api (smoke, NO threshold)
       project: AgroSolution.Api.Tests (smoke only)
  7. Debug coverage files (ls + cat first 120 lines)
  8. Validate coverage threshold
       script: .github/scripts/check_coverage.sh
       COVERAGE_THRESHOLD: 55  ← baseline 59.11% actual; raise as tests expand
  9. Upload artifact: coverage-reports → coverage/

COVERAGE_SCOPE: only ReceiveIoTData + Validation namespaces
COVERAGE_BASELINE: line=59.11%, branch=50%, method=52.94%
COVERLET_PACKAGE: coverlet.msbuild required (added to Core.Tests.csproj)

IntegrationTests: SKIPPED (Features/ empty — re-enable in Etapa 2/3 when workers exist)

ADD_NEW_TEST_PROJECT:
  1. Add PackageReference coverlet.msbuild to .csproj
  2. Add new dotnet test step in ci.yml with /p:CoverletOutput=coverage/NAME.cobertura.xml
  3. check_coverage.sh auto-iterates all *.cobertura.xml — threshold applies to all files
```

---

## §10 CHANGE_IMPACT_MATRIX

### Add a new Entity
```
REQUIRED:
  1. AgroSolution.Core\Domain\Entities\{Entity}.cs               ← entity with invariants
  2. AgroSolution.Core\Domain\Interfaces\I{Entity}Repository.cs  ← interface
  3. AgroSolution.Core\Infra\Repositories\{Entity}Repository.cs  ← EF impl
  4. AgroSolution.Core\Infra\Data\ManagementDbContext.cs          ← add DbSet
  5. AgroSolution.Core\Infra\Data\Mappings\{Entity}Mapping.cs    ← EF fluent config
  6. EF migration (dotnet ef migrations add)
  7. AgroSolution.Api\Config\DependencyInjectionConfig.cs         ← register repo
```

### Add a new Use Case / Feature
```
REQUIRED:
  1. AgroSolution.Core\App\DTO\{Name}Dto.cs                       ← input DTO
  2. AgroSolution.Core\App\Features\{Name}\I{Name}.cs             ← interface
  3. AgroSolution.Core\App\Features\{Name}\{Name}.cs              ← implementation
  4. AgroSolution.Api\Controllers\{Resource}Controller.cs         ← endpoint
  5. AgroSolution.Api\Config\DependencyInjectionConfig.cs         ← register use case
  6. AgroSolution.Core.Tests\Features\{Name}Tests.cs             ← unit tests
```

### Add a new IoT Device Type
```
REQUIRED:
  1. AgroSolution.Core\App\DTO\ReceiveIoTDataDto.cs               ← add enum value (next int)
  2. AgroSolution.Core\App\Validation\IoTDeviceValidator.cs       ← implement IIoTDeviceValidator + add to factory dict
  3. AgroSolution.Core.Tests\Features\ReceiveIoTDataTests.cs      ← add test cases for new type
  OPTIONAL (Etapa 2):
  4. ROADMAP_STATE §11 → add routing key mapping for new type
  5. ci.yml coverage include filter may need updated namespace if validator in new file
```

### Add a new endpoint to existing controller
```
REQUIRED:
  1. AgroSolution.Core\App\DTO\ (new DTOs if needed)
  2. Feature files (use case)
  3. Controller method
  4. DependencyInjectionConfig registration
  IF new error message mapping needed → BaseController.CustomResponse switch
```

### Change Repository contract (IPropertyRepository, IIoTDataRepository, IDeviceRepository)
```
IMPACT_CHAIN:
  Interface file → Impl file → ALL use cases that inject it → ALL test mocks (Moq.Setup)
  Files to update for IIoTDataRepository change:
    AgroSolution.Core\Domain\Interfaces\IIoTDataRepository.cs
    AgroSolution.Core\Infra\Repositories\IoTDataRepository.cs
    AgroSolution.Core\App\Features\ReceiveIoTData\ReceiveIoTData.cs (if method used)
    AgroSolution.Core.Tests\Features\ReceiveIoTDataTests.cs (_iotDataRepositoryMock.Setup)
```

### Modify BaseController.CustomResponse error mapping
```
FILE: AgroSolution.Api\Controllers\BaseController.cs
  - Add case to switch expression
  - Match exact string returned by use case Result<T>.Fail(message)
```

---

## §11 ROADMAP_STATE

```
ETAPA: 1 → COMPLETE (API receives IoT data, validates, persists)
ETAPA: 2 → IN_PROGRESS (RabbitMQ docker isolated ✅ | Producer+Consumer Workers pending)
ETAPA: 3 → PENDING  (Analytics, Kubernetes, Prometheus/Grafana)

COMPLETED_SINCE_ETAPA_1:
  AgroSolution.Identity ✅ (FR-01) — POST /api/auth/register + POST /api/auth/login
  docker-compose.yml    ✅ — RabbitMQ + PostgreSQL isolated, topology pre-loaded

ETAPA_2_COMPONENTS (not yet created):
  Worker/IoTDataProducerWorker.cs
    deps: IIoTDataRepository (GetPendingAsync → MarkAsQueued → UpdateAsync)
          IRabbitMQPublisher (new interface to create)
          IPropertyRepository (validate plot existence)
    trigger: IHostedService loop, interval = RabbitMQSettings.ProducerPollingIntervalMs

  Worker/IoTDataConsumerWorker.cs
    deps: IRabbitMQConsumer (new interface)
          IIoTDataProcessor (per-device-type processor, new interface)
          IIoTDataRepository (UpdateAsync → MarkAsProcessed/Failed)

  RabbitMQ exchanges/queues → see §14 LOCAL_INFRA for topology details
  exchange: iot.events | type: topic
  config class: AgroSolution.Core\Infra\Messaging\RabbitMQSettings.cs ← ALL routing keys/queues here

  New project: AgroSolution.Worker (BackgroundService host)
    OR: embed workers into AgroSolution.Api as IHostedService

ETAPA_3_COMPONENTS:
  Kubernetes manifests (k8s/ or helm/)
  Prometheus metrics endpoint (AspNetCore.Diagnostics.HealthChecks or custom middleware)
  Grafana dashboards
  InfluxDB or TimescaleDB for time-series (replaces/augments ManagementDbContext for IoTData)
```

---

## §12 DECISION_RULES

```
RULE_01: Return type from use case
  → ALWAYS Result<T>; NEVER throw from business logic; exceptions only from DomainException in entity constructors

RULE_02: Where to put business logic
  → Entity invariants (AssertValidation) + use case (duplicate/ownership checks)
  → NEVER in controller or repository

RULE_03: Controller error response
  → Call CustomResponse(result); if new HTTP status needed, add case to BaseController switch

RULE_04: New IoT sensor type
  → Add enum value → add validator class → register in factory; do NOT touch ReceiveIoTData use case

RULE_05: PlotId resolution for IoT data
  → Priority: dto.PlotId > dto.DeviceId > rawData JSON (device_id|deviceId) > IDeviceRepository lookup
  → Fail if none resolves

RULE_06: Saving data
  → IoTDataRepository.AddAsync and UpdateAsync call SaveChanges internally → DO NOT call external SaveChanges
  → PropertyRepository.Update is sync; must call SaveChangesAsync() AFTER (see AddPlot pattern)

RULE_07: Authentication
  → AppUserId = Guid.Parse(JWT sub claim); available in all controllers via BaseController.AppUserId
  → Guid.Empty if unauthenticated; add [Authorize] to protect; validate ownership in use case

RULE_08: Adding use case
  → Interface and implementation in SAME folder under App/Features/{FeatureName}/
  → Use primary constructor syntax: public class Foo(IDep dep) : IFoo

RULE_09: CI coverage threshold
  → Scope filter targets ReceiveIoTData + Validation namespaces only
  → Threshold: 55% (baseline 59.11%); raise threshold incrementally as tests are added
  → Adding a new validator class WITHOUT tests will drop coverage → add tests simultaneously

RULE_10: InMemoryDeviceRepository
  → DEV/TEST only; for production Etapa 2 replace with DB-backed implementation of IDeviceRepository
  → Do not add real device data to InMemory repo; use only for test fixtures

RULE_11: Modifying IoTData status
  → Use domain methods ONLY: MarkAsQueued(id) | MarkAsProcessed() | MarkAsFailed(msg)
  → Direct property assignment to ProcessingStatus violates encapsulation

RULE_12: EF Mappings
  → Fluent config in Infra\Data\Mappings\ auto-discovered via ApplyConfigurationsFromAssembly
  → Never use DataAnnotations on domain entities
```

---

## §13 KEY_FILE_INDEX
<!-- Quick path lookup — use when you know what to edit but not where -->

```
ENTITY Property           → AgroSolution.Core\Domain\Entities\Property.cs
ENTITY Plot               → AgroSolution.Core\Domain\Entities\Plot.cs
ENTITY IoTData            → AgroSolution.Core\Domain\Entities\IoTData.cs
ENUM IoTDeviceType        → AgroSolution.Core\App\DTO\ReceiveIoTDataDto.cs
ENUM IoTProcessingStatus  → AgroSolution.Core\Domain\Entities\IoTData.cs
RESULT<T>                 → AgroSolution.Core\App\Common\Result.cs
ASSERT_VALIDATION         → AgroSolution.Core\Domain\AssertValidation.cs
DOMAIN_EXCEPTION          → AgroSolution.Core\Domain\DomainException.cs
DB_CONTEXT                → AgroSolution.Core\Infra\Data\ManagementDbContext.cs
DI_CONFIG                 → AgroSolution.Api\Config\DependencyInjectionConfig.cs
BASE_CONTROLLER           → AgroSolution.Api\Controllers\BaseController.cs
IOT_VALIDATORS            → AgroSolution.Core\App\Validation\IoTDeviceValidator.cs
IOT_CONTROLLER            → AgroSolution.Api\Controllers\IoTDataController.cs
PROPERTY_CONTROLLER       → AgroSolution.Api\Controllers\PropertyController.cs
PLOT_CONTROLLER           → AgroSolution.Api\Controllers\PlotController.cs
CI_WORKFLOW               → .github\workflows\ci.yml
COVERAGE_SCRIPT           → .github\scripts\check_coverage.sh
CORE_TESTS_PROJECT        → AgroSolution.Core.Tests\AgroSolution.Core.Tests.csproj
IOT_TESTS                 → AgroSolution.Core.Tests\Features\ReceiveIoTDataTests.cs
INTEGRATION_TESTS_DIR     → AgroSolution.IntegrationTests\Features\   ← EMPTY
BUSINESS_RULES_REF        → .ai-docs\BusinessRulesMapping.md
CONTEXT_REF               → .ai-docs\02-Context\CONTEXTO.md
PROMPT_GUARDS_REF         → .ai-docs\01-Prompt-Guards\REGRAS.md
IOT_PATTERNS_REF          → .ai-docs\03-Padroes-Codigo\PADROES_IOT.md
IOT_FLOWS_REF             → .ai-docs\04-Fluxos\FLUXOS_IOT.md
MVP_PLAN_REF              → .ai-docs\MVP_Implementation_Plan.md
ROADMAP_REF               → ROADMAP-ETAPA2-3.md

# Identity project
IDENTITY_PROGRAM          → AgroSolution.Identity\Program.cs
IDENTITY_DB_CONTEXT       → AgroSolution.Identity\Infra\Data\IdentityDbContext.cs
IDENTITY_DI               → AgroSolution.Identity\Config\DependencyInjectionConfig.cs
IDENTITY_AUTH_CTRL        → AgroSolution.Identity\Controllers\AuthController.cs
PRODUCER_ENTITY           → AgroSolution.Identity\Domain\Entities\Producer.cs
PASSWORD_HASHER           → AgroSolution.Identity\Infra\Services\PasswordHasher.cs
JWT_SERVICE               → AgroSolution.Identity\Infra\Services\JwtTokenService.cs
PRODUCER_REPO             → AgroSolution.Identity\Infra\Repositories\ProducerRepository.cs

# RabbitMQ / Messaging
RABBITMQ_SETTINGS         → AgroSolution.Core\Infra\Messaging\RabbitMQSettings.cs
RABBITMQ_DEFINITIONS      → docker\rabbitmq\definitions.json   ← exchanges/queues/bindings
RABBITMQ_CONFIG           → docker\rabbitmq\rabbitmq.conf

# Local infra
DOCKER_COMPOSE                → docker-compose.yml
DOCKER_COMPOSE_OVERRIDE_EX    → docker-compose.override.yml.example  ← copy to .override.yml (gitignored)
DOCKER_ENV_TEMPLATE           → .env.example                         ← copy to .env (gitignored)
DOCKER_POSTGRES_INIT          → docker\postgres\init.sql
DOCKER_PGADMIN_SERVERS        → docker\pgadmin\servers.json           ← pre-registered DB connections
DOCKERFILE_API                → AgroSolution.Api\Dockerfile
DOCKERFILE_IDENTITY           → AgroSolution.Identity\Dockerfile
DOCKERIGNORE                  → .dockerignore
```

---

## §14 LOCAL_INFRA

```
# ── Setup (one-time) ──────────────────────────────────────────────────────────
cp .env.example .env                # never committed; customise credentials/ports

# ── Run infra only (recommended during development) ───────────────────────────
docker compose up -d                # starts postgres + pgadmin + rabbitmq
dotnet run --project AgroSolution.Api\AgroSolution.Api.csproj
dotnet run --project AgroSolution.Identity\AgroSolution.Identity.csproj

# ── Run fully containerised (demo/staging/CI) ─────────────────────────────────
cp docker-compose.override.yml.example docker-compose.override.yml
docker compose build
docker compose up -d
# Api      → http://localhost:8080/swagger
# Identity → http://localhost:8081/swagger

# ── Day-to-day commands ───────────────────────────────────────────────────────
docker compose logs -f postgres      → tail PostgreSQL logs
docker compose logs -f rabbitmq      → tail RabbitMQ logs
docker compose restart rabbitmq      → apply definitions.json topology changes
docker compose down                  → stop (volumes preserved)
docker compose down -v               → stop + wipe all data (fresh start)

# ── Services ──────────────────────────────────────────────────────────────────
postgres    postgres:16-alpine
  port:    ${POSTGRES_PORT:-5432}
  volume:  postgres_data (persistent)
  init:    docker/postgres/init.sql → creates both DBs on first start (idempotent)
  health:  pg_isready

pgadmin     dpage/pgadmin4:latest
  port:    ${PGADMIN_PORT:-54320} → http://localhost:54320
  login:   PGADMIN_EMAIL / PGADMIN_PASSWORD (from .env)
  servers: docker/pgadmin/servers.json → ManagementDB + IdentityDB pre-registered
           password prompt on first connect: use POSTGRES_PASSWORD from .env
  volume:  pgadmin_data (persistent: saved queries, history)
  depends: postgres (healthy)

rabbitmq    rabbitmq:3.13-management-alpine
  ports:   ${RABBITMQ_PORT_AMQP:-5672} (AMQP) + ${RABBITMQ_PORT_MGMT:-15672} (UI)
  ui:      http://localhost:15672 (user/pass from .env)
  config:  docker/rabbitmq/rabbitmq.conf
  topology: docker/rabbitmq/definitions.json (auto-loaded on first start)
  health:  rabbitmq-diagnostics ping

# ── Connection string strategy ────────────────────────────────────────────────
#   LOCAL  (dotnet run): Host=localhost  ← appsettings.Development.json
#   DOCKER (container):  Host=postgres   ← env var injected by docker-compose.override.yml
#
# ASP.NET Core double-underscore env var override:
#   ConnectionStrings__ManagementConnection   (AgroSolution.Api)
#   ConnectionStrings__IdentityConnection     (AgroSolution.Identity)
# Pre-defined in docker-compose.override.yml.example using values from .env.

# ── Dockerfiles ───────────────────────────────────────────────────────────────
# AgroSolution.Api/Dockerfile
#   multi-stage: restore → publish → aspnet:9.0 runtime
#   port: 8080 | user: agro (non-root) | context: solution root
#   build: docker build -f AgroSolution.Api/Dockerfile -t agrosolution-api .
#
# AgroSolution.Identity/Dockerfile
#   multi-stage: restore → publish → aspnet:9.0 runtime
#   port: 8081 | user: agro (non-root) | context: solution root
#   build: docker build -f AgroSolution.Identity/Dockerfile -t agrosolution-identity .

# ── RabbitMQ topology (docker/rabbitmq/definitions.json) ──────────────────────
#   Exchange : iot.events      | topic  | durable
#   Exchange : iot.dead-letter | fanout | durable
#   Queue    : queue.temperature   ttl=24h → iot.temperature
#   Queue    : queue.humidity      ttl=24h → iot.humidity
#   Queue    : queue.precipitation ttl=24h → iot.precipitation
#   Queue    : queue.weather       ttl=24h → iot.weather
#   Queue    : queue.dead-letter   no-ttl  → dead-letter exchange

ADD_NEW_QUEUE:
  1. docker/rabbitmq/definitions.json → add entry in queues + bindings
  2. AgroSolution.Core\Infra\Messaging\RabbitMQSettings.cs → add property
  3. appsettings.json "RabbitMQ" section
  4. docker compose restart rabbitmq

RABBITMQ_SETTINGS_DI (Etapa 2 — when Worker is created):
  services.Configure<RabbitMQSettings>(config.GetSection(RabbitMQSettings.SectionName));
  inject: IOptions<RabbitMQSettings>
  env override: RABBITMQ__Host, RABBITMQ__Password, RABBITMQ__Username, etc.
```
