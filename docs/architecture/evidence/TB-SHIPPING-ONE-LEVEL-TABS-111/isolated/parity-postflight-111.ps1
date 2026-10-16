$ErrorActionPreference = 'Stop'
$repo = 'D:\Users\User\source\repos\SarvNewVer'
$ev = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-111'
$log = Join-Path $ev 'cleanup-and-parity.txt'

Add-Content -Encoding utf8 $log '--- SHARED PARITY POSTFLIGHT (read-only) ---'
Add-Content -Encoding utf8 $log ('shared_pid_5088=' + (Get-NetTCPConnection -State Listen -LocalPort 5088 -ErrorAction SilentlyContinue | Select-Object -First 1).OwningProcess)
Add-Content -Encoding utf8 $log ('shared_pid_3000=' + (Get-NetTCPConnection -State Listen -LocalPort 3000 -ErrorAction SilentlyContinue | Select-Object -First 1).OwningProcess)

try { $h = 'shared_health_5088=' + (Invoke-WebRequest -Uri 'http://127.0.0.1:5088/health' -UseBasicParsing -TimeoutSec 8).Content }
catch { $h = 'shared_health_5088=ERR ' + $_.Exception.Message }
Add-Content -Encoding utf8 $log $h

try { $pc = 'shared_platform_commerce=' + (Invoke-WebRequest -Uri 'http://127.0.0.1:5088/__platform-commerce' -UseBasicParsing -TimeoutSec 8).Content }
catch { $pc = 'shared_platform_commerce=ERR ' + $_.Exception.Message }
Add-Content -Encoding utf8 $log $pc

try { $n = Invoke-WebRequest -Uri 'http://127.0.0.1:3000/' -UseBasicParsing -TimeoutSec 8; $ns = 'shared_next_3000=HTTP ' + $n.StatusCode }
catch { $ns = 'shared_next_3000=' + $_.Exception.Message }
Add-Content -Encoding utf8 $log $ns

Get-Content $log | Select-Object -Last 8
