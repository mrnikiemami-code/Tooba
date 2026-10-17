# TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-ACCEPTANCE-168 — teardown of the task-owned scratch environment.
# Removes ONLY task-owned resources: the isolated Host (5422), the isolated Next frontend (3117) and the
# disposable PostgreSQL container tooba-tb168-pg (55487). Shared :3000 / :5088 / :5432 are never touched.
param(
    [switch]$VerifyOnly
)

$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent $PSScriptRoot
$repo = (Resolve-Path (Join-Path $root '..\..\..')).Path
$runDir = Join-Path $repo '.tmp-168-run'
$out = Join-Path $PSScriptRoot 'cleanup-168.txt'
$lines = [System.Collections.Generic.List[string]]::new()

function Log([string]$text) {
    $lines.Add($text)
    Write-Host $text
}

function Stop-PidFile([string]$name) {
    $file = Join-Path $runDir ($name + '.pid')
    if (-not (Test-Path $file)) { Log "$name pid file: absent"; return }
    $raw = (Get-Content $file -Raw).Trim()
    $procId = 0
    if (-not [int]::TryParse($raw, [ref]$procId)) { Log "$name pid file: unparsable ($raw)"; return }
    $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
    if ($null -eq $proc) { Log "$name pid ${procId}: already stopped"; return }
    try {
        Stop-Process -Id $procId -Force -ErrorAction Stop
        Log "$name pid ${procId}: stopped"
    } catch {
        Log "$name pid ${procId}: stop failed ($($_.Exception.Message))"
    }
}

function Port-Owner([int]$port) {
    $conn = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    if ($null -eq $conn) { return 'closed' }
    return (($conn | Select-Object -ExpandProperty OwningProcess -Unique) -join ',')
}

Log ("capturedUtc=" + (Get-Date).ToUniversalTime().ToString('o'))

if (-not $VerifyOnly) {
    Stop-PidFile 'host'
    Stop-PidFile 'frontend'

    # The launchers may have spawned child processes (dotnet build host / next start).
    foreach ($port in 5422, 3117) {
        Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty OwningProcess -Unique |
            ForEach-Object {
                try { Stop-Process -Id $_ -Force -ErrorAction Stop; Log "port $port pid $($_): stopped" }
                catch { Log "port $port pid $($_): stop failed ($($_.Exception.Message))" }
            }
    }

    # Disposable PostgreSQL container (task-owned name only).
    $docker = Get-Command docker -ErrorAction SilentlyContinue
    if ($null -eq $docker) {
        Log 'docker: not found on PATH'
    } else {
        $exists = & docker ps -a --filter 'name=^tooba-tb168-pg$' --format '{{.Names}}' 2>$null
        if ([string]::IsNullOrWhiteSpace($exists)) {
            Log 'container tooba-tb168-pg: not present'
        } else {
            & docker rm -f tooba-tb168-pg 2>&1 | ForEach-Object { Log ("docker rm -f: " + $_) }
            Log 'container tooba-tb168-pg: removed'
        }
    }

    if (Test-Path $runDir) {
        Remove-Item -Path $runDir -Recurse -Force -ErrorAction SilentlyContinue
        Log ("run dir removed: " + $runDir + " exists=" + (Test-Path $runDir))
    } else {
        Log ("run dir: already absent (" + $runDir + ")")
    }
}

Start-Sleep -Seconds 2
Log "port 5422 (task host)  = $(Port-Owner 5422)"
Log "port 3117 (task web)   = $(Port-Owner 3117)"
Log "port 55487 (task pg)   = $(Port-Owner 55487)"
Log "port 3000 (shared web) = $(Port-Owner 3000)"
Log "port 5088 (shared host)= $(Port-Owner 5088)"
Log "port 5432 (shared pg)  = $(Port-Owner 5432)"

$scratchClosed =
    (Port-Owner 5422) -eq 'closed' -and
    (Port-Owner 3117) -eq 'closed' -and
    (Port-Owner 55487) -eq 'closed'
$sharedIntact = (Port-Owner 3000) -ne 'closed' -and (Port-Owner 5088) -ne 'closed' -and (Port-Owner 5432) -ne 'closed'

Log "scratch_ports_closed=$scratchClosed"
Log "shared_ports_intact=$sharedIntact"

$lines | Set-Content -Path $out -Encoding UTF8
Write-Host ("wrote " + $out)

if (-not ($scratchClosed -and $sharedIntact)) { exit 2 }
exit 0
