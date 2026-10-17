# TB-STOREFRONT-PERF-SCALE-CLOSURE-180 phase 2 — legs, seed, legs (same scratch DB, same published binary).
# Adds a hard measurement-validity assertion: the listener on the task port must be the exact started PID.
$ErrorActionPreference = 'Continue'

$Container = 'tooba-tb180-pg'
$Database = 'tooba_180_scratch'
$User = 'scratch180'
$HostPort = 5451
$SeedCapSeconds = 480
$Requests = 3
$Locale = 'fa'

$repo = (Get-Location).Path
$runDir = Join-Path $repo '.tmp-180-run'
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-STOREFRONT-PERF-SCALE-CLOSURE-180'
$password = $env:TOOBA_180_SCRATCH_PASSWORD
$hostOut = Join-Path $runDir 'host'
$progress = Join-Path $evidenceDir 'progress-180.log'
if (-not $password) { throw 'TOOBA_180_SCRATCH_PASSWORD required' }
function Log([string]$m) { Add-Content -LiteralPath $progress -Value "$(Get-Date -Format 'HH:mm:ss') $m" -Encoding utf8 }
function Psql([string]$sql) { (& docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -tAc $sql 2>$null | Out-String).Trim() }
function RowCounts {
    Psql "select 'products='||(select count(*) from catalog.products)||' categories='||(select count(*) from catalog.categories)||' variants='||(select count(*) from catalog.variants)||' offers='||(select count(*) from offer.offers)||' prices='||(select count(*) from pricing.prices)||' stock='||(select count(*) from inventory.stock_positions)||' parties='||(select count(*) from party.parties)||' texts='||(select count(*) from catalog.localized_texts)"
}
function Wait-PortFree([int]$port) {
    for ($i = 0; $i -lt 40; $i++) {
        if (-not (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)) { return $true }
        Start-Sleep -Milliseconds 500
    }
    return $false
}

$env:ASPNETCORE_URLS = "http://127.0.0.1:$HostPort"
$env:DOTNET_ENVIRONMENT = 'Development'

