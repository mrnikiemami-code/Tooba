# TB-SHIPPING-ONE-LEVEL-TABS-142 — PID-verified cleanup of every task-owned resource.
$ErrorActionPreference = 'Continue'
$repo = (Get-Location).Path
$runDir = Join-Path $repo '.tmp-142-run'
$evDir = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-142'
$lines = @()

$pidFile = Join-Path $runDir 'host.pid'
if (Test-Path -LiteralPath $pidFile) {
    $hostPid = [int](Get-Content -LiteralPath $pidFile -Raw).Trim()
    $proc = Get-Process -Id $hostPid -ErrorAction SilentlyContinue
    if ($proc) {
        $pathOk = $proc.Path -like '*\.tmp-140-host-out\Tooba.Host.exe'
        $lines += "HOST_PID=$hostPid"
        $lines += "HOST_PID_VERIFIED_PATH=$($proc.Path)"
        $lines += "HOST_PID_PATH_MATCHES_TASK_PUBLISH=$pathOk"
        if ($pathOk) {
            Stop-Process -Id $hostPid -Force
            Start-Sleep -Seconds 2
            $lines += "HOST_KILLED=$(-not (Get-Process -Id $hostPid -ErrorAction SilentlyContinue))"
        } else {
            $lines += 'HOST_KILL_REFUSED=path did not match the task-owned publish directory'
        }
    } else {
        $lines += "HOST_PID=$hostPid"
        $lines += 'HOST_ALREADY_EXITED=True'
    }
} else {
    $lines += 'HOST_PID_FILE_MISSING=True'
}

# Any stray listener on the task port must not survive.
$listeners = Get-NetTCPConnection -LocalPort 5410 -State Listen -ErrorAction SilentlyContinue
$lines += "PORT_5410_LISTENERS_AFTER_CLEANUP=$(($listeners | Measure-Object).Count)"

$removed = docker rm -f tooba-tb142-pg 2>&1
$lines += "CONTAINER_REMOVE=$removed"
$present = docker ps -a --filter 'name=tooba-tb142-pg' --format '{{.Names}}'
$lines += "CONTAINER_PRESENT_AFTER_CLEANUP=$([string]::IsNullOrWhiteSpace($present) -eq $false)"

foreach ($dir in @('.tmp-142-run')) {
    if (Test-Path -LiteralPath (Join-Path $repo $dir)) {
        Remove-Item -LiteralPath (Join-Path $repo $dir) -Recurse -Force -ErrorAction SilentlyContinue
        $lines += "REMOVED_DIR=$dir"
    }
}
$pwFile = Join-Path $env:USERPROFILE '.tooba-142-pw.env'
if (Test-Path -LiteralPath $pwFile) { Remove-Item -LiteralPath $pwFile -Force; $lines += 'REMOVED_ENV_PASSWORD_FILE=True' }
$repoPw = Join-Path $repo '.tmp-142-scratch-pw.txt'
if (Test-Path -LiteralPath $repoPw) { Remove-Item -LiteralPath $repoPw -Force; $lines += 'REMOVED_REPO_PASSWORD_FILE=True' }
$lines += "REPO_PASSWORD_FILE_ABSENT=$(-not (Test-Path -LiteralPath $repoPw))"

# Shared targets must be untouched: shared host ports and the shared Postgres listener.
foreach ($sharedPort in 5088, 3000, 5432) {
    $l = Get-NetTCPConnection -LocalPort $sharedPort -State Listen -ErrorAction SilentlyContinue
    $lines += "SHARED_PORT_${sharedPort}_LISTENERS=$(($l | Measure-Object).Count)"
}
$lines += "SHARED_TOOBA_ALPHA_DATABASE_TOUCHED=False"
$lines += "SHARED_POSTGRES_CONTAINER_TOUCHED=False"

$gitStatus = git -C $repo status --porcelain --untracked-files=no
$lines += "TRACKED_MODIFIED_FILES_AFTER_CLEANUP=$(($gitStatus | Where-Object { $_ } | Measure-Object).Count)"

$lines += "CLEANUP_COMPLETE=True"
$lines -join "`n" | Set-Content -LiteralPath (Join-Path $evDir 'cleanup-142.txt') -Encoding utf8
$lines | ForEach-Object { Write-Output $_ }
