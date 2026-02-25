<#
.SYNOPSIS
    AgroSolution smoke test -- validates the full D-04 demo scenario end-to-end.
.PARAMETER ApiUrl
    Base URL of AgroSolution.Api. Default: http://localhost:5034
.PARAMETER IdentityUrl
    Base URL of AgroSolution.Identity. Default: http://localhost:5001
.EXAMPLE
    .scripts\Invoke-SmokeTest.ps1
    .scripts\Invoke-SmokeTest.ps1 -ApiUrl http://localhost:30080 -IdentityUrl http://localhost:30081
#>
param(
    [string]$ApiUrl      = 'http://localhost:5034',
    [string]$IdentityUrl = 'http://localhost:5001'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:Passed = 0
$script:Failed = 0

function Write-Step { param([string]$Name); Write-Host "`n  >> $Name" -ForegroundColor Cyan }

function Report {
    param([string]$Label,[bool]$Ok,[int]$StatusCode,[long]$ElapsedMs,[string]$Detail='')
    $icon   = if ($Ok) { 'OK' } else { 'FAIL' }
    $colour = if ($Ok) { 'Green' } else { 'Red' }
    Write-Host ("    [{0}] {1,-44} [{2}]  {3} ms  {4}" -f $icon,$Label,$StatusCode,$ElapsedMs,$Detail) -ForegroundColor $colour
    if ($Ok) { $script:Passed++ } else { $script:Failed++ }
}

function Invoke-Step {
    param([string]$Label,[string]$Uri,[string]$Method='GET',[object]$Body=$null,
          [hashtable]$Headers=@{},[int[]]$ExpectedCodes=@(200,201),[switch]$Raw)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $params = @{ Uri=$Uri; Method=$Method; UseBasicParsing=$true
                     Headers=( $Headers + @{'Content-Type'='application/json'} ) }
        if ($Body) { $params['Body'] = ($Body | ConvertTo-Json -Depth 10) }
        $resp = Invoke-WebRequest @params
        $sw.Stop()
        $ok   = $resp.StatusCode -in $ExpectedCodes
        Report -Label $Label -Ok $ok -StatusCode $resp.StatusCode -ElapsedMs $sw.ElapsedMilliseconds -Detail $resp.Content.Substring(0,[Math]::Min(60,$resp.Content.Length))
        if ($Raw) { return $resp.Content }
        $json = $resp.Content | ConvertFrom-Json
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
        $body2  = $reader.ReadToEnd() | ConvertFrom-Json
        Report -Label $Label -Ok $true -StatusCode $code -ElapsedMs $sw.ElapsedMilliseconds
        return $body2
    }
    catch {
        $sw.Stop()
        Report -Label $Label -Ok $false -StatusCode 0 -ElapsedMs $sw.ElapsedMilliseconds -Detail $_.Exception.Message
        return $null
    }
}

$timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
Write-Host ""
Write-Host "  +---------------------------------------------------+" -ForegroundColor Yellow
Write-Host "  |  AgroSolution -- Smoke Test (D-04 Demo Flow)      |" -ForegroundColor Yellow
Write-Host ("  |  {0}                        |" -f $timestamp) -ForegroundColor Yellow
Write-Host ("  |  Api:      {0}" -f $ApiUrl) -ForegroundColor Yellow
Write-Host ("  |  Identity: {0}" -f $IdentityUrl) -ForegroundColor Yellow
Write-Host "  +---------------------------------------------------+" -ForegroundColor Yellow

# STEP 0 - Health checks
Write-Step "STEP 0 -- Health checks"
Invoke-Step -Label "GET /health (Api)"      -Uri "$ApiUrl/health"      -Raw | Out-Null
Invoke-Step -Label "GET /health (Identity)" -Uri "$IdentityUrl/health" -Raw | Out-Null

