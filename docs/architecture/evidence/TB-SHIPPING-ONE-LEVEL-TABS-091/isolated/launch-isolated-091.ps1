$ErrorActionPreference = 'Continue'
$repo = 'D:\Users\User\source\repos\SarvNewVer'
$out = Join-Path $repo '.tmp-091-host-out'
$run = Join-Path $repo '.tmp-091-host-run'

# Stage the guarded isolated configuration into the task-owned build output directory.
Copy-Item (Join-Path $run 'appsettings.json') (Join-Path $out 'appsettings.json') -Force
Copy-Item (Join-Path $run 'appsettings.Development.json') (Join-Path $out 'appsettings.Development.json') -Force

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5099'
# The single isolated connection reference is declared in the guarded appsettings.Development.json
# staged above (exactly one tenant, exactly one reference, 127.0.0.1:55433 / tooba_091_scratch_q3w8t1).

$exe = Join-Path $out 'Tooba.Host.exe'
$p = Start-Process -FilePath $exe -WorkingDirectory $out -PassThru `
    -RedirectStandardOutput (Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-091\isolated-host-091-stdout.txt') `
    -RedirectStandardError (Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-091\isolated-host-091-stderr.txt')
"ISOLATED_HOST_PID=$($p.Id)" | Tee-Object -FilePath (Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-091\log-isolated-host-091.txt')
