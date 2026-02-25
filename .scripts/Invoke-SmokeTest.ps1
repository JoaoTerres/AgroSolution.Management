<#
.SYNOPSIS
    AgroSolution smoke test — validates the full D-04 demo scenario end-to-end.

.DESCRIPTION
    Executes every step of the D-04 demo flow against a running environment.
    Reports PASS/FAIL + HTTP status + elapsed ms per step.
    Exits with code 0 on full pass, 1 on any failure.

.PARAMETER ApiUrl
    Base URL of AgroSolution.Api.       Default: http://localhost:5000
.PARAMETER IdentityUrl
    Base URL of AgroSolution.Identity.  Default: http://localhost:5001
.PARAMETER Verbose
    Print full response bodies.

.EXAMPLE
    # Local docker-compose
    .\.scripts\Invoke-SmokeTest.ps1

    # Kubernetes (minikube)
    .\.scripts\Invoke-SmokeTest.ps1 -ApiUrl http://$(minikube ip)/api -IdentityUrl http://$(minikube ip)/identity
#>
param(
    [string]$ApiUrl      = 'http://localhost:5000',
    [string]$IdentityUrl = 'http://localhost:5001',
    [switch]$Verbose
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Helpers ──────────────────────────────────────────────────────────────────

$script:Passed = 0
$script:Failed = 0

function Write-Step {
    param([string]$Name)
    Write-Host "`n  ► $Name" -ForegroundColor Cyan
}

function Report {
    param([string]$Label, [bool]$Ok, [int]$StatusCode, [long]$ElapsedMs, [string]$Detail = '')
    $icon   = if ($Ok) { '✓' } else { '✗' }
    $colour = if ($Ok) { 'Green' } else { 'Red' }
    Write-Host ("    {0} {1,-42} [{2}]  {3} ms  {4}" -f $icon, $Label, $StatusCode, $ElapsedMs, $Detail) -ForegroundColor $colour
    if ($Ok) { $script:Passed++ } else { $script:Failed++ }
}

function Invoke-Step {
    param(
        [string]$Label,
        [string]$Uri,
        [string]$Method       = 'GET',
        [object]$Body         = $null,
        [hashtable]$Headers   = @{},
        [int[]]$ExpectedCodes = @(200, 201)
    )

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $params = @{
            Uri             = $Uri
            Method          = $Method
            Headers         = $Headers + @{ 'Content-Type' = 'application/json' }
            UseBasicParsing = $true
        }
        if ($Body) { $params['Body'] = ($Body | ConvertTo-Json -Depth 10) }

        $resp   = Invoke-WebRequest @params
        $sw.Stop()
        $json   = $resp.Content | ConvertFrom-Json
        $ok     = $resp.StatusCode -in $ExpectedCodes

        if ($Verbose) { $resp.Content | ConvertFrom-Json | ConvertTo-Json -Depth 5 | Write-Host -ForegroundColor DarkGray }
        Report -Label $Label -Ok $ok -StatusCode $resp.StatusCode -ElapsedMs $sw.ElapsedMilliseconds
        return $json
    }
    catch [System.Net.WebException] {
        $sw.Stop()
        $code = [int]$_.Exception.Response.StatusCode
        $ok   = $code -in $ExpectedCodes
        if (-not $ok) {
            Report -Label $Label -Ok $false -StatusCode $code -ElapsedMs $sw.ElapsedMilliseconds -Detail $_.Exception.Message
            return $null
        }
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = [System.IO.StreamReader]::new($stream)
        $body   = $reader.ReadToEnd() | ConvertFrom-Json
        Report -Label $Label -Ok $true -StatusCode $code -ElapsedMs $sw.ElapsedMilliseconds
        return $body
    }
    catch {
        $sw.Stop()
        Report -Label $Label -Ok $false -StatusCode 0 -ElapsedMs $sw.ElapsedMilliseconds -Detail $_.Exception.Message
        return $null
    }
}

# ── Banner ───────────────────────────────────────────────────────────────────

$timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
Write-Host ""
Write-Host "  ┌─────────────────────────────────────────────────┐" -ForegroundColor Yellow
Write-Host "  │   AgroSolution — Smoke Test (D-04 Demo Flow)    │" -ForegroundColor Yellow
Write-Host "  │   $timestamp                          │" -ForegroundColor Yellow
Write-Host "  │   Api:      $ApiUrl" -ForegroundColor Yellow
Write-Host "  │   Identity: $IdentityUrl" -ForegroundColor Yellow
Write-Host "  └─────────────────────────────────────────────────┘" -ForegroundColor Yellow

# ── Step 0: Health checks ────────────────────────────────────────────────────

Write-Step "STEP 0 — Health checks"
Invoke-Step -Label "GET /health  (Api)"      -Uri "$ApiUrl/health"
Invoke-Step -Label "GET /health  (Identity)" -Uri "$IdentityUrl/health"

# ── Step 1: Register producer ─────────────────────────────────────────────────

