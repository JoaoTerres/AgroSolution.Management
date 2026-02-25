<#
.SYNOPSIS
    AgroSolution load benchmark — measures API throughput and latency under concurrent load.

.DESCRIPTION
    Runs parallel HTTP requests against the two hot paths:
      • POST /api/iot/data        (IoT ingestion — write path)
      • GET  /api/iot/data/{plot} (dashboard query — read path)

    Collects per-request latency, then reports:
      RPS, success rate, Min/Avg/P50/P95/P99/Max latency.

    Results are saved to benchmark/results_<timestamp>.json so you can
    track performance across runs and paste the summary into README.md.

.PARAMETER ApiUrl
    Base URL of AgroSolution.Api.  Default: http://localhost:5000
.PARAMETER IdentityUrl
    Base URL of AgroSolution.Identity.  Default: http://localhost:5001
.PARAMETER Concurrency
    Number of virtual users (parallel runspaces).  Default: 10
.PARAMETER DurationSeconds
    How long to sustain load.  Default: 30
.PARAMETER Email
    Producer email to login with.  If omitted, a new account is registered.
.PARAMETER Password
    Producer password.  Default: BenchmarkPwd@1
.PARAMETER PlotId
    GUID of the plot to target. If omitted, a property + plot are created automatically.
.PARAMETER OutputDir
    Directory for JSON result files.  Default: benchmark/

.EXAMPLE
    # Quick 10 s run with 5 concurrent users (local)
    .\.scripts\Invoke-LoadTest.ps1 -DurationSeconds 10 -Concurrency 5

    # Full benchmark against minikube
    .\.scripts\Invoke-LoadTest.ps1 -ApiUrl http://192.168.49.2/api -IdentityUrl http://192.168.49.2/identity -Concurrency 20 -DurationSeconds 60
