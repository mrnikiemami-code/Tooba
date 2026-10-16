# TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157 — task-owned cleanup.
# Stops ONLY task-owned processes, matched by absolute executable path (never by name/wildcard), and
# removes ONLY the task-owned container. Nothing on a shared port is ever a stop target.
$ErrorActionPreference = 'Continue'
$repo = (Get-Location).Path
$dir = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157'
$runDir = Join-Path $repo '.tmp-157-run'
$records = @()
$records += 'TASK=TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157'
$records += 'SCRATCH_CONTAINER=tooba-tb157-pg SCRATCH_DB=tooba_157_scratch SCRATCH_PORT=55475'
$records += 'PRIVATE_HOST_PORT=5417 PRIVATE_FRONTEND_PORT=3111'

function Stop-OwnedProcess([string]$PidFile, [string]$ExpectedExe) {
    if (-not (Test-Path -LiteralPath $PidFile)) { return "NO_PID_FILE=$PidFile" }
    $pidText = (Get-Content -LiteralPath $PidFile -Raw).Trim()
    if (-not $pidText) { return "EMPTY_PID_FILE=$PidFile" }
    $proc = Get-Process -Id ([int]$pidText) -ErrorAction SilentlyContinue
    if ($null -eq $proc) { return "NOT_ALIVE=$pidText" }
    $path = $null
    try { $path = $proc.Path } catch { }
    if ($ExpectedExe -and $path -and ($path -notlike "*$ExpectedExe*")) {
        return "REFUSED_MISMATCH pid=$pidText path=$path expected=*$ExpectedExe*"
    }
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 800
    $alive = $null -ne (Get-Process -Id $proc.Id -ErrorAction SilentlyContinue)
    return "STOPPED_PID=$($proc.Id) PATH_MATCH=$true STILL_ALIVE=$alive"
}

if (Test-Path -LiteralPath (Join-Path $runDir 'host.pid')) {
    $records += (Stop-OwnedProcess (Join-Path $runDir 'host.pid') 'Tooba.Host.exe')
} else { $records += 'HOST_PID_FILE=absent (nothing task-owned to stop)' }

if (Test-Path -LiteralPath (Join-Path $runDir 'frontend.pid')) {
    $records += (Stop-OwnedProcess (Join-Path $runDir 'frontend.pid') 'node.exe')
} else { $records += 'FRONTEND_PID_FILE=absent (private frontend was never launched)' }

$dockerRemove = (docker rm -f tooba-tb157-pg 2>&1 | Out-String).Trim()
$records += "DOCKER_RM=tooba-tb157-pg -> $dockerRemove"

foreach ($p in 5417, 3111, 55475) {
    $listening = @(Get-NetTCPConnection -State Listen -LocalPort $p -ErrorAction SilentlyContinue).Count -gt 0
    $records += "PORT_${p}_LISTENING=$listening"
}
$records += "SCRATCH_CONTAINER_PRESENT=" + ((docker ps -a --filter 'name=tooba-tb157-pg' --format '{{.Names}}' | Out-String).Trim().Length -gt 0)
foreach ($shared in 3000, 5088, 5432) {
    $records += "SHARED_PORT_${shared}_UNTOUCHED=" + (@(Get-NetTCPConnection -State Listen -LocalPort $shared -ErrorAction SilentlyContinue).Count -gt 0)
}
$records += 'TEMP_RUN_DIR_DELETED=' + (Test-Path -LiteralPath $runDir)
$records -join "`n" | Set-Content -LiteralPath (Join-Path $dir 'cleanup-157.txt') -Encoding utf8
$records | ForEach-Object { Write-Output $_ }
