$ErrorActionPreference = 'Continue'
$Base = 'http://127.0.0.1:5088'
$log = Join-Path $PSScriptRoot 'log-tier-boundary-and-cleanup.txt'
$out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { $script:out.Add($s); Write-Host $s }

$IR     = 'e3451206-e503-51a8-91ad-afa9ab70e330'
$ALBORZ = '1f8050a8-c325-59fa-959f-82d76b5b2bc5'
$KARAJ  = '68255340-c69f-5d20-bf33-e8e0b24edde9'
$TIER_A = '01a13efc-347b-7000-a77a-384c96ad2cdd'
$TIER_B = '01a13efc-34a3-7000-bff6-d19a2502265a'
$PACK   = '01a13efc-34bb-7000-aae3-bab08995cb1b'

$login = Invoke-RestMethod -Uri "$Base/v1/auth/login" -Method POST -ContentType 'application/json' `
    -Body '{"identifierKind":"Email","identifier":"admin-actor@tooba.local","password":"admin-dev-horse-1"}' -TimeoutSec 30
$AH = @{ 'Authorization' = "Bearer $($login.accessToken)"; 'Accept-Language' = 'fa' }

function Call($method, $path, $body) {
    $p = @{ Uri = "$Base$path"; Method = $method; Headers = $AH; UseBasicParsing = $true; TimeoutSec = 30 }
    if ($null -ne $body) { $p['Body'] = ($body | ConvertTo-Json -Depth 8 -Compress); $p['ContentType'] = 'application/json' }
    try { $r = Invoke-WebRequest @p; return [pscustomobject]@{ Status = $r.StatusCode; Body = $r.Content } }
    catch {
        $resp = $_.Exception.Response
        if ($resp) { $sr = New-Object System.IO.StreamReader($resp.GetResponseStream()); return [pscustomobject]@{ Status = [int]$resp.StatusCode; Body = $sr.ReadToEnd() } }
        return [pscustomobject]@{ Status = -1; Body = $_.Exception.Message }
    }
}

Emit "=== TB-SHIPPING-FULL-OPERATIONS-079 :: live tier boundary 1999/2000 + packaging/COD + cleanup : $(Get-Date -Format o) ==="
Emit "task-owned fixtures: tier A $TIER_A [0,2000)=75000, tier B $TIER_B [2000,5000)=95000, packaging $PACK fee=25000 CollectOnDelivery (all at City level IR/alborz/karaj)"
Emit ""

Emit "--- the half-open tier boundary through the AUTHORIZED admin diagnostic route ---"
foreach ($grams in 1999, 2000) {
    $q = Call 'POST' '/v1/admin/shipping-services/store_courier/operational-quote' @{
        scope = @{ countryId = $IR; administrativeAreaId = $ALBORZ; cityId = $KARAJ }
        cartSubtotalExclusive = 500000
        currencyCode = 'IRR'
        weightGrams = $grams
        itemCount = 2
    }
    $b = ($q.Body | ConvertFrom-Json)
    Emit "weightGrams=$grams -> HTTP $($q.Status) outcome=$($b.outcome) freight=$($b.freightAmount) packaging=$($b.packagingAmount) total=$($b.totalServiceExpense) checkoutDue=$($b.amountPayableAtCheckout) codDue=$($b.amountCollectOnDelivery) payer=$($b.freightPayerMode) packagingPayer=$($b.packagingPayer) matchedWeight=$(($b.matchedCondition.weightGrams | ConvertTo-Json -Compress)) carrierReason=$($b.carrierFreight.reasonCode) packagingReason=$($b.packaging.reasonCode) capacity=$($b.schedule.capacityEnforcement)"
}
Emit ""

Emit "--- the same identity WITHOUT a measurement (fail-closed, no fabricated tier) ---"
$nm = Call 'POST' '/v1/admin/shipping-services/store_courier/operational-quote' @{
    scope = @{ countryId = $IR; administrativeAreaId = $ALBORZ; cityId = $KARAJ }
    cartSubtotalExclusive = 500000
    currencyCode = 'IRR'
    itemCount = 2
}
$nb = ($nm.Body | ConvertFrom-Json)
Emit "no weight -> HTTP $($nm.Status) outcome=$($nb.outcome) freight=$($nb.freightAmount) packaging=$($nb.packagingAmount) total=$($nb.totalServiceExpense) reason=$($nb.reasonCode) carrierReason=$($nb.carrierFreight.reasonCode)"
Emit ""

Emit "--- cleanup: deactivate every task-owned fixture ---"
$offA = Call 'PATCH' "/v1/admin/shipping-services/store_courier/rates/$TIER_A/active" @{ isActive = $false }
Emit "deactivate tier A -> $($offA.Status) $($offA.Body)"
$offB = Call 'PATCH' "/v1/admin/shipping-services/store_courier/rates/$TIER_B/active" @{ isActive = $false }
Emit "deactivate tier B -> $($offB.Status) $($offB.Body)"
$offP = Call 'PATCH' "/v1/admin/shipping-services/store_courier/packaging-policies/$PACK/active" @{ isActive = $false }
Emit "deactivate packaging -> $($offP.Status) $($offP.Body)"
Emit ""

Emit "--- after cleanup (every task-owned row inactive; the pre-existing area-level rule untouched) ---"
$ratesAfter = (Call 'GET' '/v1/admin/shipping-services/store_courier/rates' $null).Body
Emit "rates after: $ratesAfter"
$packAfter = (Call 'GET' '/v1/admin/shipping-services/store_courier/packaging-policies' $null).Body
Emit "packaging after: $packAfter"

Emit ""
Emit "--- post-cleanup re-check: the diagnostic answer is the pre-existing area-level rule again ---"
$rc = Call 'POST' '/v1/admin/shipping-services/store_courier/operational-quote' @{
    scope = @{ countryId = $IR; administrativeAreaId = $ALBORZ; cityId = $KARAJ }
    cartSubtotalExclusive = 500000
    currencyCode = 'IRR'
    weightGrams = 1999
    itemCount = 2
}
$rb = ($rc.Body | ConvertFrom-Json)
Emit "weightGrams=1999 -> outcome=$($rb.outcome) freight=$($rb.freightAmount) packaging=$($rb.packagingAmount) matched=$(($rb.matchedCondition | ConvertTo-Json -Compress)) carrierReason=$($rb.carrierFreight.reasonCode)"

$out -join "`r`n" | Set-Content $log -Encoding UTF8
Write-Host "WROTE $log"
