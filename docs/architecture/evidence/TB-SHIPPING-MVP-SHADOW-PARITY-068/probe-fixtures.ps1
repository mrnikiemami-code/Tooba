$ErrorActionPreference = 'Continue'
$Base = 'http://127.0.0.1:5088'
$A = '01a036c2-970e-7000-8eb7-94bf5cc2d8db'
$ha = @{ 'X-Tooba-Dev-Actor-User-Id' = $A }
$out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { $script:out.Add($s); Write-Host $s }

$IR        = 'e3451206-e503-51a8-91ad-afa9ab70e330'
$ALBORZ    = '1f8050a8-c325-59fa-959f-82d76b5b2bc5'
$KARAJ     = '68255340-c69f-5d20-bf33-e8e0b24edde9'

function Post($name, $url, $body) {
    try {
        $r = Invoke-WebRequest -Uri $url -Method POST -Headers $ha -ContentType 'application/json' -Body $body -UseBasicParsing -TimeoutSec 30
        Emit "$name => $($r.StatusCode) $($r.Content)"
        return $r.Content
    } catch {
        $resp = $_.Exception.Response
        if ($resp) { $sr = New-Object System.IO.StreamReader($resp.GetResponseStream()); Emit "$name => $([int]$resp.StatusCode) $($sr.ReadToEnd())" }
        else { Emit "$name => ERR $($_.Exception.Message)" }
        return $null
    }
}

Emit "=== TB-SHIPPING-MVP-SHADOW-PARITY-068 :: SYNTHETIC DEVELOPMENT-ONLY parity fixtures (mission B) ==="
Emit "target: Development host :5088, tenant store-alpha, DB tooba_alpha (tenant-alpha connection reference)"
Emit "scope: country IR $IR / administrative area alborz $ALBORZ  (area level, NOT the 065 city-level rows)"
Emit "representative city for the projection matrix: karaj $KARAJ"
Emit ""
Emit "These amounts are TEST FIXTURES chosen to reproduce the MEASURED legacy amounts so parity can be"
Emit "compared. They are NOT user-approved commercial tariffs and must never be described as such."
Emit ""

$fixtures = @(
    @{ service = 'post';          charge = 120000; prep = 1; lead = 4; why = 'mirrors measured legacy post:standard (base 120000, lead 4)' },
    @{ service = 'store_courier'; charge = 75000;  prep = 1; lead = 1; why = 'mirrors measured legacy store_courier (base 75000, lead 1)' },
    @{ service = 'in_person';     charge = 0;      prep = 1; lead = 0; why = 'mirrors measured legacy in_person (base 0, lead 0)' },
    @{ service = 'tipax';         charge = 180000; prep = 1; lead = 3; why = 'mirrors measured legacy tipax:standard; tipax:express (260000) cannot match by construction' }
)

Emit "--- rate policies (POST /v1/admin/shipping-services/{service}/rates) ---"
foreach ($f in $fixtures) {
    Emit "fixture $($f.service): $($f.why)"
    $body = '{"serviceCode":"' + $f.service + '","scope":{"countryId":"' + $IR + '","administrativeAreaId":"' + $ALBORZ + '"},"chargeAmount":' + $f.charge + ',"currencyCode":"IRR","preparationDays":' + $f.prep + ',"leadDays":' + $f.lead + '}'
    $raw = Post "  CREATE rate $($f.service)" "$Base/v1/admin/shipping-services/$($f.service)/rates" $body
    if ($raw) {
        try {
            $j = $raw | ConvertFrom-Json
            $idProp = $j.PSObject.Properties['ratePolicyId']
            if ($idProp) { Emit "  FIXTURE_RATE_$($f.service.ToUpper())_ID=$($idProp.Value)" }
        } catch {}
    }
}
Emit ""

Emit "--- coverage grants (POST /v1/admin/shipping-services/{service}/coverage-grants) ---"
foreach ($f in $fixtures) {
    $body = '{"serviceCode":"' + $f.service + '","scope":{"countryId":"' + $IR + '","administrativeAreaId":"' + $ALBORZ + '"}}'
    $raw = Post "  CREATE grant $($f.service)" "$Base/v1/admin/shipping-services/$($f.service)/coverage-grants" $body
    if ($raw) {
        try {
            $j = $raw | ConvertFrom-Json
            $idProp = $j.PSObject.Properties['coverageGrantId']
            if ($idProp) { Emit "  FIXTURE_GRANT_$($f.service.ToUpper())_ID=$($idProp.Value)" }
        } catch {}
    }
}
Emit ""

Emit "--- AFTER counts (must show exactly one active area-level policy+grant per fixture service) ---"
foreach ($s in @('post', 'tipax', 'store_courier', 'in_person', 'snapp_courier')) {
    try {
        $r = Invoke-WebRequest -Uri "$Base/v1/admin/shipping-services/$s/rates" -Headers $ha -UseBasicParsing -TimeoutSec 25
        $j = $r.Content | ConvertFrom-Json
        $rates = @($j.ratePolicies); $grants = @($j.coverageGrants)
        $activeRates = @($rates | Where-Object { $_.isActive }); $activeGrants = @($grants | Where-Object { $_.isActive })
        Emit "$s : ratePolicies=$($rates.Count) activeRates=$($activeRates.Count) coverageGrants=$($grants.Count) activeGrants=$($activeGrants.Count)"
        foreach ($x in $rates) { Emit ("   RATE  {0} level={1} area={2} city={3} charge={4} {5} prep={6} lead={7} active={8}" -f $x.ratePolicyId, $x.level, $x.administrativeAreaId, $x.cityId, $x.chargeAmount, $x.currencyCode, $x.preparationDays, $x.leadDays, $x.isActive) }
        foreach ($x in $grants) { Emit ("   GRANT {0} level={1} area={2} city={3} active={4}" -f $x.coverageGrantId, $x.level, $x.administrativeAreaId, $x.cityId, $x.isActive) }
    } catch { Emit "$s => ERR $($_.Exception.Message)" }
}
Emit ""
Emit "REVERSIBILITY: every fixture is a single area-level row. Deactivation (the strongest available state,"
Emit "because the Shipping admin surface exposes no delete route) is one PATCH per row:"
Emit "  PATCH /v1/admin/shipping-services/<service>/rates/<ratePolicyId>/active   body {\"isActive\":false}"
Emit "  PATCH /v1/admin/shipping-services/<service>/coverage-grants/<grantId>/active body {\"isActive\":false}"
Emit "An inactive policy/grant is not coverage and never quotes (engine filters on IsActive)."

$out -join "`r`n" | Set-Content "$PSScriptRoot\log-fixtures.txt" -Encoding UTF8
Write-Host "WROTE log-fixtures.txt"

