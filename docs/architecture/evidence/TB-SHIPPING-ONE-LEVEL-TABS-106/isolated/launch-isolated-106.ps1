# TB-SHIPPING-ONE-LEVEL-TABS-106 — ONE isolated startup attempt with an OUTER watchdog.
# Purpose: capture the 105 diagnostic line (stage=catalog-dev.attribute-schema.publish-product; missing=[...])
# emitted when CatalogAttributeSchemaDevelopmentSeed.PublishProductAsync is refused on an empty scratch database.
# Exactly one launch. Hard watchdog 240s. No retry.
$ErrorActionPreference = 'Stop'
$repo = 'D:\Users\User\source\repos\SarvNewVer'
$out = Join-Path $repo '.tmp-106-host-out'
$ev = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-106'
$stdout = Join-Path $ev 'isolated-host-106-stdout.txt'
$stderr = Join-Path $ev 'isolated-host-106-stderr.txt'
$watchdog = Join-Path $ev 'watchdog-result.txt'
$watchdogLimitSeconds = 240

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5392'
# No seed controls, no shipping gates, no auth bypass.
foreach ($name in @(
    'Tooba__ShippingOneLevelCatalog__Enabled',
    'Tooba__ShippingRateAuthority__Enabled',
    'Tooba__ShippingOperationalShadow__Enabled',
    'Tooba__CatalogDemo__RunLegacyBootstraps',
    'Tooba__CatalogDemo__AllowResetAndSeed')) {
    Remove-Item "Env:$name" -ErrorAction SilentlyContinue
}

Remove-Item $stdout, $stderr -ErrorAction SilentlyContinue
$started = Get-Date
$exe = Join-Path $out 'Tooba.Host.exe'
$p = Start-Process -FilePath $exe -WorkingDirectory $out -PassThru `
    -RedirectStandardOutput $stdout -RedirectStandardError $stderr

$diagnosticFound = $false
$exitCode = $null
$elapsed = 0
while ($true) {
    Start-Sleep -Seconds 5
    $elapsed = [int]((Get-Date) - $started).TotalSeconds

    $text = ''
    if (Test-Path $stdout) { $text += (Get-Content $stdout -Raw -ErrorAction SilentlyContinue) }
    if (Test-Path $stderr) { $text += (Get-Content $stderr -Raw -ErrorAction SilentlyContinue) }
    if ($text -and $text.Contains('Tooba.Catalog.Development.PublishReadiness')) { $diagnosticFound = $true }

    $p.Refresh()
    $hasExited = $p.HasExited
    if ($hasExited) { $exitCode = $p.ExitCode }

    if ($diagnosticFound -and $hasExited) { break }
    if ($hasExited -and $elapsed -ge 10) { break }
    if ($elapsed -ge $watchdogLimitSeconds) { break }
}

$p.Refresh()
$killed = $false
if (-not $p.HasExited) {
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
    $killed = $true
    Start-Sleep -Seconds 3
}

$listeners = (Get-NetTCPConnection -State Listen -LocalPort 5392 -ErrorAction SilentlyContinue | Measure-Object).Count

@(
    "ISOLATED_HOST_PID=$($p.Id)",
    "LAUNCH_COUNT=1",
    "WATCHDOG_LIMIT_SECONDS=$watchdogLimitSeconds",
    "ELAPSED_SECONDS=$elapsed",
    "PROCESS_EXITED=$($p.HasExited)",
    "EXIT_CODE=$exitCode",
    "KILLED_BY_WATCHDOG=$killed",
    "DIAGNOSTIC_LINE_CAPTURED=$diagnosticFound",
    "LISTENERS_ON_5392_AFTER_STOP=$listeners"
) | Tee-Object -FilePath $watchdog
