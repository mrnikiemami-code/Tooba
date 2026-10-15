param(
    [Parameter(Mandatory = $true)][ValidateSet('OFF', 'ON')][string]$Label
)
$ErrorActionPreference = 'Continue'
$Base = 'http://127.0.0.1:5088'
$offerId = '0442a1fa-1d95-4b24-b9d9-129fc19942d2'   # real dev catalogue offer (نایک شلوار مردانه سری 1)
$IR     = 'e3451206-e503-51a8-91ad-afa9ab70e330'
$ALBORZ = '1f8050a8-c325-59fa-959f-82d76b5b2bc5'
$KARAJ  = '68255340-c69f-5d20-bf33-e8e0b24edde9'
$sessionFile = Join-Path $env:TEMP '068-session.json'

$out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { $script:out.Add($s); Write-Host $s }

function Show($name, $block) {
    try { & $block } catch {
        $resp = $_.Exception.Response
        if ($resp) { $sr = New-Object System.IO.StreamReader($resp.GetResponseStream()); Emit "$name => HTTP $([int]$resp.StatusCode) $($sr.ReadToEnd())" }
        else { Emit "$name => ERR $($_.Exception.Message)" }
    }
}

# --- canonical development customer session: created once, reused for both OFF and ON -----------------
if (Test-Path $sessionFile) {
    $s = Get-Content $sessionFile -Raw | ConvertFrom-Json
    $token = $s.token; $cartId = $s.cartId; $guestSecret = $s.guestSecret
    Emit "reusing the task-owned canonical Development cart/session created earlier in this task"
} else {
    $stamp = Get-Date -Format 'yyyyMMddHHmmss'
    $identifier = "068.probe.$stamp@tooba.local"
    $pwd = 'Tooba!068probe'
    $body = '{"identifierKind":"email","identifier":"' + $identifier + '","password":"' + $pwd + '"}'
    $null = Invoke-RestMethod -Uri "$Base/v1/auth/register" -Method POST -ContentType 'application/json' -Body $body -TimeoutSec 30
    $l = Invoke-RestMethod -Uri "$Base/v1/auth/login" -Method POST -ContentType 'application/json' -Body $body -TimeoutSec 30
    $token = $l.accessToken
    $h = @{ 'Authorization' = "Bearer $token"; 'Accept-Language' = 'fa' }
    $cart = Invoke-RestMethod -Uri "$Base/v1/storefront/cart" -Method POST -Headers $h -ContentType 'application/json' -Body '{}' -TimeoutSec 30
    $cartId = $cart.cartId; $guestSecret = $cart.guestSecret
    $h2 = @{ 'Authorization' = "Bearer $token"; 'Accept-Language' = 'fa'; 'X-Tooba-Guest-Secret' = $guestSecret; 'X-Tooba-Cart-Version' = [string]$cart.version }
    $line = Invoke-RestMethod -Uri "$Base/v1/storefront/cart/$cartId/lines" -Method POST -Headers $h2 -ContentType 'application/json' -Body ('{"offerId":"' + $offerId + '","quantity":1,"currency":"IRR"}') -TimeoutSec 60
    Emit "created disposable customer $identifier ; cart $cartId ; line added, version=$($line.version) itemCount=$($line.itemCount)"
    @{ token = $token; cartId = $cartId; guestSecret = $guestSecret; customer = $identifier } | ConvertTo-Json | Set-Content $sessionFile -Encoding UTF8
}

$H = @{ 'Authorization' = "Bearer $token"; 'Accept-Language' = 'fa'; 'X-Tooba-Guest-Secret' = $guestSecret }

Emit ""
Emit "=== TB-SHIPPING-MVP-SHADOW-PARITY-068 :: storefront projection matrix (shadow $Label) : $(Get-Date -Format o) ==="
Emit "cart=$cartId  (task-owned disposable Development cart, one real line, no order, no payment)"
Emit ""

function Projection($name, $body) {
    Show "$name" {
        $p = Invoke-RestMethod -Uri "$Base/v1/storefront/shipping/projection" -Method POST -Headers $H -ContentType 'application/json' -Body $body -TimeoutSec 60
        $codes = ($p.methods | ForEach-Object { "$($_.methodCode)=$($_.priceAmount)/L$($_.leadDays)" }) -join ' '
        Emit "  methods=$($p.methods.Count) [$codes]"
        Emit "  selected=$($p.selectedMethodCode) amount=$($p.selectedShippingAmount) minDelivery=$($p.minimumDeliveryDate) subtotal=$($p.subtotalExclusiveOfTax) currency=$($p.currency) provinces=$($p.provinces.Count)"
        $script:payloads[$name] = ($p | ConvertTo-Json -Depth 8 -Compress)
    }
}

$payloads = @{}

# M1: old client shape — province only, no city, no destination ids
Projection 'M1 legacy-client (province only)' ('{"cartId":"' + $cartId + '","provinceName":"البرز","methodCode":null,"language":"fa"}')

# M2: city-precision legacy text only (city name, no ids)
Projection 'M2 city text (no ids)' ('{"cartId":"' + $cartId + '","provinceName":"البرز","cityName":"کرج","methodCode":null,"language":"fa"}')

# M3: validated country+area+city Geography ids (the real destination identity)
Projection 'M3 validated Geography ids' ('{"cartId":"' + $cartId + '","provinceName":"البرز","cityName":"کرج","destination":{"countryId":"' + $IR + '","administrativeAreaId":"' + $ALBORZ + '","cityId":"' + $KARAJ + '"},"methodCode":null,"language":"fa"}')

# M4: malformed city id inside a valid-looking hierarchy (must never be trusted)
Projection 'M4 malformed city id' ('{"cartId":"' + $cartId + '","provinceName":"البرز","cityName":"کرج","destination":{"countryId":"' + $IR + '","administrativeAreaId":"' + $ALBORZ + '","cityId":"00000000-0000-4000-8000-000000000999"},"methodCode":null,"language":"fa"}')

# M5: foreign/incoherent hierarchy (area from another country)
Projection 'M5 incoherent hierarchy' ('{"cartId":"' + $cartId + '","provinceName":"البرز","cityName":"کرج","destination":{"countryId":"e3451206-e503-51a8-91ad-afa9ab70e330","administrativeAreaId":"995347e5-0388-5d1d-8c0f-1fd7660216af","cityId":"' + $KARAJ + '"},"methodCode":null,"language":"fa"}')

Emit ""
Emit "--- payload identity check inside this run ---"
$keys = $payloads.Keys | Sort-Object
foreach ($k in $keys) { Emit "  $k length=$($payloads[$k].Length)" }
$base = $payloads['M1 legacy-client (province only)']
foreach ($k in $keys) { Emit "  M1 == $k : $($base -eq $payloads[$k])" }

$payloads | ConvertTo-Json -Depth 3 | Set-Content "$PSScriptRoot\payloads-$Label.json" -Encoding UTF8
$out -join "`r`n" | Set-Content "$PSScriptRoot\log-matrix-$Label.txt" -Encoding UTF8
Write-Host "WROTE log-matrix-$Label.txt"
