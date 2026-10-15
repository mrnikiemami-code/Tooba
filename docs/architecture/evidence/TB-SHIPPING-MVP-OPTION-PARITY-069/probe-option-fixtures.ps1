$ErrorActionPreference = 'Continue'
$B = 'http://127.0.0.1:5088'
$A = '01a036c2-970e-7000-8eb7-94bf5cc2d8db'
$ha = @{ 'X-Tooba-Dev-Actor-User-Id' = $A }
function Emit([string]$s) { Write-Host $s }

$IR     = 'e3451206-e503-51a8-91ad-afa9ab70e330'
$ALBORZ = '1f8050a8-c325-59fa-959f-82d76b5b2bc5'

function Call([string]$method, [string]$url, [string]$body) {
    try {
        if ($body) { $r = Invoke-WebRequest -Uri $url -Method $method -Headers $ha -ContentType 'application/json' -Body $body -UseBasicParsing -TimeoutSec 30 }
        else       { $r = Invoke-WebRequest -Uri $url -Method $method -Headers $ha -UseBasicParsing -TimeoutSec 30 }
        Emit "$method $url => $($r.StatusCode) $($r.Content)"
        return $r.Content
    } catch {
        $resp = $_.Exception.Response
        if ($resp) { $sr = New-Object System.IO.StreamReader($resp.GetResponseStream()); Emit "$method $url => $([int]$resp.StatusCode) $($sr.ReadToEnd())" }
        else { Emit "$method $url => ERR $($_.Exception.Message)" }
        return $null
    }
}

Emit "=== TB-SHIPPING-MVP-OPTION-PARITY-069 :: option-level rate fixtures (mission C) ==="
Emit "target: Development host :5088, tenant store-alpha, DB tooba_alpha (tenant-alpha connection reference)"
Emit "scope: country IR / administrative area alborz (area level) - exactly the 068 evidenced scope"
Emit "values are PARITY FIXTURES (mirror the measured legacy amounts), NOT approved commercial tariffs."
Emit ""

Emit "--- 1. invalid option combination must be refused by the Fulfillment catalog seam ---"
Call 'POST' "$B/v1/admin/shipping-services/tipax/rates" ('{"serviceCode":"tipax","scope":{"countryId":"' + $IR + '","administrativeAreaId":"' + $ALBORZ + '"},"chargeAmount":1,"currencyCode":"IRR","preparationDays":1,"leadDays":1,"optionCode":"ghost"}') | Out-Null
Call 'POST' "$B/v1/admin/shipping-services/ghost_service/rates" ('{"serviceCode":"ghost_service","scope":{"countryId":"' + $IR + '","administrativeAreaId":"' + $ALBORZ + '"},"chargeAmount":1,"currencyCode":"IRR","preparationDays":1,"leadDays":1}') | Out-Null
Emit ""

Emit "--- 2. pre-state of tipax (generic service-level row to be superseded) ---"
$pre = Call 'GET' "$B/v1/admin/shipping-services/tipax/rates" $null
$genericTipaxId = $null
if ($pre) {
    $j = $pre | ConvertFrom-Json
    foreach ($x in @($j.ratePolicies)) {
        Emit ("   PRE RATE {0} option={1} area={2} city={3} charge={4} lead={5} active={6}" -f $x.ratePolicyId, $x.optionCode, $x.administrativeAreaId, $x.cityId, $x.chargeAmount, $x.leadDays, $x.isActive)
        if ($x.isActive -and -not $x.administrativeAreaId -eq $null -and -not $x.optionCode) { $genericTipaxId = $x.ratePolicyId }
    }
}
Emit "GENERIC_TIPAX_ID=$genericTipaxId"
Emit ""

Emit "--- 3. option-level tipax fixtures: standard 180000/lead3 and express 260000/lead1 ---"
$fx = @(
    @{ option = 'standard'; charge = 180000; lead = 3 },
    @{ option = 'express';  charge = 260000; lead = 1 }
)
foreach ($f in $fx) {
    $body = '{"serviceCode":"tipax","scope":{"countryId":"' + $IR + '","administrativeAreaId":"' + $ALBORZ + '"},"chargeAmount":' + $f.charge + ',"currencyCode":"IRR","preparationDays":1,"leadDays":' + $f.lead + ',"optionCode":"' + $f.option + '"}'
    Call 'POST' "$B/v1/admin/shipping-services/tipax/rates" $body | Out-Null
}
Emit ""

Emit "--- 4. supersede the 068 generic tipax service-level row (only after both options exist) ---"
if ($genericTipaxId) {
    Call 'PATCH' "$B/v1/admin/shipping-services/tipax/rates/$genericTipaxId/active" '{"isActive":false}' | Out-Null
} else {
    Emit "   SKIP: generic tipax row not found"
}
Emit ""

Emit "--- 5. post-state of tipax (two option-level rows active, generic superseded) ---"
$post = Call 'GET' "$B/v1/admin/shipping-services/tipax/rates" $null
if ($post) {
    $j = $post | ConvertFrom-Json
    foreach ($x in @($j.ratePolicies)) {
        Emit ("   POST RATE {0} option={1} area={2} city={3} charge={4} lead={5} active={6}" -f $x.ratePolicyId, $x.optionCode, $x.administrativeAreaId, $x.cityId, $x.chargeAmount, $x.leadDays, $x.isActive)
    }
    foreach ($g in @($j.coverageGrants)) {
        Emit ("   POST GRANT {0} option-scope=service area={1} active={2}" -f $g.coverageGrantId, $g.administrativeAreaId, $g.isActive)
    }
}
Emit ""
Emit "ROLLBACK (bounded): deactivate both option rows by id and reactivate the generic row;"
Emit "the model change itself is reverted by the migration Down() (drops option_code)."
