param([string]$Gate = 'off', [string]$Tag = 'off')
$ErrorActionPreference = 'Stop'
$repo = 'D:\Users\User\source\repos\SarvNewVer'
$out = Join-Path $repo '.tmp-102-host-out'
$run = Join-Path $repo '.tmp-102-run'
$ev = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-102'

Copy-Item (Join-Path $run 'appsettings.json') (Join-Path $out 'appsettings.json') -Force
Copy-Item (Join-Path $run 'appsettings.Development.json') (Join-Path $out 'appsettings.Development.json') -Force

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5192'
if ($Gate -eq 'on') { $env:Tooba__ShippingOneLevelCatalog__Enabled = 'true' } else { Remove-Item Env:Tooba__ShippingOneLevelCatalog__Enabled -ErrorAction SilentlyContinue }
# Never set: ShippingRateAuthority / ShippingOperationalShadow remain OFF.

$exe = Join-Path $out 'Tooba.Host.exe'
$p = Start-Process -FilePath $exe -WorkingDirectory $out -PassThru `
    -RedirectStandardOutput (Join-Path $ev "isolated-host-102-stdout-$Tag.txt") `
    -RedirectStandardError (Join-Path $ev "isolated-host-102-stderr-$Tag.txt")
"ISOLATED_HOST_PID=$($p.Id)" | Tee-Object -FilePath (Join-Path $ev "log-isolated-host-102-$Tag.txt")
