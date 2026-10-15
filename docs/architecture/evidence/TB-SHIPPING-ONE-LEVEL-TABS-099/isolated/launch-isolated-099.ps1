$ErrorActionPreference = 'Stop'
$repo = 'D:\Users\User\source\repos\SarvNewVer'
$out = Join-Path $repo '.tmp-099-host-out'
$run = Join-Path $repo '.tmp-099-host-run'
$ev = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-099'

# Stage the guarded isolated configuration over the build output so the process cannot fall back
# to the repository's shared Development connection settings.
Copy-Item (Join-Path $run 'appsettings.json') (Join-Path $out 'appsettings.json') -Force
Copy-Item (Join-Path $run 'appsettings.Development.json') (Join-Path $out 'appsettings.Development.json') -Force

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5099'
# Process-only one-level gate; never persisted in any file.
$env:Tooba__ShippingOneLevelCatalog__Enabled = 'true'
# Never set: ShippingRateAuthority / ShippingOperationalShadow remain OFF.

$exe = Join-Path $out 'Tooba.Host.exe'
$p = Start-Process -FilePath $exe -WorkingDirectory $out -PassThru `
    -RedirectStandardOutput (Join-Path $ev 'isolated-host-099-stdout.txt') `
    -RedirectStandardError (Join-Path $ev 'isolated-host-099-stderr.txt')
"ISOLATED_HOST_PID=$($p.Id)" | Tee-Object -FilePath (Join-Path $ev 'log-isolated-host-099.txt')
