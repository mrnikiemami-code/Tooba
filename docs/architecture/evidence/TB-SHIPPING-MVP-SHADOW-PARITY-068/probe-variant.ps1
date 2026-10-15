$ErrorActionPreference = 'Continue'
$Base = 'http://127.0.0.1:5088'
$A = '01a036c2-970e-7000-8eb7-94bf5cc2d8db'
$ha = @{ 'X-Tooba-Dev-Actor-User-Id' = $A }
$out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { $script:out.Add($s); Write-Host $s }

$tipaxRate  = '01a13d65-cf37-7000-ad21-ac8e9b2c89d7'
$tipaxGrant = '01a13d65-cf8e-7000-a8e5-2205f6167d63'
$sessionFile = Join-Path $env:TEMP '068-session.json'
$s = Get-Content $sessionFile -Raw | ConvertFrom-Json
$H = @{ 'Authorization' = "Bearer $($s.token)"; 'Accept-Language' = 'fa'; 'X-Tooba-Guest-Secret' = $s.guestSecret }
$IR = 'e3451206-e503-51a8-91ad-afa9ab70e330'; $ALBORZ = '1f8050a8-c325-59fa-959f-82d76b5b2bc5'; $KARAJ = '68255340-c69f-5d20-bf33-e8e0b24edde9'
$body = '{"cartId":"' + $s.cartId + '","provinceName":"البرز","cityName":"کرج","destination":{"countryId":"' + $IR + '","administrativeAreaId":"' + $ALBORZ + '","cityId":"' + $KARAJ + '"},"methodCode":null,"language":"fa"}'

function Toggle($name, $url, $state) {
    $b = '{"isActive":' + ($(if ($state) { 'true' } else { 'false' })) + '}'
    try { $r = Invoke-WebRequest -Uri $url -Method PATCH -Headers $ha -ContentType 'application/json' -Body $b -UseBasicParsing -TimeoutSec 30; Emit "$name => $($r.StatusCode)" }
    catch { $resp = $_.Exception.Response; if ($resp) { $sr = New-Object System.IO.StreamReader($resp.GetResponseStream()); Emit "$name => $([int]$resp.StatusCode) $($sr.ReadToEnd())" } else { Emit "$name => ERR $($_.Exception.Message)" } }
}

Emit "=== TB-SHIPPING-MVP-SHADOW-PARITY-068 :: controlled data variant V2 (isolate the tipax service) ==="
Emit "purpose: the shadow quotes ONE rate per service, but legacy 'tipax' exposes TWO priced options"
Emit "(tipax:express 260000 / tipax:standard 180000). V2 removes tipax coverage so both tipax candidates must"
Emit "fail closed while the four single-amount services keep matching -> proves both tipax candidates were really"
Emit "evaluated and that the service/option collapse, not a missing evaluation, is the mismatch cause."
Emit ""
Emit "--- V1 state (tipax fixture active, charge 180000) already recorded; switching to V2 ---"
Toggle 'V2 deactivate tipax rate'  "$Base/v1/admin/shipping-services/tipax/rates/$tipaxRate/active" $false
Toggle 'V2 deactivate tipax grant' "$Base/v1/admin/shipping-services/tipax/coverage-grants/$tipaxGrant/active" $false
Emit ""
Emit "--- V2 projection request (same cart, same validated Geography ids) ---"
try {
    $p = Invoke-RestMethod -Uri "$Base/v1/storefront/shipping/projection" -Method POST -Headers $H -ContentType 'application/json' -Body $body -TimeoutSec 60
    $codes = ($p.methods | ForEach-Object { "$($_.methodCode)=$($_.priceAmount)" }) -join ' '
    Emit "V2 legacy projection unchanged: methods=$($p.methods.Count) [$codes] subtotal=$($p.subtotalExclusiveOfTax)"
} catch { Emit "V2 projection ERR $($_.Exception.Message)" }
Emit ""
Emit "--- restore V1 state exactly (charge 180000, both active) ---"
Toggle 'V1 reactivate tipax rate'  "$Base/v1/admin/shipping-services/tipax/rates/$tipaxRate/active" $true
Toggle 'V1 reactivate tipax grant' "$Base/v1/admin/shipping-services/tipax/coverage-grants/$tipaxGrant/active" $true
Emit ""
try {
    $r = Invoke-WebRequest -Uri "$Base/v1/admin/shipping-services/tipax/rates" -Headers $ha -UseBasicParsing -TimeoutSec 25
    Emit "post-restore tipax state: $($r.Content)"
} catch { Emit "post-restore ERR $($_.Exception.Message)" }

$out -join "`r`n" | Set-Content "$PSScriptRoot\log-variant-v2.txt" -Encoding UTF8
Write-Host "WROTE log-variant-v2.txt"
