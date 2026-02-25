<#
.SYNOPSIS
    AgroSolution load benchmark -- measures API throughput and latency under concurrent load.

.DESCRIPTION
    Runs parallel HTTP requests against the two hot paths:
      POST /api/iot/data        (IoT ingestion -- write path)
      GET  /api/iot/data/{plot} (dashboard query -- read path)

    Collects per-request latency, then reports:
      RPS, success rate, Min/Avg/P50/P95/P99/Max latency.

    Results are saved to benchmark/results_<timestamp>.json so you can
    track performance across runs and paste the summary into README.md.

.PARAMETER ApiUrl
    Base URL of AgroSolution.Api.  Default: http://localhost:5034
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
    .\.scripts\Invoke-LoadTest.ps1 -DurationSeconds 30 -Concurrency 10
    .\.scripts\Invoke-LoadTest.ps1 -ApiUrl http://192.168.49.2 -Concurrency 20 -DurationSeconds 60
#>
param(
    [string] $ApiUrl          = 'http://localhost:5034',
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

# ---- Helpers ----------------------------------------------------------------

function Invoke-JsonPost {
    param([string]$Uri, [object]$Body, [hashtable]$Headers = @{})
    $json    = $Body | ConvertTo-Json -Depth 5
    $bytes   = [System.Text.Encoding]::UTF8.GetBytes($json)
    $request = [System.Net.WebRequest]::Create($Uri)
    $request.Method        = 'POST'
    $request.ContentType   = 'application/json'
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

function Get-Percentile {
    param([long[]]$Sorted, [double]$Pct)
    if ($Sorted.Count -eq 0) { return 0L }
    $index = [Math]::Ceiling($Pct / 100.0 * $Sorted.Count) - 1
    $index = [Math]::Max(0, [Math]::Min($index, $Sorted.Count - 1))
    return $Sorted[$index]
}

# ---- Banner -----------------------------------------------------------------

$runStart = Get-Date
Write-Host ""
Write-Host "  +------------------------------------------------------+" -ForegroundColor Yellow
Write-Host "  |   AgroSolution -- Load Benchmark                     |" -ForegroundColor Yellow
Write-Host ("  |   Concurrency : {0,-4}  Duration: {1} s                 |" -f $Concurrency, $DurationSeconds) -ForegroundColor Yellow
Write-Host ("  |   Api      : {0}" -f $ApiUrl) -ForegroundColor Yellow
Write-Host ("  |   Identity : {0}" -f $IdentityUrl) -ForegroundColor Yellow
Write-Host "  +------------------------------------------------------+" -ForegroundColor Yellow
Write-Host ""

# ---- Auth + Setup -----------------------------------------------------------

Write-Host "  [setup] Authenticating..." -ForegroundColor Cyan

if (-not $Email) {
    $Email = "bench_$(Get-Date -Format 'yyyyMMddHHmmss')@agro.test"
    try {
        Invoke-JsonPost -Uri "$IdentityUrl/api/auth/register" `
            -Body @{ email = $Email; password = $Password; name = "Benchmarker" } | Out-Null
        Write-Host ("         Registered: {0}" -f $Email) -ForegroundColor DarkGray
    } catch {
        Write-Host "         (register skipped -- account may already exist)" -ForegroundColor DarkGray
    }
}

$loginResp = Invoke-JsonPost -Uri "$IdentityUrl/api/auth/login" `
    -Body @{ email = $Email; password = $Password }

$jwt = $null
if ($loginResp -and $loginResp.data -and $loginResp.data.accessToken) { $jwt = $loginResp.data.accessToken }
elseif ($loginResp -and $loginResp.accessToken)                        { $jwt = $loginResp.accessToken }
elseif ($loginResp -and $loginResp.token)                              { $jwt = $loginResp.token }

if (-not $jwt) { Write-Host "FATAL: could not obtain JWT." -ForegroundColor Red; exit 1 }
Write-Host "         JWT obtained." -ForegroundColor DarkGray

$authHeader = @{ Authorization = "Bearer $jwt" }

if (-not $PlotId) {
    Write-Host "  [setup] Creating property + plot..." -ForegroundColor Cyan

    $prop = Invoke-JsonPost -Uri "$ApiUrl/api/properties" -Headers $authHeader `
        -Body @{ name = ("BenchFarm_" + (Get-Random -Maximum 9999)); location = "MT, Brasil" }

    $propertyId = $null
    if ($prop -and $prop.data) {
        if ($prop.data -is [string]) { $propertyId = $prop.data } else { $propertyId = $prop.data.id }
    } elseif ($prop -and $prop.id) { $propertyId = $prop.id }

    $plot = Invoke-JsonPost -Uri "$ApiUrl/api/plots" -Headers $authHeader `
        -Body @{ propertyId = $propertyId; name = "Talhao Bench"; cropType = "Milho"; area = 5.0 }

    if ($plot -and $plot.data) {
        if ($plot.data -is [string]) { $PlotId = $plot.data } else { $PlotId = $plot.data.id }
    } elseif ($plot -and $plot.id) { $PlotId = $plot.id }

    Write-Host ("         PlotId: {0}" -f $PlotId) -ForegroundColor DarkGray
}

# ---- Load test using Runspaces ----------------------------------------------

Write-Host ""
Write-Host ("  [bench] Starting load test ({0} workers x {1} s)..." -f $Concurrency, $DurationSeconds) -ForegroundColor Cyan
Write-Host ""

$writeLatencies = [System.Collections.Concurrent.ConcurrentBag[long]]::new()
$writeFailed    = [System.Collections.Concurrent.ConcurrentBag[string]]::new()
$readLatencies  = [System.Collections.Concurrent.ConcurrentBag[long]]::new()
$readFailed     = [System.Collections.Concurrent.ConcurrentBag[string]]::new()

$deadline = [DateTime]::UtcNow.AddSeconds($DurationSeconds)

$scriptBlock = {
    param($ApiUrl, $PlotId, $Jwt, $Deadline, $WriteBag, $WriteFail, $ReadBag, $ReadFail)

    $writeUri = $ApiUrl + "/api/iot/data"
    $from     = [DateTime]::UtcNow.AddDays(-1).ToString("yyyy-MM-dd")
    $to       = [DateTime]::UtcNow.AddDays(1).ToString("yyyy-MM-dd")
    $readUri  = $ApiUrl + "/api/iot/data/" + $PlotId + "?from=" + $from + "&to=" + $to
    $auth     = "Bearer $Jwt"
    $ct       = "application/json"
    $iter     = 0

    while ([DateTime]::UtcNow -lt $Deadline) {
        if ($iter % 2 -eq 0) {
            # Write path
            $body = '{"plotId":"' + $PlotId + '","deviceType":"HumiditySensor","value":35.0,"unit":"%"}'
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            try {
                $wc = [System.Net.WebClient]::new()
                $wc.Headers.Add("Authorization", $auth)
                $wc.Headers.Add("Content-Type", $ct)
                $wc.UploadString($writeUri, "POST", $body) | Out-Null
                $sw.Stop()
                $WriteBag.Add($sw.ElapsedMilliseconds)
            } catch { $sw.Stop(); $WriteFail.Add([string]$sw.ElapsedMilliseconds) }
        } else {
            # Read path
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            try {
                $wc = [System.Net.WebClient]::new()
                $wc.Headers.Add("Authorization", $auth)
                $wc.DownloadString($readUri) | Out-Null
                $sw.Stop()
                $ReadBag.Add($sw.ElapsedMilliseconds)
            } catch { $sw.Stop(); $ReadFail.Add([string]$sw.ElapsedMilliseconds) }
        }
        $iter++
    }
}

$pool = [RunspaceFactory]::CreateRunspacePool(1, $Concurrency)
$pool.Open()
$jobs = @()

for ($i = 0; $i -lt $Concurrency; $i++) {
    $ps = [PowerShell]::Create()
    $ps.RunspacePool = $pool
    $ps.AddScript($scriptBlock)       | Out-Null
    $ps.AddArgument($ApiUrl)          | Out-Null
    $ps.AddArgument($PlotId)          | Out-Null
    $ps.AddArgument($jwt)             | Out-Null
    $ps.AddArgument($deadline)        | Out-Null
    $ps.AddArgument($writeLatencies)  | Out-Null
    $ps.AddArgument($writeFailed)     | Out-Null
    $ps.AddArgument($readLatencies)   | Out-Null
    $ps.AddArgument($readFailed)      | Out-Null
    $jobs += @{ PS = $ps; Handle = $ps.BeginInvoke() }
}

# Progress bar
$elapsed = 0
while ($elapsed -lt $DurationSeconds) {
    Start-Sleep -Seconds 1
    $elapsed++
    $pct = [int]($elapsed * 100 / $DurationSeconds)
    $bar = "#" * [int]($pct / 5) + "-" * (20 - [int]($pct / 5))
    $w   = $writeLatencies.Count
    $r   = $readLatencies.Count
    Write-Host ("`r  [{0}] {1,3}%  writes={2}  reads={3}" -f $bar, $pct, $w, $r) -NoNewline
}
Write-Host ""

foreach ($j in $jobs) { $j.PS.EndInvoke($j.Handle) | Out-Null; $j.PS.Dispose() }
$pool.Close()

# ---- Statistics -------------------------------------------------------------

$actualDuration = ([DateTime]::UtcNow - $runStart.ToUniversalTime()).TotalSeconds

function Compute-Stats {
    param([long[]]$Latencies, [int]$Fails, [string]$Label, [double]$Duration)
    $total      = $Latencies.Count + $Fails
    $success    = $Latencies.Count
    $rps        = if ($Duration -gt 0) { [Math]::Round($success / $Duration, 1) } else { 0 }
    $successPct = if ($total -gt 0) { [Math]::Round($success * 100.0 / $total, 1) } else { 0 }

    if ($success -eq 0) {
        return [PSCustomObject]@{
            Label = $Label; Total = $total; Success = 0; Failed = $Fails
            RPS = 0; SuccessPct = $successPct
            Min = 0; Avg = 0; P50 = 0; P95 = 0; P99 = 0; Max = 0
        }
    }

    $sorted = $Latencies | Sort-Object
    $avg    = [Math]::Round(($sorted | Measure-Object -Sum).Sum / $sorted.Count, 1)
    return [PSCustomObject]@{
        Label      = $Label
        Total      = $total
        Success    = $success
        Failed     = $Fails
        RPS        = $rps
        SuccessPct = $successPct
        Min        = $sorted[0]
        Avg        = $avg
        P50        = Get-Percentile -Sorted $sorted -Pct 50
        P95        = Get-Percentile -Sorted $sorted -Pct 95
        P99        = Get-Percentile -Sorted $sorted -Pct 99
        Max        = $sorted[-1]
    }
}

$writeStats = Compute-Stats -Latencies ([long[]]$writeLatencies.ToArray()) -Fails $writeFailed.Count -Label "POST /api/iot/data" -Duration $actualDuration
$readStats  = Compute-Stats -Latencies ([long[]]$readLatencies.ToArray())  -Fails $readFailed.Count  -Label "GET  /api/iot/data/{plot}" -Duration $actualDuration

# ---- Print results ----------------------------------------------------------

Write-Host ""
Write-Host "  +-----------------------------------------------------------------------+" -ForegroundColor Green
Write-Host "  |  BENCHMARK RESULTS                                                    |" -ForegroundColor Green
Write-Host "  +-----------------------------------------------------------------------+" -ForegroundColor Green
Write-Host ("  |  Duration  : {0:F1} s    Concurrency: {1} workers" -f $actualDuration, $Concurrency) -ForegroundColor Green
Write-Host ("  |  PlotId    : {0}" -f $PlotId) -ForegroundColor Green
Write-Host "  +-----------------------------+-------+----+--------+------+------+------+" -ForegroundColor Green
Write-Host "  | Endpoint                    |  RPS  |Suc%| Min ms | P50  | P95  | P99  |" -ForegroundColor Green
Write-Host "  +-----------------------------+-------+----+--------+------+------+------+" -ForegroundColor Green

foreach ($s in @($writeStats, $readStats)) {
    Write-Host ("  | {0,-28}| {1,5} |{2,3}%| {3,6} | {4,4} | {5,4} | {6,4} |" -f `
        $s.Label, $s.RPS, $s.SuccessPct, $s.Min, $s.P50, $s.P95, $s.P99) -ForegroundColor Green
}
Write-Host "  +-----------------------------+-------+----+--------+------+------+------+" -ForegroundColor Green

# ---- Save JSON --------------------------------------------------------------

if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir | Out-Null }
$ts      = Get-Date -Format "yyyyMMdd_HHmmss"
$outFile = Join-Path $OutputDir "benchmark_$ts.json"
@{
    timestamp        = (Get-Date -Format "o")
    durationSeconds  = $actualDuration
    concurrency      = $Concurrency
    apiUrl           = $ApiUrl
    plotId           = $PlotId
    write            = $writeStats
    read             = $readStats
} | ConvertTo-Json -Depth 5 | Out-File -FilePath $outFile -Encoding UTF8
Write-Host ""
Write-Host ("  Results saved to: {0}" -f $outFile) -ForegroundColor DarkGray

# ---- Markdown snippet -------------------------------------------------------

Write-Host ""
Write-Host "  --- Markdown snippet for README.md ---" -ForegroundColor DarkGray
Write-Host ""
Write-Host ("| {0,-32} | {1,6} RPS | {2,5} ms P50 | {3,5} ms P99 | {4}% OK |" -f `
    $writeStats.Label, $writeStats.RPS, $writeStats.P50, $writeStats.P99, $writeStats.SuccessPct)
Write-Host ("| {0,-32} | {1,6} RPS | {2,5} ms P50 | {3,5} ms P99 | {4}% OK |" -f `
    $readStats.Label, $readStats.RPS, $readStats.P50, $readStats.P99, $readStats.SuccessPct)
Write-Host ""
