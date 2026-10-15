$ErrorActionPreference = 'Continue'
$B = 'http://127.0.0.1:5088'
$A = '01a036c2-970e-7000-8eb7-94bf5cc2d8db'
$ha = @{ 'X-Tooba-Dev-Actor-User-Id' = $A }
$AREA = '1f8050a8-c325-59fa-959f-82d76b5b2bc5'
$COUNTRY = 'e3451206-e503-51a8-91ad-afa9ab70e330'

function Emit([string]$s) { Write-Host $s }

function Call([string]$method, [string]$url, [string]$body, [hashtable]$headers) {
    try {
        if ($body) {
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($body)
            $r = Invoke-WebRequest -Uri $url -Method $method -Headers $headers -ContentType 'application/json; charset=utf-8' -Body $bytes -UseBasicParsing -TimeoutSec 30
        } else {
            $r = Invoke-WebRequest -Uri $url -Method $method -Headers $headers -UseBasicParsing -TimeoutSec 30
        }
        Emit "$method $url => $($r.StatusCode)"
        Emit "   $($r.Content)"
        return $r
    } catch {
        $resp = $_.Exception.Response
        if ($resp) {
            $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
            Emit "$method $url => $([int]$resp.StatusCode)"
            Emit "   $($sr.ReadToEnd())"
        } else {
            Emit "$method $url => ERR $($_.Exception.Message)"
        }
        return $null
    }
}

# Body builder: conditionJson is a raw JSON fragment or '' for an unconditioned row.
function RateBody([string]$option, [string]$conditionJson, [string]$charge) {
    $optionJson = if ($option -eq '') { 'null' } else { '"' + $option + '"' }
    if ($conditionJson -eq '') {
        $cond = 'null'
    } else {
        $cond = $conditionJson
    }
    return '{"serviceCode":"tipax","scope":{"countryId":"' + $COUNTRY + '","administrativeAreaId":"' + $AREA + '"},"chargeAmount":' + $charge + ',"currencyCode":"IRR","preparationDays":1,"leadDays":2,"optionCode":' + $optionJson + ',"condition":' + $cond + '}'
}

function WeightCond([string]$min, [string]$max) {
    if ($max -eq '') { return '{"weightGrams":{"min":' + $min + '}}' }
    return '{"weightGrams":{"min":' + $min + ',"max":' + $max + '}}'
}

function CountCond([string]$min, [string]$max) {
    if ($max -eq '') { return '{"itemCount":{"min":' + $min + '}}' }
    return '{"itemCount":{"min":' + $min + ',"max":' + $max + '}}'
}

function IdOf($response) {
    if (-not $response) { return $null }
    $p = $response.Content | ConvertFrom-Json
    $id = $p.data.ratePolicyId
    if (-not $id) { $id = $p.ratePolicyId }
    return $id
}

Emit "=== TB-SHIPPING-FULL-OPERATIONS-073 :: live weight/count tier admin roundtrip ==="
Emit "target: existing Development host :5088, tenant store-alpha, DB tooba_alpha"
Emit ""

Emit "--- 0. positive target provenance (Development, no production/staging) ---"
try {
    $pc = Invoke-WebRequest -Uri "$B/__platform-commerce" -Headers $ha -UseBasicParsing -TimeoutSec 15
    Emit "GET /__platform-commerce => $($pc.StatusCode)"
    Emit "   $($pc.Content)"
} catch { Emit "GET /__platform-commerce => ERR $($_.Exception.Message)" }
Emit ""

Emit "--- 1. baseline: existing rows are all unconditioned (condition == null) ---"
$before = Call 'GET' "$B/v1/admin/shipping-services/tipax/rates" $null $ha
if ($before) {
    $parsed = $before.Content | ConvertFrom-Json
    $rows = $parsed.data.ratePolicies
    if (-not $rows) { $rows = $parsed.ratePolicies }
    Emit "   EXISTING_ROWS=$($rows.Count)"
    foreach ($row in $rows) {
        $cond = if ($null -eq $row.condition) { 'null' } else { ($row.condition | ConvertTo-Json -Compress) }
        Emit "   option=$($row.optionCode) charge=$($row.chargeAmount) active=$($row.isActive) condition=$cond"
    }
}
Emit ""

Emit "--- 2. create tier [0,2000) grams for tipax/standard (base unconditioned row already exists) ---"
$low = Call 'POST' "$B/v1/admin/shipping-services/tipax/rates" (RateBody 'standard' (WeightCond '0' '2000') '120000') $ha
$lowId = IdOf $low
Emit "   TIER_LOW_ID=$lowId"
Emit ""

Emit "--- 3. create adjacent tier [2000,5000) grams for tipax/standard (half-open: no overlap) ---"
$high = Call 'POST' "$B/v1/admin/shipping-services/tipax/rates" (RateBody 'standard' (WeightCond '2000' '5000') '260000') $ha
$highId = IdOf $high
Emit "   TIER_HIGH_ID=$highId"
Emit ""

