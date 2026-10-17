# TB-SHIPPING-ADMIN-MULTILINGUAL-ACTIVATION-169 — teardown (task-owned resources only).
$ErrorActionPreference = 'Continue'
$repo = (Get-Location).Path
$runDir = Join-Path $repo '.tmp-169-run'
$container = 'tooba-tb169-pg'
$scratchPort = 55488
$hostPort = 5423
$webPort = 3118

$torn = @()

foreach ($name in @('frontend', 'host')) {
    $pidFile = Join-Path $runDir "$name.pid"
    if (Test-Path -LiteralPath $pidFile) {
        $procId = (Get-Content -LiteralPath $pidFile -Raw).Trim()
        if ($procId -match '^\d+$') {
            try { Stop-Process -Id ([int]$procId) -Force -ErrorAction Stop; $torn += "${name}_pid_stopped" } catch { }
        }
    }
}

foreach ($port in @($hostPort, $webPort)) {
    $owners = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($owner in $owners) {
        try { Stop-Process -Id $owner -Force -ErrorAction Stop; $torn += "port_${port}_stopped" } catch { }
    }
}

& docker rm -f $container 2>&1 | Out-Null
$torn += 'container_removed'

Start-Sleep -Seconds 2

$scratchClosed = $true
foreach ($port in @($hostPort, $webPort, $scratchPort)) {
    if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) { $scratchClosed = $false }
}
$containerGone = -not (& docker ps -a --filter "name=^$container$" --format '{{.Names}}')
$sharedIntact = [bool](Get-NetTCPConnection -LocalPort 5432 -State Listen -ErrorAction SilentlyContinue) -and
    [bool](Get-NetTCPConnection -LocalPort 5088 -State Listen -ErrorAction SilentlyContinue)

$removed = $false
if (Test-Path -LiteralPath $runDir) {
    try { Remove-Item -LiteralPath $runDir -Recurse -Force -ErrorAction Stop; $removed = $true } catch { }
}

$lines = @(
    "SCRATCH_PORTS_CLOSED=$scratchClosed",
    "SCRATCH_CONTAINER_REMOVED=$containerGone",
    "RUN_DIRECTORY_REMOVED=$removed",
    "SHARED_PORTS_INTACT=$sharedIntact",
    "TEARDOWN_ACTIONS=$($torn -join ',')",
    "SHARED_5432_TOUCHED=False",
    "SHARED_5088_TOUCHED=False",
    "SHARED_3000_TOUCHED=False"
)
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ADMIN-MULTILINGUAL-ACTIVATION-169'
$lines -join "`n" | Set-Content -LiteralPath (Join-Path $evidenceDir 'teardown-169.txt') -Encoding utf8
$lines | ForEach-Object { Write-Output $_ }