Write-Step "STEP 1 — Register producer (FR-01)"
$email    = "smoketest_$(Get-Date -Format 'yyyyMMddHHmmss')@agro.test"
$password = "SmokeTest@123"

$reg = Invoke-Step -Label "POST /api/auth/register" `
    -Uri "$IdentityUrl/api/auth/register" `
    -Method 'POST' `
    -Body @{ email = $email; password = $password; name = "Smoke Tester" } `
    -ExpectedCodes @(200, 201)

# ── Step 2: Login ────────────────────────────────────────────────────────────

Write-Step "STEP 2 — Login → JWT (FR-01)"
$login = Invoke-Step -Label "POST /api/auth/login" `
    -Uri "$IdentityUrl/api/auth/login" `
    -Method 'POST' `
    -Body @{ email = $email; password = $password }

if (-not $login -or -not $login.token) {
    Write-Host "`n  FATAL: could not obtain JWT — aborting remaining steps." -ForegroundColor Red
    exit 1
}

$authHeader = @{ Authorization = "Bearer $($login.token)" }
Write-Host "         JWT obtained (first 30 chars): $($login.token.Substring(0,30))..." -ForegroundColor DarkGray

# ── Step 3: Create property ───────────────────────────────────────────────────

Write-Step "STEP 3 — Property management (FR-02)"
$prop = Invoke-Step -Label "POST /api/properties" `
    -Uri "$ApiUrl/api/properties" `
    -Method 'POST' `
    -Headers $authHeader `
    -Body @{ name = "Fazenda Smoke"; location = "MT, Brasil" }

$propertyId = $prop.id ?? $prop.data.id

# ── Step 4: Add plot ──────────────────────────────────────────────────────────

Write-Step "STEP 4 — Add plot (FR-02)"
$plot = Invoke-Step -Label "POST /api/properties/{id}/plots" `
    -Uri "$ApiUrl/api/properties/$propertyId/plots" `
    -Method 'POST' `
    -Headers $authHeader `
    -Body @{ name = "Talhão A"; cropType = "Soja"; area = 10.5 }

$plotId = $plot.id ?? $plot.data.id

# ── Step 5: Ingest IoT data (humidity below threshold → trigger alert) ────────

Write-Step "STEP 5 — IoT data ingestion (FR-03)"
$readingsPast = @(
    @{ plotId = $plotId; deviceType = "HumiditySensor"; value = 25.0; unit = "%"; recordedAt = (Get-Date).AddHours(-26).ToString("o") },
    @{ plotId = $plotId; deviceType = "HumiditySensor"; value = 22.0; unit = "%"; recordedAt = (Get-Date).AddHours(-13).ToString("o") },
    @{ plotId = $plotId; deviceType = "HumiditySensor"; value = 20.0; unit = "%"; recordedAt = (Get-Date).ToString("o") }
)
foreach ($r in $readingsPast) {
    Invoke-Step -Label "POST /api/iot/data  [humidity=$($r.value)%]" `
        -Uri "$ApiUrl/api/iot/data" `
        -Method 'POST' `
        -Headers $authHeader `
        -Body $r | Out-Null
}

# ── Step 6: Dashboard — historical data ──────────────────────────────────────

Write-Step "STEP 6 — Dashboard historical data (FR-04)"
$from = (Get-Date).AddDays(-1).ToString("yyyy-MM-dd")
$to   = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")
Invoke-Step -Label "GET /api/iot/data/{plotId}?from=$from&to=$to" `
    -Uri "$ApiUrl/api/iot/data/$plotId`?from=$from&to=$to" `
    -Headers $authHeader | Out-Null

# ── Step 7: Alerts ────────────────────────────────────────────────────────────

Write-Step "STEP 7 — Alert engine output (FR-05)"
$alerts = Invoke-Step -Label "GET /api/alerts/{plotId}" `
    -Uri "$ApiUrl/api/alerts/$plotId" `
    -Headers $authHeader `
    -ExpectedCodes @(200)

if ($alerts) {
    $count = if ($alerts -is [array]) { $alerts.Count } else { ($alerts | Measure-Object).Count }
    Write-Host "         Alerts found: $count" -ForegroundColor DarkGray
}

# ── Summary ───────────────────────────────────────────────────────────────────

$total = $script:Passed + $script:Failed
Write-Host ""
Write-Host "  ─────────────────────────────────────────────────────" -ForegroundColor Yellow
if ($script:Failed -eq 0) {
    Write-Host ("  RESULT  PASS — {0}/{1} steps succeeded" -f $script:Passed, $total) -ForegroundColor Green
} else {
    Write-Host ("  RESULT  FAIL — {0} passed, {1} FAILED (total {2})" -f $script:Passed, $script:Failed, $total) -ForegroundColor Red
}
Write-Host "  ─────────────────────────────────────────────────────" -ForegroundColor Yellow
Write-Host ""

exit $(if ($script:Failed -eq 0) { 0 } else { 1 })
