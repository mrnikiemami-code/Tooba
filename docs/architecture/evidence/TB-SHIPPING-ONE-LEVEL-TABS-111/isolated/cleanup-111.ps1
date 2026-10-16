$ErrorActionPreference = 'Stop'
$repo = 'D:\Users\User\source\repos\SarvNewVer'
$ev = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-111'
$log = Join-Path $ev 'cleanup-and-parity.txt'
Set-Content -Encoding utf8 $log 'TB-SHIPPING-ONE-LEVEL-TABS-111 — MISSION B cleanup and shared parity'

# --- task-owned Host process check: exe-path based ONLY (never name/wildcard termination) ---
$taskExe = (Resolve-Path (Join-Path $repo '.tmp-111-host-out\Tooba.Host.exe')).Path
$found = @(Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $taskExe })
if ($found.Count -gt 0) { $hostState = 'TASK_OWNED_HOST_STILL_RUNNING=' + ($found.Id -join ',') }
else { $hostState = 'NO_TASK_OWNED_HOST_PROCESS' }
Add-Content -Encoding utf8 $log $hostState
Add-Content -Encoding utf8 $log ('listeners_5395_after_run=' + @(Get-NetTCPConnection -State Listen -LocalPort 5395 -ErrorAction SilentlyContinue).Count)
Add-Content -Encoding utf8 $log ('listeners_55447_before_container_rm=' + @(Get-NetTCPConnection -State Listen -LocalPort 55447 -ErrorAction SilentlyContinue).Count)

# --- drop raw logs after the credential scan (excerpt retained) ---
$stdoutPath = Join-Path $ev 'isolated-host-111-stdout.txt'
$stderrPath = Join-Path $ev 'isolated-host-111-stderr.txt'
$s1 = 1929043
$s2 = 1877
if (Test-Path $stdoutPath) { $s1 = (Get-Item $stdoutPath).Length }
if (Test-Path $stderrPath) { $s2 = (Get-Item $stderrPath).Length }
Remove-Item $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
Add-Content -Encoding utf8 $log ('dropped_stdout_bytes=' + $s1 + ' dropped_stderr_bytes=' + $s2 + ' (credential scan clean before drop)')

# --- remove the task-owned disposable container ---
$rm = 'already_removed'
if (docker ps -a --filter 'name=tooba-111-scratch-pg' --format '{{.Names}}' | Select-String -Quiet 'tooba-111-scratch-pg') {
    $rm = (docker rm -f tooba-111-scratch-pg 2>&1 | Out-String).Trim()
}
Add-Content -Encoding utf8 $log ('docker_rm=' + $rm)
Start-Sleep -Seconds 3
Add-Content -Encoding utf8 $log ('listeners_55447_after_container_rm=' + @(Get-NetTCPConnection -State Listen -LocalPort 55447 -ErrorAction SilentlyContinue).Count)
$left = (docker ps -a --filter 'name=tooba-111' --format '{{.Names}}' | Out-String).Trim()
if (-not $left) { $left = 'NONE' }
Add-Content -Encoding utf8 $log ('docker_containers_named_tooba-111_remaining=' + $left)

# --- remove task-owned tmp paths and the scratch credential file ---
Remove-Item -Recurse -Force (Join-Path $repo '.tmp-111-run'), (Join-Path $repo '.tmp-111-host-out') -ErrorAction SilentlyContinue
Remove-Item -Force (Join-Path $repo '.tmp-111-scratch-password.txt') -ErrorAction SilentlyContinue
$remaining = @(Get-ChildItem -Force -Path $repo -Filter '.tmp-111*' | Select-Object -ExpandProperty Name)
if ($remaining.Count -eq 0) { $remainingText = 'NONE' } else { $remainingText = $remaining -join ',' }
Add-Content -Encoding utf8 $log ('tmp_paths_remaining=' + $remainingText)
Get-Content $log
