# TB-SHIPPING-ONE-LEVEL-TABS-133 — cleanup: stop ONLY the recorded task-owned PIDs after absolute-path
# verification, remove ONLY task-owned scratch container/temp dirs/secret, then prove ports released.
$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$runDir = Join-Path $repo '.tmp-133-run'
$hostExe = (Resolve-Path -LiteralPath (Join-Path $repo '.tmp-133-host-out\Tooba.Host.exe')).Path
$nodeExe = (Get-Content -LiteralPath (Join-Path $runDir 'frontend-exe.txt') -Raw).Trim()

$result = New-Object System.Collections.Generic.List[string]
$result.Add("HOST_EXPECTED_EXE=$hostExe")
$result.Add("FRONTEND_EXPECTED_EXE=$nodeExe")

# --- stop isolated frontend (recorded PID, verified path) ---
$pidF = [int](Get-Content -LiteralPath (Join-Path $runDir 'frontend.pid') -Raw).Trim()
$fp = Get-Process -Id $pidF -ErrorAction SilentlyContinue
if ($fp -and $fp.Path -eq $nodeExe) {
    Stop-Process -Id $pidF -Force
    $result.Add("FRONTEND_STOPPED_BY_RECORDED_PID=$pidF")
} else {
    $result.Add("FRONTEND_NOT_STOPPED pid=$pidF present=$([bool]$fp) path=$($fp.Path)")
}
Start-Sleep -Seconds 3

# --- stop isolated Host (recorded PID, verified path) ---
$pidH = [int](Get-Content -LiteralPath (Join-Path $runDir 'host.pid') -Raw).Trim()
$hp = Get-Process -Id $pidH -ErrorAction SilentlyContinue
if ($hp -and $hp.Path -eq $hostExe) {
    Stop-Process -Id $pidH -Force
    $result.Add("HOST_STOPPED_BY_RECORDED_PID=$pidH")
} else {
    $result.Add("HOST_NOT_STOPPED pid=$pidH present=$([bool]$hp) path=$($hp.Path)")
}
Start-Sleep -Seconds 4

# --- remove ONLY the task-owned scratch container (no volume was ever created) ---
$container = 'tooba-tb133-pg'
$existing = docker ps -a --filter "name=^/$container$" --format '{{.Names}}'
if ($existing -eq $container) {
    docker rm -f $container | Out-Null
    $result.Add("SCRATCH_CONTAINER_REMOVED=$container")
} else {
    $result.Add("SCRATCH_CONTAINER_ABSENT=$container")
}
$volumes = docker volume ls --filter "name=tb133" --format '{{.Name}}'
$result.Add("SCRATCH_VOLUMES=$(if ($volumes) { ($volumes -join ',') } else { 'none' })")

# --- remove ONLY task-owned temp dirs and the uncommitted scratch secret ---
foreach ($dir in @('.tmp-133-host-out', '.tmp-133-run')) {
    $full = Join-Path $repo $dir
    if (Test-Path -LiteralPath $full) { Remove-Item -LiteralPath $full -Recurse -Force; $result.Add("REMOVED=$dir") }
}
$pwFile = Join-Path $repo '.tmp-133-scratch-pw.txt'
if (Test-Path -LiteralPath $pwFile) { Remove-Item -LiteralPath $pwFile -Force; $result.Add('REMOVED=.tmp-133-scratch-pw.txt') }
$pubLog = Join-Path $repo '.tmp-133-publish.log'
if (Test-Path -LiteralPath $pubLog) { Remove-Item -LiteralPath $pubLog -Force; $result.Add('REMOVED=.tmp-133-publish.log') }
$ttyLog = Join-Path $repo '.tmp-133-host-launch-tty.txt'
if (Test-Path -LiteralPath $ttyLog) { Remove-Item -LiteralPath $ttyLog -Force; $result.Add('REMOVED=.tmp-133-host-launch-tty.txt') }

Start-Sleep -Seconds 3
foreach ($port in @(5403, 3103, 55456)) {
    $count = @(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue).Count
    $result.Add("LISTENERS_ON_$port" + "_AFTER_CLEANUP=$count")
}
$result.Add("SHARED_5088_LISTENERS=$(@(Get-NetTCPConnection -State Listen -LocalPort 5088 -ErrorAction SilentlyContinue).Count)")
$result.Add("SHARED_3000_LISTENERS=$(@(Get-NetTCPConnection -State Listen -LocalPort 3000 -ErrorAction SilentlyContinue).Count)")
$result.Add("SHARED_CONTAINERS=$((docker ps --format '{{.Names}}' | Sort-Object) -join ',')")
$result.Add("SHARED_ALPHA_DB_PRESENT=$((docker ps --format '{{.Names}}' | Where-Object { $_ -eq 'postgres-db' }).Count)")

$result -join "`n" | Set-Content -LiteralPath (Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-133\cleanup-133.txt') -Encoding utf8
$result | ForEach-Object { Write-Output $_ }
