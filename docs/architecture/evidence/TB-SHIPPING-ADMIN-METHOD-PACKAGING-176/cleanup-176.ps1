# TB-SHIPPING-ADMIN-METHOD-PACKAGING-176 — scratch teardown.
# Stops ONLY the task-owned Host process and removes the task-owned disposable PostgreSQL container.
$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$runDir = Join-Path $repo '.tmp-176-run'
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ADMIN-METHOD-PACKAGING-176'
$container = 'tooba-tb176-pg'

foreach ($gate in @('true', 'false')) {
    $pidFile = Join-Path $runDir "host-$gate.pid"
    if (Test-Path -LiteralPath $pidFile) {
        $hostPid = Get-Content -LiteralPath $pidFile
        if ($hostPid) { Stop-Process -Id $hostPid -Force -ErrorAction SilentlyContinue }
    }
}

& docker rm -f $container 2>$null | Out-Null

Start-Sleep -Seconds 2
$ports = @(5430, 55496, 3125)
$listening = @()
foreach ($p in $ports) {
    $conns = Get-NetTCPConnection -LocalPort $p -State Listen -ErrorAction SilentlyContinue
    if ($conns) { $listening += $p }
}
$containerLeft = (& docker ps -a --filter "name=^$container$" --format '{{.Names}}' 2>$null | Out-String).Trim()

$lines = @(
    'TB-176 SCRATCH TEARDOWN',
    "HOST_PROCESSES_STOPPED=$(if (Test-Path (Join-Path $runDir 'host-true.pid')) { 'true' } else { 'unknown' })",
    "CONTAINER=$container",
    "CONTAINER_REMAINING=$(if ($containerLeft) { $containerLeft } else { 'none' })",
    "PORTS_STILL_LISTENING=$(if ($listening.Count -gt 0) { $listening -join ',' } else { 'none' })",
    'SHARED_PORTS_3000_5088_5432_TOUCHED=false'
)
$lines -join "`n" | Set-Content -LiteralPath (Join-Path $evidenceDir 'cleanup-176.txt') -Encoding utf8
$lines | ForEach-Object { Write-Output $_ }
