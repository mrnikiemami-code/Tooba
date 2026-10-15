param(
    [Parameter(Mandatory = $true)][ValidateSet('ON', 'OFF')][string]$Mode
)
$ErrorActionPreference = 'Continue'
$exe = 'D:\Users\User\source\repos\SarvNewVer\src\backend\Host\Tooba.Host\bin\Debug\net8.0\Tooba.Host.exe'
$base = 'http://127.0.0.1:5088'
$log = Join-Path $PSScriptRoot "log-relaunch-$Mode.txt"

$out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { $script:out.Add($s); Write-Host $s }

function Listening { (Get-NetTCPConnection -State Listen -LocalPort 5088 -ErrorAction SilentlyContinue | Measure-Object).Count }

Emit "=== TB-SHIPPING-FULL-OPERATIONS-079 :: controlled relaunch ($Mode) : $(Get-Date -Format o) ==="

# stop whatever currently owns the port (the pre-existing Development Host or our own previous run)
$owners = Get-NetTCPConnection -State Listen -LocalPort 5088 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique
foreach ($ownerPid in $owners) {
    $p = Get-CimInstance Win32_Process -Filter "ProcessId = $ownerPid"
    Emit "STOP pid=$ownerPid name=$($p.Name) start=$($p.CreationDate)"
    Stop-Process -Id $ownerPid -Force
}
Start-Sleep -Seconds 4
Emit "port free: $(Listening) listeners"

# clear any inherited override so the two runs cannot contaminate each other
Remove-Item Env:Tooba__ShippingOperationalShadow__Enabled -ErrorAction SilentlyContinue
Remove-Item Env:Tooba__ShippingOperationalShadow__PrerequisitesConfirmed -ErrorAction SilentlyContinue

if ($Mode -eq 'ON') {
    $env:Tooba__ShippingOperationalShadow__Enabled = 'true'
    $env:Tooba__ShippingOperationalShadow__PrerequisitesConfirmed = 'true'
    $env:Logging__LogLevel__Default = 'Information'
    Emit "override env for THIS process: Tooba__ShippingOperationalShadow__Enabled=true, Tooba__ShippingOperationalShadow__PrerequisitesConfirmed=true, Logging__LogLevel__Default=Information"
} else {
    Emit "NO override env: the restored process runs on the repository's safe defaults (both flags false)"
}
Emit "no configuration file was created or modified; the override lives only in this process environment"

$proc = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -PassThru -RedirectStandardOutput (Join-Path $PSScriptRoot "host-stdout-$Mode.txt") -RedirectStandardError (Join-Path $PSScriptRoot "host-stderr-$Mode.txt")
Emit "RUNNING pid=$($proc.Id) start=$($proc.StartTime)"

$ready = $false
for ($i = 0; $i -lt 60; $i++) {
    Start-Sleep -Seconds 2
    try {
        $h = Invoke-WebRequest -Uri "$base/health" -UseBasicParsing -TimeoutSec 5
        if ($h.StatusCode -eq 200) { Emit "HEALTH 200 after ~$((($i + 1) * 2))s : $($h.Content)"; $ready = $true; break }
    } catch { }
}
Emit "ready=$ready"

$out -join "`r`n" | Set-Content $log -Encoding UTF8
Write-Host "WROTE $log"
