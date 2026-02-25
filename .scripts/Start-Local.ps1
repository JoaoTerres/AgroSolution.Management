<#
.SYNOPSIS
    AgroSolution -- ambiente local completo em um unico comando.

.DESCRIPTION
    Este script automatiza todo o setup para rodar o projeto localmente:

      1. Verifica pre-requisitos (.NET 9 SDK, Docker Desktop)
      2. Cria o arquivo .env a partir de .env.example (se nao existir)
      3. Inicia o Docker Desktop (se nao estiver rodando)
      4. Sobe postgres + rabbitmq via docker compose
      5. Aguarda healthcheck dos containers
      6. Aplica as migrations de EF Core (Management + Identity)
      7. Builda todos os projetos
      8. Inicia AgroSolution.Identity   (http://localhost:5001)
              AgroSolution.Api        (http://localhost:5034)
              AgroSolution.Worker     (background)
         em janelas PowerShell separadas
      9. Aguarda as APIs responderem em /health
     10. Executa o smoke test (opcional -- -SkipSmokeTest para pular)
     11. Exibe o sumario de URLs

.PARAMETER SkipSmokeTest
    Pula a execucao do smoke test apos iniciar os servicos.

.PARAMETER SkipBuild
    Pula o 'dotnet build' (util quando o codigo nao mudou).

.EXAMPLE
    # Primeira execucao (setup completo + smoke test)
    Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
    .\.scripts\Start-Local.ps1

    # Reiniciar sem rebuild
    .\.scripts\Start-Local.ps1 -SkipBuild

    # Apenas subir servicos, sem smoke test
    .\.scripts\Start-Local.ps1 -SkipSmokeTest
#>
param(
    [switch]$SkipSmokeTest,
    [switch]$SkipBuild
)

Set-StrictMode -Off
$ErrorActionPreference = 'Continue'

$Root         = Split-Path $PSScriptRoot -Parent
$ScriptsDir   = $PSScriptRoot
$ApiUrl       = 'http://localhost:5034'
$IdentityUrl  = 'http://localhost:5001'

Set-Location $Root

# ---- Helper: printing ---------------------------------------------------------

function Write-Banner {
    param([string]$Text)
    Write-Host ""
    Write-Host "  +=============================================================+" -ForegroundColor Cyan
    Write-Host ("  |  {0,-59}|" -f $Text) -ForegroundColor Cyan
    Write-Host "  +=============================================================+" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step {
    param([int]$Num, [string]$Text)
    Write-Host ("  [{0}] {1}" -f $Num, $Text) -ForegroundColor Yellow
}

function Write-OK   { param([string]$t); Write-Host ("      [OK] {0}" -f $t) -ForegroundColor Green }
function Write-FAIL { param([string]$t); Write-Host ("      [FAIL] {0}" -f $t) -ForegroundColor Red }
function Write-INFO { param([string]$t); Write-Host ("      --> {0}" -f $t) -ForegroundColor DarkGray }

# ---- STEP 1: Pre-requisitos ---------------------------------------------------

Write-Banner "AgroSolution -- Start Local Environment"
Write-Step 1 "Verificando pre-requisitos..."

# .NET SDK 9
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-FAIL ".NET SDK nao encontrado. Instale em https://dot.net/download"
    exit 1
}
$dotnetVer = (dotnet --version 2>$null)
if ($dotnetVer -notmatch '^9\.') {
    Write-FAIL ".NET 9 SDK necessario (encontrado: $dotnetVer). Instale em https://dot.net/download"
    exit 1
}
Write-OK ".NET $dotnetVer"

# Docker
$dockerCmd = Get-Command docker -ErrorAction SilentlyContinue
if (-not $dockerCmd) {
    Write-FAIL "Docker nao encontrado. Instale Docker Desktop em https://docker.com"
    exit 1
}
Write-OK "docker encontrado"

# ---- STEP 2: .env -------------------------------------------------------------

Write-Step 2 "Configurando .env..."

if (-not (Test-Path "$Root\.env")) {
    if (Test-Path "$Root\.env.example") {
        Copy-Item "$Root\.env.example" "$Root\.env"
        Write-OK ".env criado a partir de .env.example"
        Write-INFO "Edite .env para ajustar senhas se necessario."
    } else {
        Write-FAIL ".env.example nao encontrado. Crie manualmente o arquivo .env."
        exit 1
    }
} else {
    Write-OK ".env ja existe"
}

# ---- STEP 3: Docker Desktop ---------------------------------------------------

Write-Step 3 "Verificando Docker Desktop..."

$dockerRunning = $false
try { docker info 2>&1 | Out-Null; $dockerRunning = $true } catch {}

if (-not $dockerRunning) {
    Write-INFO "Docker nao esta rodando. Iniciando Docker Desktop..."
    $desktopPath = "C:\Program Files\Docker\Docker\Docker Desktop.exe"
    if (Test-Path $desktopPath) {
        Start-Process $desktopPath
    } else {
        Write-FAIL "Docker Desktop nao encontrado em '$desktopPath'. Inicie manualmente."
        exit 1
    }
    $timeout = 90
    $elapsed = 0
    while ($elapsed -lt $timeout) {
        Start-Sleep 3; $elapsed += 3
        $ok = $false
        try { docker info 2>&1 | Out-Null; $ok = $true } catch {}
        if ($ok) { break }
        Write-Host ("      Aguardando Docker Desktop... {0}s" -f $elapsed) -NoNewline
        Write-Host "`r" -NoNewline
    }
    try { docker info 2>&1 | Out-Null } catch {
        Write-FAIL "Docker Desktop nao respondeu em $timeout s."
        exit 1
    }
}
Write-OK "Docker Desktop rodando"

# ---- STEP 4: Infra containers -------------------------------------------------

Write-Step 4 "Subindo postgres + rabbitmq..."

$composeFile = "$Root\docker-compose.yml"
docker compose -f $composeFile up -d --remove-orphans postgres rabbitmq 2>&1 | Out-Null

# Wait for healthy
$containers = @("agrosolution-postgres", "agrosolution-rabbitmq")
foreach ($c in $containers) {
    $seconds = 0
    while ($seconds -lt 60) {
        $health = docker inspect $c --format "{{.State.Health.Status}}" 2>$null
        if ($health -eq "healthy") { break }
        Start-Sleep 2; $seconds += 2
        Write-Host ("      Aguardando {0} ficar healthy... {1}s" -f $c, $seconds) -NoNewline
        Write-Host "`r" -NoNewline
    }
    $health = docker inspect $c --format "{{.State.Health.Status}}" 2>$null
    if ($health -eq "healthy") {
        Write-OK ("{0}: healthy" -f $c)
    } else {
        Write-FAIL ("{0}: nao ficou healthy em 60s (estado: {1})." -f $c, $health)
        exit 1
    }
}

# ---- STEP 5: Migrations -------------------------------------------------------

Write-Step 5 "Aplicando migrations EF Core..."

# Management DB -- skip InitialMigration se necessario
$histCheck = "SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='20260223025205_InitialMigration';"
$histResult = ($histCheck | docker exec -i agrosolution-postgres psql -U postgres -d agrosolution_management -tA 2>$null).Trim()

if ($histResult -ne "1") {
    Write-INFO "Inserindo placeholder para InitialMigration (previne conflito de tabelas)..."
    $fakeInsert = "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20260223025205_InitialMigration','9.0.0') ON CONFLICT DO NOTHING;"
    $fakeInsert | docker exec -i agrosolution-postgres psql -U postgres -d agrosolution_management 2>&1 | Out-Null
}

Push-Location "$Root\AgroSolution.Api"
dotnet ef database update --no-build 2>&1 | Select-Object -Last 5 | ForEach-Object { Write-INFO $_ }
Pop-Location
Write-OK "Management migrations aplicadas"

Push-Location "$Root\AgroSolution.Identity"
dotnet ef database update --no-build 2>&1 | Select-Object -Last 5 | ForEach-Object { Write-INFO $_ }
Pop-Location
Write-OK "Identity migrations aplicadas"

# ---- STEP 6: Build ------------------------------------------------------------

Write-Step 6 "Buildando solucao..."

if (-not $SkipBuild) {
    $buildOut = dotnet build "$Root\AgroSolution.Management.sln" -c Debug --nologo 2>&1
    $errors = $buildOut | Where-Object { $_ -match ' error ' }
    if ($errors) {
        $errors | ForEach-Object { Write-FAIL $_ }
        exit 1
    }
    Write-OK "Build concluida sem erros"
} else {
    Write-INFO "Build pulado (-SkipBuild)"
}

# ---- STEP 7: Iniciar servicos -------------------------------------------------

Write-Step 7 "Iniciando servicos .NET..."

function Start-Service {
    param([string]$Name, [string]$Project, [string]$Url)
    Write-INFO "Iniciando $Name em $Url ..."
    $args = "-NoExit -Command `"Set-Location '$Root\$Project'; `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run --no-build --urls $Url`""
    Start-Process powershell -ArgumentList $args -WindowStyle Minimized
}

# Kill any existing instances first
Get-Process -Name "AgroSolution.Api", "AgroSolution.Identity", "AgroSolution.Worker" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep 1

Start-Service -Name "AgroSolution.Identity" -Project "AgroSolution.Identity" -Url "http://localhost:5001"
Start-Service -Name "AgroSolution.Api"      -Project "AgroSolution.Api"      -Url "http://localhost:5034"

# Worker (no URL)
Write-INFO "Iniciando AgroSolution.Worker..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$Root\AgroSolution.Worker'; `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run --no-build" -WindowStyle Minimized

# ---- STEP 8: Aguardar /health -------------------------------------------------

Write-Step 8 "Aguardando APIs ficarem prontas..."

function Wait-Health {
    param([string]$Name, [string]$Url, [int]$TimeoutSec = 60)
    $elapsed = 0
    while ($elapsed -lt $TimeoutSec) {
        try {
            $code = (Invoke-WebRequest "$Url/health" -UseBasicParsing -TimeoutSec 2).StatusCode
            if ($code -eq 200) {
                Write-OK ("{0} pronto em {1}/health" -f $Name, $Url)
                return $true
            }
        } catch {}
        Start-Sleep 2; $elapsed += 2
        Write-Host ("      Aguardando {0}... {1}s" -f $Name, $elapsed) -NoNewline
        Write-Host "`r" -NoNewline
    }
    Write-FAIL ("{0} nao respondeu em {1}s" -f $Name, $TimeoutSec)
    return $false
}

$apiOk      = Wait-Health -Name "AgroSolution.Api"      -Url $ApiUrl
$identityOk = Wait-Health -Name "AgroSolution.Identity" -Url $IdentityUrl

if (-not ($apiOk -and $identityOk)) {
    Write-Host ""
    Write-FAIL "Um ou mais servicos nao iniciaram corretamente."
    Write-INFO "Verifique as janelas abertas para ver logs de erro."
    exit 1
}

# ---- STEP 9: Smoke test -------------------------------------------------------

if (-not $SkipSmokeTest) {
    Write-Step 9 "Executando smoke test (D-04)..."
    $smokeScript = "$ScriptsDir\Invoke-SmokeTest.ps1"
    if (Test-Path $smokeScript) {
        & $smokeScript -ApiUrl $ApiUrl -IdentityUrl $IdentityUrl
    } else {
        Write-INFO "Smoke test nao encontrado em $smokeScript -- pulando."
    }
} else {
    Write-INFO "Smoke test pulado (-SkipSmokeTest)"
}

# ---- STEP 10: Sumario ---------------------------------------------------------

Write-Host ""
Write-Host "  +=============================================================+" -ForegroundColor Green
Write-Host "  |  AMBIENTE LOCAL PRONTO                                      |" -ForegroundColor Green
Write-Host "  +=============================================================+" -ForegroundColor Green
Write-Host ""
Write-Host "  Servicos:" -ForegroundColor White
Write-Host ("  {0,-30} {1}" -f "AgroSolution.Api (HTTP)    :", $ApiUrl) -ForegroundColor Cyan
Write-Host ("  {0,-30} {1}/swagger" -f "AgroSolution.Api (Swagger):", $ApiUrl) -ForegroundColor Cyan
Write-Host ("  {0,-30} {1}" -f "AgroSolution.Identity      :", $IdentityUrl) -ForegroundColor Cyan
Write-Host ("  {0,-30} {1}/swagger" -f "AgroSolution.Identity Sw  :", $IdentityUrl) -ForegroundColor Cyan
Write-Host ""
Write-Host "  Infra:" -ForegroundColor White
Write-Host ("  {0,-30} localhost:5432  (user: postgres / pass: postgres)" -f "PostgreSQL:") -ForegroundColor DarkCyan
Write-Host ("  {0,-30} localhost:15672 (user: agro / pass: agro123)" -f "RabbitMQ Management UI:") -ForegroundColor DarkCyan
Write-Host ("  {0,-30} localhost:54320 (admin@agrosolution.local / admin123)" -f "pgAdmin:") -ForegroundColor DarkCyan
Write-Host ""
Write-Host "  Scripts uteis:" -ForegroundColor White
Write-Host ("  {0,-30} {1}" -f "Smoke test          :", ".\.scripts\Invoke-SmokeTest.ps1") -ForegroundColor DarkGray
Write-Host ("  {0,-30} {1}" -f "Load benchmark      :", ".\.scripts\Invoke-LoadTest.ps1 -DurationSeconds 30 -Concurrency 10") -ForegroundColor DarkGray
Write-Host ("  {0,-30} {1}" -f "Metricas Prometheus :", "$ApiUrl/metrics") -ForegroundColor DarkGray
Write-Host ""
Write-Host "  Para parar tudo:" -ForegroundColor White
Write-Host "  docker compose down" -ForegroundColor DarkGray
Write-Host "  Get-Process dotnet | Stop-Process -Force" -ForegroundColor DarkGray
Write-Host ""
