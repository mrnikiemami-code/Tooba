param(
    [Parameter(Mandatory = $true)][ValidateSet('OFF', 'ON')][string]$Label
)
$ErrorActionPreference = 'Continue'
$Base = 'http://127.0.0.1:5088'
$IR     = 'e3451206-e503-51a8-91ad-afa9ab70e330'
$ALBORZ = '1f8050a8-c325-59fa-959f-82d76b5b2bc5'
$KARAJ  = '68255340-c69f-5d20-bf33-e8e0b24edde9'
$offerId = '0442a1fa-1d95-4b24-b9d9-129fc19942d2'
$sessionFile = Join-Path $env:TEMP '079-session.json'
$adminSessionFile = Join-Path $env:TEMP '079-admin.json'

$out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { $script:out.Add($s); Write-Host $s }

function Show($name, $block) {
    try { & $block } catch {
        $resp = $_.Exception.Response
        if ($resp) { $sr = New-Object System.IO.StreamReader($resp.GetResponseStream()); Emit "$name => HTTP $([int]$resp.StatusCode) $($sr.ReadToEnd())" }
        else { Emit "$name => ERR $($_.Exception.Message)" }
    }
}

function Unwrap($body) {
    if ($null -eq $body) { return $null }
    if ($body -is [string]) { return $body }
    if ($null -ne $body.PSObject.Properties['data']) { return $body.data }
    return $body
}

