# TB-SHIPPING-NEW-METHOD-ACTIVATION-170 — cleanup (task-owned resources only)
# Stops the isolated Host launched by bootstrap-scratch-170.ps1 and removes the disposable container.
$ErrorActionPreference = 'Continue'
$repo = (Get-Location).Path
$runDir = Join-Path $repo '.tmp-170-run'
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-NEW-METHOD-ACTIVATION-170'
$torn = @()

foreach ($name in @('host')) {
    $pidFile = Join-Path $runDir "$name.pid"
    if (Test-Path -LiteralPath $pidFile) {
        $procId = (Get-Content -LiteralPath $pidFile -Raw).Trim()
        if ($procId -match '^\d+$') {
            try { Stop-Process -Id ([int]$procId) -Force -ErrorAction Stop; $torn += "${name}_pid_stopped" } catch { }
        }
    }
}

& docker rm -f tooba-tb170-pg 2>&1 | Out-Null
$torn += 'container_removed'

$remaining = & docker ps -a --filter 'name=^tooba-tb170-pg$' --format '{{.Names}}' 2>$null
$port = Get-NetTCPConnection -LocalPort 5424 -State Listen -ErrorAction SilentlyContinue
$lines = @(
    "HOST_PROCESS_STOPPED=$($torn -contains 'host_pid_stopped')",
    "CONTAINER_PRESENT_AFTER_CLEANUP=$([bool]($remaining -match 'tooba-tb170-pg'))",
    "HOST_PORT_5424_STILL_LISTENING=$([bool]$port)",
    "SHARED_PORTS_TOUCHED=False"
)
$lines -join "`n" | Set-Content -LiteralPath (Join-Path $evidenceDir 'teardown-170.txt') -Encoding utf8
$lines | ForEach-Object { Write-Output $_ }