function Start-Host([string]$leg, [string]$tag, [string]$efLevel) {
    if (-not (Wait-PortFree $HostPort)) { throw "port $HostPort not free before $tag/$leg" }
    $stdout = Join-Path $runDir "p3-$tag-$leg-stdout.jsonl"
    $stderr = Join-Path $runDir "p3-$tag-$leg-stderr.txt"
    Remove-Item -Force -ErrorAction SilentlyContinue $stdout, $stderr
    if ($efLevel) { Set-Item -Path 'Env:Logging__LogLevel__Microsoft.EntityFrameworkCore' -Value $efLevel }
    else { Remove-Item -Path 'Env:Logging__LogLevel__Microsoft.EntityFrameworkCore' -ErrorAction SilentlyContinue }
    $proc = Start-Process -FilePath (Join-Path $hostOut 'Tooba.Host.exe') -WorkingDirectory $hostOut -PassThru -NoNewWindow `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $healthy = $false
    $started = Get-Date
    while (((Get-Date) - $started).TotalSeconds -lt 240) {
        if ($proc.HasExited) { break }
        try {
            if ((Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/health" -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200) { $healthy = $true; break }
        } catch { }
        Start-Sleep -Milliseconds 700
    }
    if (-not $healthy) { try { Stop-Process -Id $proc.Id -Force } catch { }; throw "host $tag/$leg not healthy" }
    $owner = (Get-NetTCPConnection -LocalPort $HostPort -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1).OwningProcess
    if ($owner -ne $proc.Id) { try { Stop-Process -Id $proc.Id -Force } catch { }; throw "measurement invalid: listener pid $owner != started pid $($proc.Id) for $tag/$leg" }
    Log "host up $tag/$leg pid=$($proc.Id) listener=$owner"
    return [pscustomobject]@{ proc = $proc; stdout = $stdout; listenerPid = $owner }
}

function Stop-Host($h, [string]$label) {
    try { Stop-Process -Id $h.proc.Id -Force } catch { }
    try { $h.proc.WaitForExit(20000) | Out-Null } catch { }
    if (-not (Wait-PortFree $HostPort)) { Log "WARN port still busy after $label" }
    Start-Sleep -Milliseconds 300
}

function Measure-Leg([string]$leg, [string]$tag) {
    $h = Start-Host $leg $tag $(if ($leg -eq 'diagnostic') { 'Information' } else { $null })
    $uri = "http://127.0.0.1:$HostPort/v1/storefront/home?locale=$Locale"
    $headers = @{ Host = 'alpha.localhost' }

    $wS = [DateTime]::UtcNow
    $wSw = [System.Diagnostics.Stopwatch]::StartNew()
    $warm = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 180 -Headers $headers
    $wSw.Stop()
    $wE = [DateTime]::UtcNow
    $warmBody = $warm.Content
    $warmBody | Set-Content -LiteralPath (Join-Path $evidenceDir "home-warmup-180-$tag-$leg.json") -Encoding utf8

    $samples = @()
    for ($i = 1; $i -le $Requests; $i++) {
        $s = [DateTime]::UtcNow
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $resp = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 180 -Headers $headers
        $sw.Stop()
        $e = [DateTime]::UtcNow
        $body = $resp.Content
        $sha = (Get-FileHash -InputStream ([System.IO.MemoryStream]::new([System.Text.Encoding]::UTF8.GetBytes($body))) -Algorithm SHA256).Hash
        if ($i -eq 1) { Set-Content -LiteralPath (Join-Path $evidenceDir "home-body-180-$tag-$leg.json") -Value $body -Encoding utf8 }
        $samples += [pscustomobject]@{
            request = $i; status = $resp.StatusCode
            wallMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 1)
            payloadBytes = [System.Text.Encoding]::UTF8.GetByteCount($body)
            slugCount = ([regex]::Matches($body, '"slug"')).Count
            productIdCount = ([regex]::Matches($body, '"productId"')).Count
            bodySha256 = $sha
            startUtc = $s.ToString('o'); endUtc = $e.ToString('o')
        }
    }
    Stop-Host $h "$tag/$leg"

    $text = if (Test-Path $h.stdout) { [System.IO.File]::ReadAllText($h.stdout) } else { '' }
    $lines = @(($text -split "`n") | Where-Object { $_.Trim().Length -gt 0 })
    $cmd = @()
    foreach ($line in $lines) {
        if ($line -notmatch 'Database\.Command') { continue }
        try {
            $o = $line | ConvertFrom-Json
            if ($o.Category -notmatch '^Microsoft\.EntityFrameworkCore\.Database\.Command') { continue }
            $trace = ''
            foreach ($scope in $o.Scopes) { if ($scope.Message -match 'TraceId:([0-9a-f]+)') { $trace = $Matches[1] } }
            $cmd += [pscustomobject]@{ ts = [DateTimeOffset]::Parse($o.Timestamp).UtcDateTime; trace = $trace }
        } catch { }
    }
    $windows = @()
    $windows += [pscustomobject]@{ label = 'warmup'; commands = @($cmd | Where-Object { $_.ts -ge $wS -and $_.ts -le $wE }).Count; wallMs = [math]::Round($wSw.Elapsed.TotalMilliseconds, 1) }
    foreach ($s in $samples) {
        $ws = [DateTimeOffset]::Parse($s.startUtc).UtcDateTime
        $we = [DateTimeOffset]::Parse($s.endUtc).UtcDateTime
        $windows += [pscustomobject]@{ label = "req$($s.request)"; commands = @($cmd | Where-Object { $_.ts -ge $ws -and $_.ts -le $we }).Count; wallMs = $s.wallMs }
    }
    $groups = $cmd | Group-Object trace | Sort-Object Count -Descending | Select-Object -First 10 |
        ForEach-Object { [pscustomobject]@{ trace = $_.Name; commands = $_.Count } }

    return [pscustomobject]@{
        tag = $tag; leg = $leg
        efLevel = if ($leg -eq 'diagnostic') { 'Information' } else { '<committed default>' }
        hostPid = $h.proc.Id; listenerPid = $h.listenerPid
        warmup = [pscustomobject]@{ status = $warm.StatusCode; wallMs = [math]::Round($wSw.Elapsed.TotalMilliseconds, 1); payloadBytes = [System.Text.Encoding]::UTF8.GetByteCount($warmBody); slugCount = ([regex]::Matches($warmBody, '"slug"')).Count }
        requests = $Requests
        samples = $samples
        requestScopedCommands = $windows
        totalCommandLines = $cmd.Count
        topTraceCommandGroups = $groups
        stdoutBytes = [System.Text.Encoding]::UTF8.GetByteCount($text)
        stdoutLines = $lines.Count
        efCategoryLines = @($lines | Where-Object { $_ -match '"Category":"Microsoft\.EntityFrameworkCore' }).Count
        errorOrCriticalLines = @($lines | Where-Object { $_ -match '"LogLevel":"(Error|Critical)"' }).Count
    }
}

function Dump-Snapshot([string]$tag) {
    $tables = @('catalog.products', 'catalog.variants', 'offer.offers', 'pricing.prices', 'inventory.stock_positions', 'party.parties', 'catalog.localized_texts', 'catalog.categories')
    $sb = New-Object System.Text.StringBuilder
    foreach ($t in $tables) {
        $rows = & docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -tAc "select row_to_json(t) from (select * from $t) t" 2>$null
        foreach ($r in $rows) { if ($r.Trim()) { [void]$sb.AppendLine("$t|" + $r.Trim()) } }
    }
    Set-Content -LiteralPath (Join-Path $evidenceDir "db-snapshot-180-$tag.txt") -Value $sb.ToString() -Encoding utf8
    Log "snapshot $tag written"
}

# --- SIZE A (re-measured with listener-PID validity assertion) --------------------------------
$rowsA = RowCounts
Log "sizeA rows: $rowsA"
Dump-Snapshot 'size-a'
$legADiag = Measure-Leg 'diagnostic' 'size-a'
$legADefault = Measure-Leg 'default' 'size-a'
Log ("sizeA medians diag=" + (($legADiag.samples | Measure-Object wallMs -Average).Average) + " default=" + (($legADefault.samples | Measure-Object wallMs -Average).Average))