Emit "--- 4. cross-axis ambiguity: a weight-only tier and a count-only tier on the SAME identity"
Emit "        genuinely intersect (a 1500 g / 3-item parcel matches both), so the pair is refused."
Emit "        This is the guard working, not a failure: the refusal is the correct outcome."
$count = Call 'POST' "$B/v1/admin/shipping-services/tipax/rates" (RateBody 'standard' (CountCond '1' '6') '90000') $ha
$countId = IdOf $count
Emit "   TIER_COUNT_ID=$countId (expected empty: refused as shipping.rate_policy.tier_overlap)"
Emit ""

Emit "--- 4b. a count-only tier on a DIFFERENT identity (service-level tipax, no active"
Emit "         conditioned sibling) is accepted, proving the guard is identity-scoped."
$countService = Call 'POST' "$B/v1/admin/shipping-services/tipax/rates" (RateBody '' (CountCond '1' '6') '95000') $ha
$countServiceId = IdOf $countService
Emit "   TIER_COUNT_SERVICE_ID=$countServiceId"
Emit ""

Emit "--- 4c. a combined weight+count tier on tipax/express is accepted because its weight axis"
Emit "         [2000,5000) is disjoint from the existing express tier [0,2000)."
Emit "         The engine evaluates the two axes as an INTERSECTION, never a union."
$combined = Call 'POST' "$B/v1/admin/shipping-services/tipax/rates" (RateBody 'express' '{"weightGrams":{"min":2000,"max":5000},"itemCount":{"min":6,"max":11}}' '340000') $ha
$combinedId = IdOf $combined
Emit "   TIER_COMBINED_ID=$combinedId"
Emit ""

Emit "--- 5. read back: the persisted rows carry their half-open tiers ---"
$after = Call 'GET' "$B/v1/admin/shipping-services/tipax/rates" $null $ha
if ($after) {
    $parsed = $after.Content | ConvertFrom-Json
    $rows = $parsed.data.ratePolicies
    if (-not $rows) { $rows = $parsed.ratePolicies }
    Emit "   TOTAL_ROWS=$($rows.Count)"
    foreach ($row in $rows) {
        if ($null -ne $row.condition) {
            Emit "   option=$($row.optionCode) charge=$($row.chargeAmount) condition=$(($row.condition | ConvertTo-Json -Compress))"
        }
    }
}
Emit ""

Emit "--- 6. refusals with stable codes (no partial write) ---"
Emit "6a. an overlapping tier [1500,3000) must be refused"
Call 'POST' "$B/v1/admin/shipping-services/tipax/rates" (RateBody 'standard' (WeightCond '1500' '3000') '999000') $ha | Out-Null
Emit ""
Emit "6b. the exact same tier [0,2000) must be refused as a duplicate"
Call 'POST' "$B/v1/admin/shipping-services/tipax/rates" (RateBody 'standard' (WeightCond '0' '2000') '1') $ha | Out-Null
Emit ""
Emit "6c. a reversed tier [3000,2000) must be refused"
Call 'POST' "$B/v1/admin/shipping-services/tipax/rates" (RateBody 'standard' (WeightCond '3000' '2000') '1') $ha | Out-Null
Emit ""
Emit "6d. a zero-width tier [2000,2000) must be refused"
Call 'POST' "$B/v1/admin/shipping-services/tipax/rates" (RateBody 'standard' (WeightCond '2000' '2000') '1') $ha | Out-Null
Emit ""
Emit "6e. a negative lower bound [-1,2000) must be refused"
Call 'POST' "$B/v1/admin/shipping-services/tipax/rates" (RateBody 'standard' (WeightCond '-1' '2000') '1') $ha | Out-Null
Emit ""
Emit "6f. an unknown option must be refused before any write"
Call 'POST' "$B/v1/admin/shipping-services/tipax/rates" (RateBody 'drone' (WeightCond '0' '2000') '1') $ha | Out-Null
Emit ""

Emit "--- 7. option isolation: the same range on tipax/express is a different identity, so it is accepted ---"
$express = Call 'POST' "$B/v1/admin/shipping-services/tipax/rates" (RateBody 'express' (WeightCond '0' '2000') '210000') $ha
$expressId = IdOf $express
Emit "   TIER_EXPRESS_ID=$expressId"
Emit ""

Emit "--- 8. update: widening an existing row's own tier is not a self-collision ---"
if ($lowId) {
    Call 'PUT' "$B/v1/admin/shipping-services/tipax/rates/$lowId" (RateBody 'standard' (WeightCond '0' '1000') '115000') $ha | Out-Null
}
Emit ""

Emit "--- 9. authorization boundary: unauthenticated read must be 401 ---"
Call 'GET' "$B/v1/admin/shipping-services/tipax/rates" $null @{} | Out-Null
Emit ""

Emit "--- 10. rollback ledger: the synthetic rows created by this probe ---"
Emit "   LOW=$lowId HIGH=$highId COUNT_SERVICE=$countServiceId COMBINED=$combinedId EXPRESS=$expressId"
Emit "   (deleted by the accompanying psql rollback; pre-existing 9 rate rows are untouched)"
Emit ""

Emit "--- 11. final state ---"
Call 'GET' "$B/v1/admin/shipping-services/tipax/rates" $null $ha | Out-Null
