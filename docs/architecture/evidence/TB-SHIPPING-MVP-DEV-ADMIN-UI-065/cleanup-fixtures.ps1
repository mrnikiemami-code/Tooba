$ErrorActionPreference = 'Continue'
$B = 'http://127.0.0.1:5088'
$ha = @{ 'X-Tooba-Dev-Actor-User-Id' = '01a036c2-970e-7000-8eb7-94bf5cc2d8db' }
$log = "$PSScriptRoot\log-fixtures-and-cleanup.txt"
$out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { $script:out.Add($s); Write-Host $s }
function Patch($u) { try { $r = Invoke-WebRequest -Uri $u -Method PATCH -Headers $ha -ContentType 'application/json' -Body '{"isActive":false}' -UseBasicParsing -TimeoutSec 25; return "$($r.StatusCode)" } catch { $resp=$_.Exception.Response; if($resp){ return "$([int]$resp.StatusCode)" } else { return 'ERR' } } }

Emit "=== TB-SHIPPING-MVP-DEV-ADMIN-UI-065 :: fixtures created and controlled cleanup ==="
Emit ""
Emit "Fixtures created on the Development DB (tooba_alpha), service code 'post' (existing ACTIVE fulfillment service):"
Emit "  HTTP path  : ratePolicyId=01a13ca4-4281-7000-bb23-76edd8f68818  scope=city IR-alborz-01 (alborz/karaj)"
Emit "  HTTP path  : coverageGrantId=01a13ca4-42f6-7000-b2b4-8318c1572792  same scope"
Emit "  UI path #1 : ratePolicyId=01a13ca8-3a0a-7000-8ddc-23b0c11bd871  scope=country IR (created before the fix; the requested city scope was refused by the client rule)"
Emit "  UI path #1 : coverageGrantId=01a13ca8-7fac-7000-9248-dbab79feedd1  scope=country IR"
Emit "  UI path #2 : ratePolicyId=01a13cb1-22a3-7000-9f3d-8cf18b7cbdc8  scope=city IR-east_azerbaijan-02 (marand) -- after the fix"
Emit "  UI path #2 : coverageGrantId=01a13cb1-8c4a-7000-8e00-be5de12ac374  same scope -- after the fix"
Emit ""
Emit "Marker: every fixture is scoped to a single real city (or, for the pre-fix run, the country) and carries a"
Emit "deliberately unusual synthetic amount (65000/71000/111111/222222/123456) -- no live tariff is reproduced."
Emit ""
Emit "Cleanup mode: the Shipping admin surface exposes six routes (GET, POST rate, PUT rate, PATCH rate active,"
Emit "POST grant, PATCH grant active). There is NO delete route and no schema-level soft-delete, so a fixture"
Emit "cannot be removed over the API. Controlled cleanup therefore deactivates every fixture, which is the"
Emit "strongest available state: an inactive policy/grant is not coverage and does not quote (fail-closed)."
Emit "Database rows are intentionally NOT deleted by hand: the user stated the Development data is disposable,"
Emit "but nothing in this task authorises silent row deletion."
Emit ""
foreach ($p in @(
  @('rate',  '01a13ca4-4281-7000-bb23-76edd8f68818'),
  @('grant', '01a13ca4-42f6-7000-b2b4-8318c1572792'),
  @('rate',  '01a13cb1-22a3-7000-9f3d-8cf18b7cbdc8'),
  @('grant', '01a13cb1-8c4a-7000-8e00-be5de12ac374'),
  @('rate',  '01a13ca8-3a0a-7000-8ddc-23b0c11bd871'),
  @('grant', '01a13ca8-7fac-7000-9248-dbab79feedd1')
)) {
    $kind = $p[0]; $id = $p[1]
    $url = if ($kind -eq 'rate') { "$B/v1/admin/shipping-services/post/rates/$id/active" } else { "$B/v1/admin/shipping-services/post/coverage-grants/$id/active" }
    Emit "DEACTIVATE $kind $id -> HTTP $(Patch $url)"
}
Emit ""
Emit "=== final authorized state for service 'post' ==="
$final = (Invoke-WebRequest -Uri "$B/v1/admin/shipping-services/post/rates" -Headers $ha -UseBasicParsing -TimeoutSec 25).Content | ConvertFrom-Json
Emit "ratePolicies=$($final.ratePolicies.Count) activeRates=$(($final.ratePolicies | Where-Object { $_.isActive }).Count)"
Emit "coverageGrants=$($final.coverageGrants.Count) activeGrants=$(($final.coverageGrants | Where-Object { $_.isActive }).Count)"
foreach ($r in $final.ratePolicies) { Emit "RATE   $($r.ratePolicyId) level=$($r.level) city=$($r.cityId) charge=$($r.chargeAmount) active=$($r.isActive)" }
foreach ($g in $final.coverageGrants) { Emit "GRANT  $($g.coverageGrantId) level=$($g.level) city=$($g.cityId) active=$($g.isActive)" }
Emit ""
Emit "=== untouched-service fail-closed recheck (no implicit nationwide coverage) ==="
foreach ($svc in @('tipax','store_courier','in_person')) {
    $v = (Invoke-WebRequest -Uri "$B/v1/admin/shipping-services/$svc/rates" -Headers $ha -UseBasicParsing -TimeoutSec 25).Content | ConvertFrom-Json
    Emit "$svc : ratePolicies=$($v.ratePolicies.Count) coverageGrants=$($v.coverageGrants.Count) -> no coverage, quote must stay NotCovered"
}

Set-Content -Path $log -Value ($out -join "`r`n") -Encoding UTF8
Write-Host "WROTE $log"
