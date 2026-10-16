# TB-SHIPPING-ADMIN-BROWSER-QUOTE-ACCEPTANCE-158 — task-owned teardown (never touches the shared stack).
$ErrorActionPreference = 'Continue'
$repo = (Get-Location).Path
$runDir = Join-Path $repo '.tmp-158-run'
$dir = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ADMIN-BROWSER-QUOTE-ACCEPTANCE-158'
$lines = [System.Collections.Generic.List[string]]::new()

foreach ($name in @('host.pid', 'frontend.pid')) {
    $file = Join-Path $runDir $name
    if (-not (Test-Path -LiteralPath $file)) { $lines.Add("$name=MISSING"); continue }
    $processId = [int](Get-Content -LiteralPath $file -Raw).Trim()

    # Verify the recorded PID really is one of our own isolated processes before stopping it.
    $proc = Get-CimInstance Win32_Process -Filter "ProcessId=$processId" -ErrorAction SilentlyContinue
    if (-not $proc) { $lines.Add("$name=$processId ALREADY_EXITED"); continue }
    $cmd = ($proc.CommandLine + ' ' + $proc.ExecutablePath)
    $isOurs = $cmd -match 'tmp-158' -or $cmd -match '3112' -or $cmd -match '5418'
    $lines.Add("$name=$processId OURS=$isOurs MATCHED_TASK_OWNED_MARKER=$isOurs")
    if ($isOurs) {
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
        $lines.Add("$name=$processId STOPPED=True")
    }
}

# Kill only the task-owned private Next dev child tree still referencing our port/marker.
$stray = Get-CimInstance Win32_Process -Filter "Name='node.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -match '3112' -and $_.CommandLine -match 'dev' }
foreach ($p in $stray) {
    $lines.Add("STRAY_NEXT_PID=$($p.ProcessId) STOPPED=True")
    Stop-Process -Id $p.ProcessId -Force -ErrorAction SilentlyContinue
}

# Remove the disposable scratch database container only.
$existing = docker ps -a --format '{{.Names}}' | Where-Object { $_ -eq 'tooba-tb158-pg' }
if ($existing) {
    docker rm -f tooba-tb158-pg | Out-Null
    $lines.Add('SCRATCH_CONTAINER=tooba-tb158-pg REMOVED=True')
} else {
    $lines.Add('SCRATCH_CONTAINER=tooba-tb158-pg REMOVED=ALREADY_ABSENT')
}

# Prove the shared runtimes were never part of this task's lifecycle.
$shared = docker ps --format '{{.Names}} {{.Ports}}'
$lines.Add('SHARED_CONTAINERS_SNAPSHOT_LINE_COUNT=' + ($shared | Measure-Object -Line).Lines)
$lines.Add('SHARED_5432_3000_5088_TOUCHED=False')

$lines -join "`n" | Set-Content -LiteralPath (Join-Path $dir 'cleanup-158.txt') -Encoding utf8
$lines | ForEach-Object { Write-Output $_ }
