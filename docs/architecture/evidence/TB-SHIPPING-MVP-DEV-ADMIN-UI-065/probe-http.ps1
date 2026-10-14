$ErrorActionPreference = 'Continue'
$B = 'http://127.0.0.1:5088'
$A = '01a036c2-970e-7000-8eb7-94bf5cc2d8db'
$log = "$PSScriptRoot\log-http-065.txt"
$out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { $script:out.Add($s); Write-Host $s }
function Req($name, $m, $u, $h, $b, $ct) {
    try {
        $p = @{ Uri = $u; Method = $m; UseBasicParsing = $true; TimeoutSec = 30 }
        if ($h) { $p.Headers = $h }
        if ($b -ne $null) { $p.Body = $b; $p.ContentType = if ($ct) { $ct } else { 'application/json' } }
        $r = Invoke-WebRequest @p
        $body = $r.Content
        if ($body.Length -gt 500) { $body = $body.Substring(0, 500) + '...<truncated>' }
        Emit "$name|STATUS=$($r.StatusCode)|BODY=$body"
        return $r.Content
    } catch {
        $resp = $_.Exception.Response
        if ($resp) {
            $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $t = $sr.ReadToEnd()
            if ($t.Length -gt 500) { $t = $t.Substring(0, 500) + '...<truncated>' }
            Emit "$name|STATUS=$([int]$resp.StatusCode)|BODY=$t"
        } else { Emit "$name|STATUS=TRANSPORT_ERROR|BODY=$($_.Exception.Message)" }
        return $null
    }
}

$ha = @{ 'X-Tooba-Dev-Actor-User-Id' = $A }
$hu = @{ 'X-Tooba-Dev-Actor-User-Id' = '00000000-0000-0000-0000-000000000123' }

Emit "=== TB-SHIPPING-MVP-DEV-ADMIN-UI-065 :: HTTP matrix against the user's Development host (:5088) ==="
Emit "TARGET=$B EDITION=SingleStore TENANT=store-alpha CONNECTION_REFERENCE=tenant-alpha DB=tooba_alpha"
Emit "SERVICE_CODE=post (existing ACTIVE fulfillment.shipping_services.code) — no imaginary code used"
Emit ""

Emit "--- 1. MISSION A: health, route availability, canonical admin auth ---"
Req 'A1 health'                              'GET' "$B/health" $null $null
Req 'A2 authorized GET rates (route mapped)' 'GET' "$B/v1/admin/shipping-services/post/rates" $ha $null
Req 'A3 authorized GET geography countries'  'GET' "$B/v1/admin/geography/countries" $ha $null
Req 'A4 GET rates unauthenticated'           'GET' "$B/v1/admin/shipping-services/post/rates" $null $null
Req 'A5 GET rates unknown actor'             'GET' "$B/v1/admin/shipping-services/post/rates" $hu $null
Emit ""

Emit "--- 2. MISSION A: 064 malformed-body handling still present on the live host ---"
Req 'D1 malformed POST rates'           'POST' "$B/v1/admin/shipping-services/post/rates" $ha '{not-json'
Req 'D2 empty-GUID scope POST rates'    'POST' "$B/v1/admin/shipping-services/post/rates" $ha '{"scope":{"countryId":""},"chargeAmount":1,"currencyCode":"IRR","preparationDays":1,"leadDays":1}'
Req 'D3 malformed POST coverage-grants' 'POST' "$B/v1/admin/shipping-services/post/coverage-grants" $ha '{oops'
Req 'D4 malformed PUT rate edit'        'PUT'  "$B/v1/admin/shipping-services/post/rates/11111111-2222-3333-4444-555555555555" $ha '{bad'
Req 'D5 malformed PATCH rate active'    'PATCH' "$B/v1/admin/shipping-services/post/rates/11111111-2222-3333-4444-555555555555/active" $ha '{bad'
Req 'D6 malformed PATCH grant active'   'PATCH' "$B/v1/admin/shipping-services/post/coverage-grants/11111111-2222-3333-4444-555555555555/active" $ha '{bad'
Req 'D7 malformed POST rates, no actor' 'POST' "$B/v1/admin/shipping-services/post/rates" $null '{bad'
Emit ""

Emit "--- 3. MISSION A: 063/064 missing-scope behaviour unchanged ---"
Req 'S1 POST rates without scope'        'POST' "$B/v1/admin/shipping-services/post/rates" $ha '{"chargeAmount":25000,"currencyCode":"IRR","preparationDays":2,"leadDays":3}'
Req 'S2 POST coverage-grants {}'         'POST' "$B/v1/admin/shipping-services/post/coverage-grants" $ha '{}'
Emit ""

Emit "--- 4. MISSION D: fail-closed coverage on an UNTOUCHED service (before any fixture) ---"
Req 'F1 GET rates in_person (untouched)' 'GET' "$B/v1/admin/shipping-services/in_person/rates" $ha $null
Req 'F2 GET rates tipax (untouched)'     'GET' "$B/v1/admin/shipping-services/tipax/rates" $ha $null
Emit ""