# STEP 1 - Register
Write-Step "STEP 1 -- Register producer (FR-01)"
$email    = "smoke_$(Get-Date -Format 'yyyyMMddHHmmss')@agro.test"
$password = "SmokeTest@123"
Invoke-Step -Label "POST /api/auth/register" `
    -Uri "$IdentityUrl/api/auth/register" -Method 'POST' `
    -Body @{ email=$email; password=$password; name="Smoke Tester" } `
    -ExpectedCodes @(200,201) | Out-Null

# STEP 2 - Login
Write-Step "STEP 2 -- Login -> JWT (FR-01)"
$login = Invoke-Step -Label "POST /api/auth/login" `
    -Uri "$IdentityUrl/api/auth/login" -Method 'POST' `
    -Body @{ email=$email; password=$password }

$jwtToken = $null
if ($login -and $login.data -and $login.data.accessToken) { $jwtToken = $login.data.accessToken }
elseif ($login -and $login.accessToken)                    { $jwtToken = $login.accessToken }
elseif ($login -and $login.token)                          { $jwtToken = $login.token }
if (-not $jwtToken) {
    Write-Host "`n  FATAL: could not obtain JWT -- aborting." -ForegroundColor Red
    exit 1
}
$authHeader = @{ Authorization = "Bearer $jwtToken" }
Write-Host ("         JWT: {0}..." -f $jwtToken.Substring(0,[Math]::Min(30,$jwtToken.Length))) -ForegroundColor DarkGray

# STEP 3 - Property
Write-Step "STEP 3 -- Property management (FR-02)"
$prop = Invoke-Step -Label "POST /api/properties" `
    -Uri "$ApiUrl/api/properties" -Method 'POST' -Headers $authHeader `
    -Body @{ name="Fazenda Smoke"; location="MT, Brasil" }

$propertyId = $null
if ($prop -and $prop.data) {
    if ($prop.data -is [string]) { $propertyId = $prop.data }
    else { $propertyId = $prop.data.id }
} elseif ($prop -and $prop.id) { $propertyId = $prop.id }

# STEP 4 - Plot
Write-Step "STEP 4 -- Add plot (FR-02)"
$plot = Invoke-Step -Label "POST /api/plots" `
    -Uri "$ApiUrl/api/plots" -Method 'POST' -Headers $authHeader `
    -Body @{ propertyId=$propertyId; name="Talhao A"; cropType="Soja"; area=10.5 }

$plotId = $null
if ($plot -and $plot.data) {
    if ($plot.data -is [string]) { $plotId = $plot.data }
    else { $plotId = $plot.data.id }
} elseif ($plot -and $plot.id) { $plotId = $plot.id }

# STEP 5 - IoT ingestion (3 low-humidity readings spanning 26h)
Write-Step "STEP 5 -- IoT data ingestion (FR-03)"
$readings = @(
    @{ plotId=$plotId; deviceType="HumiditySensor"; value=25.0; unit="%" },
    @{ plotId=$plotId; deviceType="HumiditySensor"; value=22.0; unit="%" },
    @{ plotId=$plotId; deviceType="HumiditySensor"; value=20.0; unit="%" }
)
foreach ($r in $readings) {
    Invoke-Step -Label ("POST /api/iot/data  [humidity={0}%]" -f $r.value) `
        -Uri "$ApiUrl/api/iot/data" -Method 'POST' -Headers $authHeader `
        -Body $r | Out-Null
}

# STEP 6 - Dashboard
Write-Step "STEP 6 -- Dashboard historical data (FR-04)"
$from = (Get-Date).AddDays(-1).ToString("yyyy-MM-dd")
$to   = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")
$dashUri = ($ApiUrl + "/api/iot/data/" + $plotId + "?from=" + $from + "&to=" + $to)
Invoke-Step -Label "GET /api/iot/data/{plotId}?from=...&to=..." `
    -Uri $dashUri -Headers $authHeader | Out-Null

# STEP 7 - Alerts
Write-Step "STEP 7 -- Alert engine output (FR-05)"
$alerts = Invoke-Step -Label "GET /api/alerts/{plotId}" `
    -Uri "$ApiUrl/api/alerts/$plotId" -Headers $authHeader -ExpectedCodes @(200)

if ($alerts) {
    $cnt = if ($alerts -is [array]) { $alerts.Count } else { ($alerts | Measure-Object).Count }
    Write-Host ("         Alerts returned: {0}" -f $cnt) -ForegroundColor DarkGray
}

# Summary
$total = $script:Passed + $script:Failed
Write-Host ""
Write-Host "  -------------------------------------------------------" -ForegroundColor Yellow
if ($script:Failed -eq 0) {
    Write-Host ("  RESULT  PASS -- {0}/{1} steps succeeded" -f $script:Passed,$total) -ForegroundColor Green
} else {
    Write-Host ("  RESULT  FAIL -- {0} passed / {1} FAILED (total {2})" -f $script:Passed,$script:Failed,$total) -ForegroundColor Red
}
Write-Host "  -------------------------------------------------------" -ForegroundColor Yellow
Write-Host ""
exit $(if ($script:Failed -eq 0) { 0 } else { 1 })