Set-Content -LiteralPath (Join-Path $evidenceDir 'stage-a.json') -Value ((@{ rows = $rowsA; diagnostic = $legADiag; default = $legADefault } | ConvertTo-Json -Depth 10)) -Encoding utf8
Write-Output 'STAGE_A_SAVED'
# --- SIZE B: additive owner demo seeder with actor polling + hard cap ------------------------
$seedHost = Start-Host 'default' 'seed' $null
$actor = ''
for ($i = 0; $i -lt 60; $i++) {
    $actor = Psql "select user_id from identity.login_identifiers where normalized_value='admin-actor@tooba.local' and kind='Email' limit 1"
    if ($actor -match '^[0-9a-fA-F-]{36}$') { break }
    Start-Sleep -Seconds 2
}
if ($actor -notmatch '^[0-9a-fA-F-]{36}$') { Stop-Host $seedHost 'seed'; Log 'FATAL dev actor missing'; throw 'dev admin actor missing' }
Log "dev actor resolved after polling"

$seedResp = Join-Path $runDir 'p3-seed-response.json'
$seedStatusFile = Join-Path $runDir 'p3-seed-status.txt'
Remove-Item -Force -ErrorAction SilentlyContinue $seedResp, $seedStatusFile
$curl = Start-Process -FilePath 'curl.exe' -ArgumentList @(
    '--max-time', "$SeedCapSeconds", '-s', '-o', $seedResp, '-w', '%{http_code}', '-X', 'POST',
    '-H', 'Host: alpha.localhost', '-H', "X-Tooba-Dev-Actor-User-Id: $actor",
    "http://127.0.0.1:$HostPort/v1/admin/catalog/demo/seed-only") -PassThru -NoNewWindow -RedirectStandardOutput $seedStatusFile
Log "seed started curl=$($curl.Id) cap=${SeedCapSeconds}s"
$seedStart = Get-Date
$lastRows = ''
while (-not $curl.HasExited) {
    if (((Get-Date) - $seedStart).TotalSeconds -gt ($SeedCapSeconds + 15)) { break }
    $rows = RowCounts
    if ($rows -ne $lastRows) { Log "seed progress: $rows"; $lastRows = $rows }
    Start-Sleep -Seconds 12
}
$hardKill = $false
if (-not $curl.HasExited) { $hardKill = $true; try { Stop-Process -Id $curl.Id -Force } catch { } }
Start-Sleep -Seconds 2
$seedElapsed = [math]::Round(((Get-Date) - $seedStart).TotalSeconds, 1)
$seedBody = if (Test-Path $seedResp) { Get-Content $seedResp -Raw } else { '' }
$seedStatus = if (Test-Path $seedStatusFile) { (Get-Content $seedStatusFile -Raw).Trim() } else { '' }
Set-Content -LiteralPath (Join-Path $evidenceDir 'seed-response-180.json') -Value $seedBody -Encoding utf8
$rowsB = RowCounts
$productsB = [int]((Psql "select count(*) from catalog.products"))
Log "seed done elapsed=${seedElapsed}s hardKill=$hardKill status=$seedStatus rows=$rowsB"
Stop-Host $seedHost 'seed'

# --- SIZE B measurement ----------------------------------------------------------------------
$legBDiag = $null; $legBDefault = $null
if ($productsB -ge 20) {
    Dump-Snapshot 'size-b'
    $legBDiag = Measure-Leg 'diagnostic' 'size-b'
    $legBDefault = Measure-Leg 'default' 'size-b'
    Log ("sizeB medians diag=" + (($legBDiag.samples | Measure-Object wallMs -Average).Average) + " default=" + (($legBDefault.samples | Measure-Object wallMs -Average).Average))
} else {
    Log "SKIP sizeB measurement: products=$productsB (<20)"
    Dump-Snapshot 'size-b'
}

$out = [pscustomobject]@{
    baseSha = (& git rev-parse HEAD)
    route = "/v1/storefront/home?locale=$Locale"
    hostHeader = 'alpha.localhost'
    scratch = [pscustomobject]@{ container = $Container; database = $Database; hostPort = $HostPort }
    sizeA = [pscustomobject]@{ rows = $rowsA; diagnostic = $legADiag; default = $legADefault }
    seed = [pscustomobject]@{ status = $seedStatus; elapsedSeconds = $seedElapsed; boundedCapSeconds = $SeedCapSeconds; hardKilledAtCap = $hardKill; rowsAfter = $rowsB }
    sizeB = [pscustomobject]@{ rows = $rowsB; diagnostic = $legBDiag; default = $legBDefault }
    sharedPortsTouched = $false
    credentialsInEvidence = $false
}
$json = $out | ConvertTo-Json -Depth 10
Set-Content -LiteralPath (Join-Path $evidenceDir 'measurement-180.json') -Value $json -Encoding utf8
Log 'measurement written'
$json