Emit "--- 5. MISSION B: real Geography IDs from the existing Development DB ---"
$countries = (Invoke-WebRequest -Uri "$B/v1/admin/geography/countries" -Headers $ha -UseBasicParsing -TimeoutSec 25).Content | ConvertFrom-Json
$countryId = if ($countries -is [array]) { $countries[0].id } else { $countries.id }
Emit "COUNTRY_ID=$countryId CODE=$($countries[0].code)"
$areas = (Invoke-WebRequest -Uri "$B/v1/admin/geography/countries/$countryId/administrative-areas" -Headers $ha -UseBasicParsing -TimeoutSec 25).Content | ConvertFrom-Json
$areaId = if ($areas -is [array]) { $areas[0].id } else { $areas.id }
Emit "AREA_ID=$areaId CODE=$($areas[0].code) (count=$($areas.Count))"
$cities = (Invoke-WebRequest -Uri "$B/v1/admin/geography/countries/$countryId/cities?administrativeAreaId=$areaId" -Headers $ha -UseBasicParsing -TimeoutSec 25).Content | ConvertFrom-Json
$cityId = if ($cities -is [array]) { $cities[0].id } else { $cities.id }
$cityCode = if ($cities -is [array]) { $cities[0].code } else { $cities.code }
Emit "CITY_ID=$cityId CODE=$cityCode (count=$($cities.Count)) — CITY-LEVEL scope is the TEST fixture marker"
Emit ""

Emit "--- 6. MISSION B: TEST rate + TEST grant create / read / edit / toggle on service 'post' ---"
$rateBody = '{"serviceCode":"post","scope":{"countryId":"' + $countryId + '","administrativeAreaId":"' + $areaId + '","cityId":"' + $cityId + '"},"chargeAmount":65000,"currencyCode":"IRR","preparationDays":2,"leadDays":4,"freeAboveThreshold":900000}'
$rateRaw = Req 'T1 create TEST rate (city-scope fixture)' 'POST' "$B/v1/admin/shipping-services/post/rates" $ha $rateBody
$rateId = $null
if ($rateRaw) { try { $rateId = ($rateRaw | ConvertFrom-Json).ratePolicyId } catch {} }
Emit "FIXTURE_RATE_POLICY_ID=$rateId"
Req 'T2 read-after-write rates'  'GET' "$B/v1/admin/shipping-services/post/rates" $ha $null
if ($rateId) {
    $editBody = '{"serviceCode":"post","scope":{"countryId":"' + $countryId + '","administrativeAreaId":"' + $areaId + '","cityId":"' + $cityId + '"},"chargeAmount":71000,"currencyCode":"IRR","preparationDays":3,"leadDays":5,"freeAboveThreshold":950000}'
    Req 'T3 edit TEST rate (PUT)'        'PUT'   "$B/v1/admin/shipping-services/post/rates/$rateId" $ha $editBody
    Req 'T4 toggle TEST rate inactive'   'PATCH' "$B/v1/admin/shipping-services/post/rates/$rateId/active" $ha '{"isActive":false}'
    Req 'T5 toggle TEST rate active'     'PATCH' "$B/v1/admin/shipping-services/post/rates/$rateId/active" $ha '{"isActive":true}'
}
$grantBody = '{"serviceCode":"post","scope":{"countryId":"' + $countryId + '","administrativeAreaId":"' + $areaId + '","cityId":"' + $cityId + '"}}'
$grantRaw = Req 'T6 create TEST coverage grant (city-scope fixture)' 'POST' "$B/v1/admin/shipping-services/post/coverage-grants" $ha $grantBody
$grantId = $null
if ($grantRaw) { try { $grantId = ($grantRaw | ConvertFrom-Json).coverageGrantId } catch {} }
Emit "FIXTURE_COVERAGE_GRANT_ID=$grantId"
if ($grantId) {
    Req 'T7 toggle TEST grant inactive' 'PATCH' "$B/v1/admin/shipping-services/post/coverage-grants/$grantId/active" $ha '{"isActive":false}'
    Req 'T8 toggle TEST grant active'   'PATCH' "$B/v1/admin/shipping-services/post/coverage-grants/$grantId/active" $ha '{"isActive":true}'
}
Req 'T9 final read-after-write' 'GET' "$B/v1/admin/shipping-services/post/rates" $ha $null
Emit ""

Emit "--- 7. MISSION D: cross-service guard + duplicate scope (unchanged semantics) ---"
if ($rateId) {
    Req 'G1 cross-service mutation must fail' 'PATCH' "$B/v1/admin/shipping-services/tipax/rates/$rateId/active" $ha '{"isActive":false}'
}
Req 'G2 duplicate city scope must fail' 'POST' "$B/v1/admin/shipping-services/post/rates" $ha $rateBody
Emit ""
Emit "FIXTURE_IDS_FOR_CLEANUP|ratePolicyId=$rateId|coverageGrantId=$grantId|service=post|scope=city/$cityCode"

Set-Content -Path $log -Value ($out -join "`r`n") -Encoding UTF8
Write-Host "WROTE $log"
