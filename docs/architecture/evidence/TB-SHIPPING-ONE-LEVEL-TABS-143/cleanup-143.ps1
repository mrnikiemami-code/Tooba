# TB-SHIPPING-ONE-LEVEL-TABS-143 — PID-verified cleanup of every task-owned isolated resource.
# Nothing shared is ever touched: the process is matched by its exact isolated executable path and the
# container is matched by its exact task-owned name.
$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$runDir = Join-Path $repo '.tmp-143-run'
$expectedExe = (Resolve-Path -LiteralPath (Join-Path $repo '.tmp-143-host-out\Tooba.Host.exe')).Path
$pidFile = Join-Path $runDir 'host.pid'
$dir = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-143'
$lines = New-Object System.Collections.Generic.List[string]

$recordedPid = 0
if (Test-Path -LiteralPath $pidFile) { $recordedPid = [int](Get-Content -LiteralPath $pidFile -Raw).Trim() }
$lines.Add("RECORDED_HOST_PID=$recordedPid")

$stopped = $false
if ($recordedPid -gt 0) {
    $proc = Get-Process -Id $recordedPid -ErrorAction SilentlyContinue
    if ($proc -and $proc.Path -eq $expectedExe) {
        Stop-Process -Id $recordedPid -Force
        $stopped = $true
    }
    elseif ($proc) {
        throw "refusing to stop PID ${recordedPid}: path $($proc.Path) is not the isolated Host"
    }
}
$lines.Add("ISOLATED_HOST_STOPPED=$stopped")

$stillListening = $false
try {
    $c = New-Object System.Net.Sockets.TcpClient
    $c.Connect('127.0.0.1', 5411)
    $stillListening = $true
    $c.Close()
} catch { }
$lines.Add("LISTENER_ON_5411_REMAINS=$stillListening")

$sharedAlive = $false
try {
    $c2 = New-Object System.Net.Sockets.TcpClient
    $c2.Connect('127.0.0.1', 5088)
    $sharedAlive = $true
    $c2.Close()
} catch { }
$lines.Add("SHARED_5088_LISTENER_ALIVE=$sharedAlive")

$containerRemoved = $false
$dockerRemove = (docker rm -f tooba-tb143-pg 2>&1 | Out-String).Trim()
if ($dockerRemove -match 'tooba-tb143-pg') { $containerRemoved = $true }
$lines.Add("SCRATCH_CONTAINER_REMOVED=$containerRemoved")
$lines.Add("SCRATCH_CONTAINER_REMOVE_OUTPUT=$dockerRemove")

Remove-Item -Recurse -Force $runDir -ErrorAction SilentlyContinue
$lines.Add('SCRATCH_RUN_DIR_DELETED=' + (-not (Test-Path -LiteralPath $runDir)))

$lines -join "`n" | Set-Content -LiteralPath (Join-Path $dir 'cleanup-143.txt') -Encoding utf8
$lines | ForEach-Object { Write-Output $_ }