# --- admin session (Development bootstrap actor) ---
if (Test-Path $adminSessionFile) {
    $token = (Get-Content $adminSessionFile -Raw | ConvertFrom-Json).token
    Emit "reusing the task-owned Development admin token"
} else {
    $login = Invoke-RestMethod -Uri "$Base/v1/auth/login" -Method POST -ContentType 'application/json' `
        -Body '{"identifierKind":"Email","identifier":"admin-actor@tooba.local","password":"admin-dev-horse-1"}' -TimeoutSec 30
    $token = $login.accessToken
    @{ token = $token } | ConvertTo-Json | Set-Content $adminSessionFile -Encoding UTF8
    Emit "acquired Development admin token"
}

# --- disposable customer session (created once, reused OFF and ON) ---
if (Test-Path $sessionFile) {
    $s = Get-Content $sessionFile -Raw | ConvertFrom-Json
    $custToken = $s.token; $cartId = $s.cartId; $guestSecret = $s.guestSecret
    Emit "reusing the task-owned disposable Development cart/session"
} else {
    $stamp = Get-Date -Format 'yyyyMMddHHmmss'
    $identifier = "079.probe.$stamp@tooba.local"
    $body = '{"identifierKind":"email","identifier":"' + $identifier + '","password":"Tooba!079probe"}'
    $null = Invoke-RestMethod -Uri "$Base/v1/auth/register" -Method POST -ContentType 'application/json' -Body $body -TimeoutSec 30
    $l = Invoke-RestMethod -Uri "$Base/v1/auth/login" -Method POST -ContentType 'application/json' -Body $body -TimeoutSec 30
    $custToken = $l.accessToken
    $h = @{ 'Authorization' = "Bearer $custToken"; 'Accept-Language' = 'fa' }
    $cart = Invoke-RestMethod -Uri "$Base/v1/storefront/cart" -Method POST -Headers $h -ContentType 'application/json' -Body '{}' -TimeoutSec 30
    $cartId = $cart.cartId; $guestSecret = $cart.guestSecret
    $h2 = @{ 'Authorization' = "Bearer $custToken"; 'Accept-Language' = 'fa'; 'X-Tooba-Guest-Secret' = $guestSecret; 'X-Tooba-Cart-Version' = [string]$cart.version }
    $line = Invoke-RestMethod -Uri "$Base/v1/storefront/cart/$cartId/lines" -Method POST -Headers $h2 -ContentType 'application/json' -Body ('{"offerId":"' + $offerId + '","quantity":1,"currency":"IRR"}') -TimeoutSec 60
    Emit "created disposable customer $identifier ; cart $cartId ; line added, version=$($line.version) itemCount=$($line.itemCount)"
    @{ token = $custToken; cartId = $cartId; guestSecret = $guestSecret; customer = $identifier } | ConvertTo-Json | Set-Content $sessionFile -Encoding UTF8
}

$H = @{ 'Authorization' = "Bearer $custToken"; 'Accept-Language' = 'fa'; 'X-Tooba-Guest-Secret' = $guestSecret }
$AH = @{ 'Authorization' = "Bearer $token"; 'Accept-Language' = 'fa' }

Emit ""
Emit "=== TB-SHIPPING-FULL-OPERATIONS-079 :: storefront operational-shadow matrix ($Label) : $(Get-Date -Format o) ==="
Emit "cart=$cartId (task-owned disposable Development cart, one real line, no order, no payment)"

$payloads = @{}

function Projection($name, $body) {
    Show "$name" {
        $raw = Invoke-WebRequest -Uri "$Base/v1/storefront/shipping/projection" -Method POST -Headers $H -ContentType 'application/json' -Body $body -TimeoutSec 60
        $p = $raw.Content | ConvertFrom-Json
        $codes = ($p.methods | ForEach-Object { "$($_.methodCode)=$($_.priceAmount)/L$($_.leadDays)" }) -join ' '
        Emit "  methods=$($p.methods.Count) [$codes]"
        Emit "  selected=$($p.selectedMethodCode) amount=$($p.selectedShippingAmount) minDelivery=$($p.minimumDeliveryDate) subtotal=$($p.subtotalExclusiveOfTax) currency=$($p.currency)"
        $script:payloads[$name] = $raw.Content
    }
}

Projection 'P1 legacy-client (province only)' ('{"cartId":"' + $cartId + '","provinceName":"البرز","methodCode":null,"language":"fa"}')
Projection 'P2 city text (no ids)' ('{"cartId":"' + $cartId + '","provinceName":"البرز","cityName":"کرج","methodCode":null,"language":"fa"}')
Projection 'P3 validated Geography ids' ('{"cartId":"' + $cartId + '","provinceName":"البرز","cityName":"کرج","destination":{"countryId":"' + $IR + '","administrativeAreaId":"' + $ALBORZ + '","cityId":"' + $KARAJ + '"},"methodCode":null,"language":"fa"}')

Emit ""
Emit "--- payload identity inside this run ---"
$keys = $payloads.Keys | Sort-Object
foreach ($k in $keys) { Emit "  $k length=$($payloads[$k].Length)" }
$baseP = $payloads['P1 legacy-client (province only)']
foreach ($k in $keys) { Emit "  P1 == $k : $($baseP -eq $payloads[$k])" }
$payloads | ConvertTo-Json -Depth 3 | Set-Content "$PSScriptRoot\payloads-$Label.json" -Encoding UTF8

# --- admin diagnostic operational quote: tier boundary, packaging, COD (authorized 076 route) ---
Emit ""
Emit "--- admin diagnostic operational-quote (authorized 076 read-only route) ---"
$scope = @{ countryId = $IR; administrativeAreaId = $ALBORZ }
function OQ($name, $extra) {
    $b = @{ scope = $scope; cartSubtotalExclusive = 500000; currencyCode = 'IRR'; optionCode = 'standard' }
    foreach ($k in $extra.Keys) { $b[$k] = $extra[$k] }
    Show $name {
        $r = Invoke-WebRequest -Uri "$Base/v1/admin/shipping-services/tipax/operational-quote" -Method POST -Headers $AH `
            -ContentType 'application/json' -Body ($b | ConvertTo-Json -Depth 6 -Compress) -TimeoutSec 30
        $q = Unwrap ($r.Content | ConvertFrom-Json)
        Emit "  outcome=$($q.outcome) freight=$($q.freightAmount) packaging=$($q.packagingAmount) total=$($q.totalServiceExpense) checkoutDue=$($q.amountPayableAtCheckout) codDue=$($q.amountCollectOnDelivery) payer=$($q.freightPayerMode) packagingPayer=$($q.packagingPayer)"
        Emit "  schedule=$($q.schedule.availability)/$($q.schedule.reasonCode) capacity=$($q.schedule.configuredCapacityTotal) enforcement=$($q.schedule.capacityEnforcement)"
        Emit "  reason=$($q.reasonCode) matched=$($q.matchedCondition | ConvertTo-Json -Compress) carrierReason=$($q.carrierFreight.reasonCode)"
    }
}
OQ 'OQ weight 1999g count 2 (Sunday inside window)' @{ weightGrams = 1999; itemCount = 2; requestedPickupAtUtc = '2026-10-18T10:00:00Z' }
OQ 'OQ weight 2000g count 2 (Sunday inside window)' @{ weightGrams = 2000; itemCount = 2; requestedPickupAtUtc = '2026-10-18T10:00:00Z' }
OQ 'OQ weight 1999g count 2 (Sunday OUTSIDE window)' @{ weightGrams = 1999; itemCount = 2; requestedPickupAtUtc = '2026-10-18T10:30:00Z' }
OQ 'OQ weight 1999g count 2 (no pickup instant => NotEvaluated)' @{ weightGrams = 1999; itemCount = 2 }
OQ 'OQ no measurement at all' @{ requestedPickupAtUtc = '2026-10-18T10:00:00Z' }

# --- no-write proof ---
Emit ""
Emit "--- no-write proof (byte identity of every Shipping surface) ---"
$rates = (Invoke-WebRequest -Uri "$Base/v1/admin/shipping-services/tipax/rates" -Headers $AH -UseBasicParsing -TimeoutSec 30).Content
$pack = (Invoke-WebRequest -Uri "$Base/v1/admin/shipping-services/tipax/packaging-policies" -Headers $AH -UseBasicParsing -TimeoutSec 30).Content
$sched = (Invoke-WebRequest -Uri "$Base/v1/admin/shipping-services/tipax/operating-schedules" -Headers $AH -UseBasicParsing -TimeoutSec 30).Content
Set-Content "$PSScriptRoot\baseline-$Label-rates.json" -Value $rates -Encoding UTF8
Set-Content "$PSScriptRoot\baseline-$Label-packaging.json" -Value $pack -Encoding UTF8
Set-Content "$PSScriptRoot\baseline-$Label-schedules.json" -Value $sched -Encoding UTF8
Emit "captured rates/packaging/schedules baselines for $Label"

$out -join "`r`n" | Set-Content "$PSScriptRoot\log-matrix-$Label.txt" -Encoding UTF8
Write-Host "WROTE log-matrix-$Label.txt"
