$ErrorActionPreference = 'Continue'
$B = 'http://127.0.0.1:5100'
$log = "$PSScriptRoot\log-crud-063.txt"
$script:out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { $script:out.Add($s); Write-Host $s }
function Req($name, $m, $u, $h, $b) {
    try {
        $p = @{ Uri = $u; Method = $m; UseBasicParsing = $true; TimeoutSec = 30 }
        if ($h) { $p.Headers = $h }
        if ($b) { $p.Body = $b; $p.ContentType = 'application/json' }
        $r = Invoke-WebRequest @p
        $body = $r.Content
        if ($body.Length -gt 700) { $body = $body.Substring(0, 700) + '...<truncated>' }
        Emit "$name|STATUS=$($r.StatusCode)|BODY=$body"
        return $r.Content
    } catch {
        $resp = $_.Exception.Response
        if ($resp) {
            $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $t = $sr.ReadToEnd()
            if ($t.Length -gt 700) { $t = $t.Substring(0, 700) + '...<truncated>' }
            Emit "$name|STATUS=$([int]$resp.StatusCode)|BODY=$t"
        } else { Emit "$name|STATUS=TRANSPORT_ERROR|BODY=$($_.Exception.Message)" }
        return $null
    }
}

Emit "=== TB-SHIPPING-MVP-SCRATCH-HTTP-063 authenticated scratch CRUD roundtrip ==="
Emit "ALL VALUES BELOW ARE SYNTHETIC AND SCRATCH-ONLY. They are NOT live tariffs and NOT checkout parity evidence."
Emit ""

$A = ((Invoke-WebRequest -Uri "$B/v1/admin/dev-context" -UseBasicParsing -TimeoutSec 20).Content | ConvertFrom-Json).actorUserId
$ha = @{ 'X-Tooba-Dev-Actor-User-Id' = $A }
Emit "ADMIN_ACTOR=$A"

$countries = (Invoke-WebRequest -Uri "$B/v1/admin/geography/countries" -Headers $ha -UseBasicParsing -TimeoutSec 25).Content | ConvertFrom-Json
Emit "GEOGRAPHY_COUNTRIES_AFTER_SEED=$($countries | ConvertTo-Json -Compress -Depth 4)"
$countryId = $null
if ($countries -is [array]) { $countryId = $countries[0].id } else { $countryId = $countries.id }
Emit "COUNTRY_ID=$countryId"
Emit ""

$svc = 'svc-063'
$rateBody = "{`"serviceCode`":`"$svc`",`"scope`":{`"countryId`":`"$countryId`"}}"
$rateBody = '{"serviceCode":"' + $svc + '","scope":{"countryId":"' + $countryId + '"},"chargeAmount":25000,"currencyCode":"IRR","preparationDays":2,"leadDays":3,"isActive":true}'

Emit "--- 1. CREATE rate (valid real geography id) ---"
$createRaw = Req 'R1 POST rate valid scope' 'POST' "$B/v1/admin/shipping-services/$svc/rates" $ha $rateBody
$rateId = $null
if ($createRaw) { try { $rateId = ($createRaw | ConvertFrom-Json).ratePolicyId } catch {} }
Emit "RATE_ID=$rateId"
Emit ""

Emit "--- 2. READ AFTER WRITE ---"
Req 'R2 GET rates (read-after-write)' 'GET' "$B/v1/admin/shipping-services/$svc/rates" $ha $null
Emit ""

Emit "--- 3. UPDATE (edit) + ACTIVE TOGGLE ---"
if ($rateId) {
    $edit = '{"serviceCode":"' + $svc + '","scope":{"countryId":"' + $countryId + '"},"chargeAmount":31000,"currencyCode":"IRR","preparationDays":3,"leadDays":4,"isActive":true}'
    Req 'R3 PUT rate edit' 'PUT' "$B/v1/admin/shipping-services/$svc/rates/$rateId" $ha $edit
    Req 'R4 PATCH rate active=false' 'PATCH' "$B/v1/admin/shipping-services/$svc/rates/$rateId/active" $ha '{"isActive":false}'
    Req 'R5 PATCH rate active=true' 'PATCH' "$B/v1/admin/shipping-services/$svc/rates/$rateId/active" $ha '{"isActive":true}'
} else { Emit 'R3/R4/R5 SKIPPED (no rate id)' }
Emit ""

Emit "--- 4. COVERAGE GRANT create + read + toggle ---"
$grantBody = '{"serviceCode":"' + $svc + '","scope":{"countryId":"' + $countryId + '"}}'
$grantRaw = Req 'G1 POST grant valid scope' 'POST' "$B/v1/admin/shipping-services/$svc/coverage-grants" $ha $grantBody
$grantId = $null
if ($grantRaw) { try { $grantId = ($grantRaw | ConvertFrom-Json).coverageGrantId } catch {} }
Emit "GRANT_ID=$grantId"
Req 'G2 GET rates+coverage (read-after-write)' 'GET' "$B/v1/admin/shipping-services/$svc/rates" $ha $null
if ($grantId) {
    Req 'G3 PATCH grant active=false' 'PATCH' "$B/v1/admin/shipping-services/$svc/coverage-grants/$grantId/active" $ha '{"isActive":false}'
    Req 'G4 PATCH grant active=true' 'PATCH' "$B/v1/admin/shipping-services/$svc/coverage-grants/$grantId/active" $ha '{"isActive":true}'
} else { Emit 'G3/G4 SKIPPED (no grant id)' }
Emit ""

Emit "--- 5. CROSS-SERVICE / DUPLICATE GUARDS ---"
Req 'X1 duplicate rate create (same scope)' 'POST' "$B/v1/admin/shipping-services/$svc/rates" $ha $rateBody
if ($rateId) { Req 'X2 cross-service mutation of the svc-063 rate' 'PATCH' "$B/v1/admin/shipping-services/svc-063-other/rates/$rateId/active" $ha '{"isActive":false}' }
Req 'X3 bogus serviceCode in body vs route' 'POST' "$B/v1/admin/shipping-services/$svc/rates" $ha ($rateBody -replace '"serviceCode":"' + $svc + '"', '"serviceCode":"totally-different"')
Emit ""

Emit "--- 6. EMPTY COVERAGE SET IS STILL NOT COVERED (a different, never-granted service) ---"
Req 'N1 GET an untouched service (must be empty, never a default)' 'GET' "$B/v1/admin/shipping-services/never-touched-063/rates" $ha $null
Emit ""
Emit "--- 7. malformed-body behavior (observation, not fixed here) ---"
Req 'S1 malformed JSON body to a POST route' 'POST' "$B/v1/admin/shipping-services/$svc/rates" $ha '{not-json'
Req 'S2 empty-string geography id in scope' 'POST' "$B/v1/admin/shipping-services/$svc/rates" $ha '{"serviceCode":"svc-063","scope":{"countryId":""},"chargeAmount":1000,"currencyCode":"IRR","preparationDays":1,"leadDays":1,"isActive":true}'

Set-Content -Path $log -Value ($script:out -join "`r`n") -Encoding UTF8
Write-Host "WROTE $log"
