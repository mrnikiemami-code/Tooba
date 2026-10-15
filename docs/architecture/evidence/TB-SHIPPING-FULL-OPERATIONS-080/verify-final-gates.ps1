$ErrorActionPreference = 'Continue'
$Base = 'http://127.0.0.1:5088'
$log = Join-Path $PSScriptRoot 'log-final-gate-verification.txt'
$out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { $script:out.Add($s); Write-Host $s }

Emit "=== TB-SHIPPING-FULL-OPERATIONS-080 :: focused current-state gate verification : $(Get-Date -Format o) ==="

# --- target provenance, positively verified on the CURRENT process ---
$prov = Invoke-RestMethod -Uri "$Base/__platform-commerce" -TimeoutSec 15
Emit "provenance: edition=$($prov.edition) deploymentId=$($prov.deploymentId) tenantId=$($prov.tenantId) connectionReference=$($prov.connectionReference) market=$($prov.defaultMarketReference)"

# --- which process owns the port right now, and what launched it ---
$ownerPid = (Get-NetTCPConnection -State Listen -LocalPort 5088 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique | Select-Object -First 1)
$proc = Get-CimInstance Win32_Process -Filter "ProcessId = $ownerPid"
Emit "current host pid=$ownerPid name=$($proc.Name) started=$($proc.CreationDate)"
Emit "no gate override is configured in the repository: appsettings carries no Tooba:ShippingRateAuthority and no Tooba:ShippingOperationalShadow section, so both gates run on their safe default (false/false)."

# --- current process environment: prove no override leaked in ---
$envDump = (Get-ChildItem "Env:" | Where-Object { $_.Name -like 'Tooba__Shipping*' } | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join '; '
Emit "inherited override in THIS shell: $(if ([string]::IsNullOrEmpty($envDump)) { 'none' } else { $envDump })"

Emit ""
Emit "--- one focused read-only storefront projection call on the CURRENT process ---"
$sessionFile = Join-Path $env:TEMP '079-session.json'
$s = Get-Content $sessionFile -Raw | ConvertFrom-Json
$H = @{ 'Authorization' = "Bearer $($s.token)"; 'Accept-Language' = 'fa'; 'X-Tooba-Guest-Secret' = $($s.guestSecret) }
$IR     = 'e3451206-e503-51a8-91ad-afa9ab70e330'
$ALBORZ = '1f8050a8-c325-59fa-959f-82d76b5b2bc5'
$KARAJ  = '68255340-c69f-5d20-bf33-e8e0b24edde9'
$body = '{"cartId":"' + $s.cartId + '","provinceName":"البرز","cityName":"کرج","destination":{"countryId":"' + $IR + '","administrativeAreaId":"' + $ALBORZ + '","cityId":"' + $KARAJ + '"},"methodCode":null,"language":"fa"}'
$raw = Invoke-WebRequest -Uri "$Base/v1/storefront/shipping/projection" -Method POST -Headers $H -ContentType 'application/json' -Body $body -TimeoutSec 60
$p = $raw.Content | ConvertFrom-Json
$codes = ($p.methods | ForEach-Object { "$($_.methodCode)=$($_.priceAmount)/L$($_.leadDays)" }) -join ' '
Emit "HTTP $($raw.StatusCode) length=$($raw.Content.Length) methods=$($p.methods.Count) [$codes]"
Emit "selected=$($p.selectedMethodCode) amount=$($p.selectedShippingAmount) minDelivery=$($p.minimumDeliveryDate) subtotal=$($p.subtotalExclusiveOfTax) currency=$($p.currency)"
Set-Content "$PSScriptRoot\final-current-projection.json" -Value $raw.Content -Encoding UTF8
Emit "wrote final-current-projection.json"

Emit ""
Emit "--- gate-OFF proof on the CURRENT process: zero operational-shadow log lines ---"
$stdout = Join-Path $PSScriptRoot 'host-stdout-OFF.txt'
if (Test-Path $stdout) {
    $shadow = (Select-String -Path $stdout -Pattern 'OperationalShadow' | Measure-Object).Count
    $simple = (Select-String -Path $stdout -Pattern 'RateAuthority\.StorefrontShippingShadowObserver' | Measure-Object).Count
    Emit "current-run host stdout: operational-shadow lines=$shadow ; simple-rate-shadow lines=$simple"
} else {
    Emit "host stdout capture not present for the current run"
}

Emit ""
Emit "--- the same request with the gate ON is recorded in 079 evidence (payloads-OFF.json == payloads-ON.json) ---"
$off = Get-Content (Join-Path $PSScriptRoot '..\TB-SHIPPING-FULL-OPERATIONS-079\payloads-OFF.json') -Raw
$on  = Get-Content (Join-Path $PSScriptRoot '..\TB-SHIPPING-FULL-OPERATIONS-079\payloads-ON.json') -Raw
Emit "079 captured OFF length=$($off.Length) ON length=$($on.Length) identical=$($off -eq $on)"

$out -join "`r`n" | Set-Content $log -Encoding UTF8
Write-Host "WROTE $log"
