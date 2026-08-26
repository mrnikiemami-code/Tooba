# Validation helper for environments where api.nuget.org DNS edges time out
# but a known Azure Front Door edge is reachable (TB-P05-T017-UNBLOCK-01).
# Does NOT disable NuGetAudit. Does NOT suppress NU1900.

param(
  [string]$EdgeIp = "150.171.109.34",
  [int]$ProxyPort = 18888,
  [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$proxyScript = Join-Path $PSScriptRoot "nuget-connect-proxy.mjs"
$cfgDir = Join-Path $env:TEMP "tooba-nuget-proxy-validation"
New-Item -ItemType Directory -Force -Path $cfgDir | Out-Null
$cfgPath = Join-Path $cfgDir "NuGet.Proxy.config"

@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="http_proxy" value="http://127.0.0.1:$ProxyPort" />
    <add key="https_proxy" value="http://127.0.0.1:$ProxyPort" />
  </config>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"@ | Set-Content -Path $cfgPath -Encoding UTF8

$env:TOOBA_NUGET_PROXY_PORT = "$ProxyPort"
$env:TOOBA_NUGET_EDGE_IP = $EdgeIp
$proxy = Start-Process -FilePath "node" -ArgumentList @($proxyScript) -WorkingDirectory $root -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 1

try {
  $env:DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2SUPPORT = "true"
  Write-Host "restore..."
  & dotnet restore (Join-Path $root "src/backend/Tooba.slnx") --configfile $cfgPath
  if ($LASTEXITCODE -ne 0) { throw "restore failed" }
  Write-Host "build..."
  & dotnet build (Join-Path $root "src/backend/Tooba.slnx") --configfile $cfgPath
  if ($LASTEXITCODE -ne 0) { throw "build failed" }
  if (-not $SkipTests) {
    Remove-Item Env:HTTP_PROXY -ErrorAction SilentlyContinue
    Remove-Item Env:HTTPS_PROXY -ErrorAction SilentlyContinue
    Remove-Item Env:ALL_PROXY -ErrorAction SilentlyContinue
    $env:NO_PROXY = "127.0.0.1,localhost"
    Write-Host "test..."
    & dotnet test (Join-Path $root "src/backend/Tooba.slnx") --no-build
    if ($LASTEXITCODE -ne 0) { throw "test failed" }
  }
  Write-Host "validation helper completed"
}
finally {
  if ($proxy -and -not $proxy.HasExited) {
    Stop-Process -Id $proxy.Id -Force -ErrorAction SilentlyContinue
  }
}