#>
param(
    [string] $ApiUrl          = 'http://localhost:5000',
    [string] $IdentityUrl     = 'http://localhost:5001',
    [int]    $Concurrency     = 10,
    [int]    $DurationSeconds = 30,
    [string] $Email           = '',
    [string] $Password        = 'BenchmarkPwd@1',
    [string] $PlotId          = '',
    [string] $OutputDir       = 'benchmark'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Helpers ──────────────────────────────────────────────────────────────────

function Invoke-JsonPost {
    param([string]$Uri, [object]$Body, [hashtable]$Headers = @{})
    $json    = $Body | ConvertTo-Json -Depth 5
    $bytes   = [System.Text.Encoding]::UTF8.GetBytes($json)
    $request = [System.Net.WebRequest]::Create($Uri)
    $request.Method      = 'POST'
    $request.ContentType = 'application/json'
    $request.ContentLength = $bytes.Length
    foreach ($k in $Headers.Keys) { $request.Headers[$k] = $Headers[$k] }
    $stream = $request.GetRequestStream()
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Close()
    $response = $request.GetResponse()
    $reader   = [System.IO.StreamReader]::new($response.GetResponseStream())
    $result   = $reader.ReadToEnd() | ConvertFrom-Json
    $response.Close()
    return $result
}

function Invoke-JsonGet {
    param([string]$Uri, [hashtable]$Headers = @{})
    $request = [System.Net.WebRequest]::Create($Uri)
    $request.Method = 'GET'
    foreach ($k in $Headers.Keys) { $request.Headers[$k] = $Headers[$k] }
    $response = $request.GetResponse()
    $reader   = [System.IO.StreamReader]::new($response.GetResponseStream())
    $result   = $reader.ReadToEnd() | ConvertFrom-Json
    $response.Close()
    return $result
}

function Get-Percentile {
    param([long[]]$Sorted, [double]$Percentile)
    if ($Sorted.Count -eq 0) { return 0 }
    $index = [Math]::Ceiling($Percentile / 100.0 * $Sorted.Count) - 1
    $index = [Math]::Max(0, [Math]::Min($index, $Sorted.Count - 1))
    return $Sorted[$index]
}

# ── Banner ───────────────────────────────────────────────────────────────────

$runStart = Get-Date
Write-Host ""
Write-Host "  ┌──────────────────────────────────────────────────────┐" -ForegroundColor Yellow
Write-Host "  │   AgroSolution — Load Benchmark                      │" -ForegroundColor Yellow
Write-Host ("  │   Concurrency : {0,-5}  Duration: {1} s                │" -f $Concurrency, $DurationSeconds) -ForegroundColor Yellow
Write-Host ("  │   Api         : {0,-38}│" -f $ApiUrl) -ForegroundColor Yellow
Write-Host "  └──────────────────────────────────────────────────────┘" -ForegroundColor Yellow
Write-Host ""

# ── Auth + Setup ─────────────────────────────────────────────────────────────

Write-Host "  [setup] Authenticating..." -ForegroundColor Cyan
if (-not $Email) {
    $Email = "bench_$(Get-Date -Format 'yyyyMMddHHmmss')@agro.test"
    try {
        Invoke-JsonPost -Uri "$IdentityUrl/api/auth/register" `
            -Body @{ email = $Email; password = $Password; name = "Benchmarker" } | Out-Null
        Write-Host "           Registered new producer: $Email" -ForegroundColor DarkGray
    } catch { Write-Host "           (register skipped — account may already exist)" -ForegroundColor DarkGray }
}

$loginResp = Invoke-JsonPost -Uri "$IdentityUrl/api/auth/login" `
    -Body @{ email = $Email; password = $Password }
$jwt = $loginResp.token
Write-Host "           JWT obtained." -ForegroundColor DarkGray

$authHeader = @{ Authorization = "Bearer $jwt" }

if (-not $PlotId) {
    Write-Host "  [setup] Creating property + plot..." -ForegroundColor Cyan
    $prop = Invoke-JsonPost -Uri "$ApiUrl/api/properties" `
        -Headers $authHeader `
        -Body @{ name = "BenchFarm_$(Get-Random -Maximum 9999)"; location = "MT, Brasil" }
    $propertyId = if ($prop.id) { $prop.id } elseif ($prop.data) { $prop.data.id } else { $null }

    $plot = Invoke-JsonPost -Uri "$ApiUrl/api/properties/$propertyId/plots" `
        -Headers $authHeader `
        -Body @{ name = "Talhão Bench"; cropType = "Milho"; area = 5.0 }
    $PlotId = if ($plot.id) { $plot.id } elseif ($plot.data) { $plot.data.id } else { $null }
    Write-Host "           PlotId: $PlotId" -ForegroundColor DarkGray
}

# ── Load test using Runspaces ─────────────────────────────────────────────────

Write-Host ""
Write-Host "  [bench] Starting load test ($Concurrency workers × $DurationSeconds s)..." -ForegroundColor Cyan
Write-Host ""

# Shared thread-safe collections
$writeLatencies = [System.Collections.Concurrent.ConcurrentBag[long]]::new()
$writeFailed    = [System.Collections.Concurrent.ConcurrentBag[string]]::new()
$readLatencies  = [System.Collections.Concurrent.ConcurrentBag[long]]::new()
$readFailed     = [System.Collections.Concurrent.ConcurrentBag[string]]::new()

$deadline     = [DateTime]::UtcNow.AddSeconds($DurationSeconds)
$writePayload = [PSCustomObject]@{
    plotId     = $PlotId
    deviceType = "HumiditySensor"
    value      = 35.0
    unit       = "%"
    recordedAt = [DateTime]::UtcNow.ToString("o")
}

$scriptBlock = {
    param($ApiUrl, $PlotId, $Jwt, $Deadline, $WriteBag, $WriteFail, $ReadBag, $ReadFail)

    $headers = @{ Authorization = "Bearer $Jwt"; 'Content-Type' = 'application/json' }
    $writeUri = "$ApiUrl/api/iot/data"
    $from     = [DateTime]::UtcNow.AddDays(-1).ToString("yyyy-MM-dd")
    $to       = [DateTime]::UtcNow.AddDays(1).ToString("yyyy-MM-dd")
    $readUri  = ("$ApiUrl/api/iot/data/$PlotId" + "?from=$from" + "&to=$to")
    $iter     = 0

    while ([DateTime]::UtcNow -lt $Deadline) {
        #  Alternate write / read every other iteration
        if ($iter % 2 -eq 0) {
            # ── Write ──
            $body = '{"plotId":"' + $PlotId + '","deviceType":"HumiditySensor","value":35.0,"unit":"%","recordedAt":"' + [DateTime]::UtcNow.ToString("o") + '"}'
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            try {
                $wc = [System.Net.WebClient]::new()
                foreach ($k in $headers.Keys) { $wc.Headers.Add($k, $headers[$k]) }
                $wc.UploadString($writeUri, 'POST', $body) | Out-Null
                $sw.Stop()
                $WriteBag.Add($sw.ElapsedMilliseconds)
            } catch { $sw.Stop(); $WriteFail.Add($sw.ElapsedMilliseconds.ToString()) }
        } else {
            # ── Read ──
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            try {
                $wc = [System.Net.WebClient]::new()
                foreach ($k in $headers.Keys) { $wc.Headers.Add($k, $headers[$k]) }
                $wc.DownloadString($readUri) | Out-Null
                $sw.Stop()
                $ReadBag.Add($sw.ElapsedMilliseconds)
            } catch { $sw.Stop(); $ReadFail.Add($sw.ElapsedMilliseconds.ToString()) }
        }
        $iter++
    }
}

# Spin up runspaces
$pool  = [RunspaceFactory]::CreateRunspacePool(1, $Concurrency)
$pool.Open()
$jobs  = @()

for ($i = 0; $i -lt $Concurrency; $i++) {
    $ps = [PowerShell]::Create()
    $ps.RunspacePool = $pool
    $ps.AddScript($scriptBlock) | Out-Null
    $ps.AddArgument($ApiUrl)    | Out-Null
    $ps.AddArgument($PlotId)    | Out-Null
    $ps.AddArgument($jwt)       | Out-Null
    $ps.AddArgument($deadline)  | Out-Null
    $ps.AddArgument($writeLatencies) | Out-Null
    $ps.AddArgument($writeFailed)    | Out-Null
    $ps.AddArgument($readLatencies)  | Out-Null
    $ps.AddArgument($readFailed)     | Out-Null
    $jobs += @{ PS = $ps; Handle = $ps.BeginInvoke() }
}

# Progress indicator
$elapsed = 0
while ($elapsed -lt $DurationSeconds) {
    Start-Sleep -Seconds 1
    $elapsed++
    $pct = [int]($elapsed / $DurationSeconds * 100)
    $bar = "█" * ($pct / 5) + "░" * (20 - $pct / 5)
    $w   = $writeLatencies.Count
    $r   = $readLatencies.Count
    Write-Host ("`r  [{0}] {1,3}%  writes={2}  reads={3}" -f $bar, $pct, $w, $r) -NoNewline
}
Write-Host ""

# Wait for all runspaces
foreach ($j in $jobs) { $j.PS.EndInvoke($j.Handle) | Out-Null; $j.PS.Dispose() }
$pool.Close()

# ── Statistics ────────────────────────────────────────────────────────────────

$actualDuration = ([DateTime]::UtcNow - $runStart.ToUniversalTime()).TotalSeconds

function Compute-Stats {
    param([long[]]$Latencies, [int]$Fails, [string]$Label, [double]$Duration)
    $total   = $Latencies.Count + $Fails
    $success = $Latencies.Count
    $rps     = if ($Duration -gt 0) { [Math]::Round($success / $Duration, 1) } else { 0 }
    $successPct = if ($total -gt 0) { [Math]::Round($success * 100.0 / $total, 1) } else { 0 }

    if ($success -eq 0) {
        return [PSCustomObject]@{
            Label = $Label; Total = $total; Success = 0; Failed = $Fails
            RPS = 0; SuccessPct = $successPct
            Min = 0; Avg = 0; P50 = 0; P95 = 0; P99 = 0; Max = 0
        }
    }

    $sorted  = $Latencies | Sort-Object
    $avg     = [Math]::Round(($sorted | Measure-Object -Average).Average, 1)
    return [PSCustomObject]@{
        Label      = $Label
        Total      = $total
        Success    = $success
        Failed     = $Fails
        RPS        = $rps
        SuccessPct = $successPct
        Min        = $sorted[0]
        Avg        = $avg
        P50        = Get-Percentile -Sorted $sorted -Percentile 50
        P95        = Get-Percentile -Sorted $sorted -Percentile 95
        P99        = Get-Percentile -Sorted $sorted -Percentile 99
        Max        = $sorted[-1]
    }
}

$wStats = Compute-Stats -Latencies ([long[]]$writeLatencies.ToArray()) `
    -Fails $writeFailed.Count -Label "POST /api/iot/data (write)" -Duration $actualDuration

$rStats = Compute-Stats -Latencies ([long[]]$readLatencies.ToArray()) `
    -Fails $readFailed.Count  -Label "GET  /api/iot/data (read)" -Duration $actualDuration

# ── Print results ──────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "  ════════════════════ BENCHMARK RESULTS ════════════════════" -ForegroundColor Yellow
Write-Host ("  Environment:  {0}" -f $ApiUrl) -ForegroundColor DarkGray
Write-Host ("  Concurrency:  {0} workers   Duration: {1} s" -f $Concurrency, $DurationSeconds) -ForegroundColor DarkGray
Write-Host ""

$header = "  {0,-32} {1,6} {2,6} {3,6} {4,7} {5,7} {6,7} {7,7}"
$row    = "  {0,-32} {1,6} {2,6} {3,6} {4,7} {5,7} {6,7} {7,7}"
Write-Host ($header -f "Endpoint", "RPS", "OK%", "P50ms", "P95ms", "P99ms", "Avgms", "Maxms") -ForegroundColor Cyan
Write-Host "  ─────────────────────────────────────────────────────────────"

foreach ($s in @($wStats, $rStats)) {
    $colour = if ($s.SuccessPct -ge 99) { 'Green' } elseif ($s.SuccessPct -ge 95) { 'Yellow' } else { 'Red' }
    Write-Host ($row -f $s.Label, $s.RPS, "$($s.SuccessPct)%", $s.P50, $s.P95, $s.P99, $s.Avg, $s.Max) -ForegroundColor $colour
}

Write-Host ""
Write-Host ("  Total requests:  write={0}  read={1}" -f $wStats.Total, $rStats.Total) -ForegroundColor DarkGray
Write-Host ("  Errors:          write={0}  read={1}" -f $wStats.Failed, $rStats.Failed) -ForegroundColor DarkGray
Write-Host ""

# ── Save JSON ─────────────────────────────────────────────────────────────────

if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir | Out-Null }

$stamp      = Get-Date -Format 'yyyyMMdd_HHmmss'
$outputFile = Join-Path $OutputDir "benchmark_${stamp}.json"

$report = [PSCustomObject]@{
    timestamp       = $runStart.ToString("o")
    host            = $ApiUrl
    concurrency     = $Concurrency
    durationSeconds = $DurationSeconds
    write           = $wStats
    read            = $rStats
    summary = [PSCustomObject]@{
        totalRequests   = $wStats.Total + $rStats.Total
        totalErrors     = $wStats.Failed + $rStats.Failed
        overallRPS      = [Math]::Round(($wStats.RPS + $rStats.RPS), 1)
    }
}

$report | ConvertTo-Json -Depth 5 | Out-File -FilePath $outputFile -Encoding UTF8
Write-Host "  Results saved: $outputFile" -ForegroundColor DarkGray
Write-Host ""

# ── Markdown snippet (paste into README) ─────────────────────────────────────

Write-Host "  ── Markdown snippet for README (copy/paste) ────────────────" -ForegroundColor Cyan
Write-Host ""
Write-Host @"
| Endpoint                    | RPS   | P50 ms | P95 ms | P99 ms | Success |
|-----------------------------|-------|--------|--------|--------|---------|
| POST /api/iot/data (write)  | $($wStats.RPS)  | $($wStats.P50)    | $($wStats.P95)    | $($wStats.P99)    | $($wStats.SuccessPct)%   |
| GET  /api/iot/data (read)   | $($rStats.RPS)  | $($rStats.P50)    | $($rStats.P95)    | $($rStats.P99)    | $($rStats.SuccessPct)%   |

> Concurrency: $Concurrency workers | Duration: $DurationSeconds s | Host: $ApiUrl
"@ -ForegroundColor White
Write-Host ""

exit 0
