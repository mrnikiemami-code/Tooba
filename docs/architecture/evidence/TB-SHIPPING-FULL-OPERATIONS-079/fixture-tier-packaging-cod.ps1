$ErrorActionPreference = 'Continue'
$Base = 'http://127.0.0.1:5088'
$log = Join-Path $PSScriptRoot 'log-tier-packaging-cod-fixture.txt'
$out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { $script:out.Add($s); Write-Host $s }

$IR     = 'e3451206-e503-51a8-91ad-afa9ab70e330'
$ALBORZ = '1f8050a8-c325-59fa-959f-82d76b5b2bc5'
$KARAJ  = '68255340-c69f-5d20-bf33-e8e0b24edde9'

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

Emit "=== TB-SHIPPING-FULL-OPERATIONS-079 :: task-owned disposable tier/packaging/COD fixture : $(Get-Date -Format o) ==="
Emit "service=store_courier (single-amount, no options) ; scope = IR / alborz / karaj (CITY level, distinct from every pre-existing area-level row)"
Emit ""

Emit "--- baseline (before) ---"
$before = Call 'GET' '/v1/admin/shipping-services/store_courier/rates' $null
Emit "rates before: $($before.Body)"
$packBefore = Call 'GET' '/v1/admin/shipping-services/store_courier/packaging-policies' $null
Emit "packaging before: $($packBefore.Body)"
Emit ""

Emit "--- create the two half-open weight tiers (task-owned) ---"
$tierA = Call 'POST' '/v1/admin/shipping-services/store_courier/rates' @{
    serviceCode = 'store_courier'
    scope = @{ countryId = $IR; administrativeAreaId = $ALBORZ; cityId = $KARAJ }
    chargeAmount = 75000
    currencyCode = 'IRR'
    preparationDays = 1
    leadDays = 1
    condition = @{ weightGrams = @{ min = 0; max = 2000 } }
}
Emit "tier A [0,2000) -> 75000 : $($tierA.Status) $($tierA.Body)"
$tierB = Call 'POST' '/v1/admin/shipping-services/store_courier/rates' @{
    serviceCode = 'store_courier'
    scope = @{ countryId = $IR; administrativeAreaId = $ALBORZ; cityId = $KARAJ }
    chargeAmount = 95000
    currencyCode = 'IRR'
    preparationDays = 1
    leadDays = 1
    condition = @{ weightGrams = @{ min = 2000; max = 5000 } }
}
Emit "tier B [2000,5000) -> 95000 : $($tierB.Status) $($tierB.Body)"
Emit ""

Emit "--- create + activate a charged packaging policy with CollectOnDelivery (task-owned) ---"
$pack = Call 'POST' '/v1/admin/shipping-services/store_courier/packaging-policies' @{
    serviceCode = 'store_courier'
    scope = @{ countryId = $IR; administrativeAreaId = $ALBORZ; cityId = $KARAJ }
    feeAmount = 25000
    currencyCode = 'IRR'
    freightPayerMode = 1
}
Emit "packaging create: $($pack.Status) $($pack.Body)"
$packId = ($pack.Body | ConvertFrom-Json).data.packagingPolicyId
Emit "packagingPolicyId = $packId"
$packActive = Call 'PATCH' "/v1/admin/shipping-services/store_courier/packaging-policies/$packId/active" @{ isActive = $true }
Emit "packaging activate: $($packActive.Status) $($packActive.Body)"
Emit ""

Emit "--- the live tier boundary through the AUTHORIZED admin diagnostic route (1999g vs 2000g) ---"
foreach ($grams in 1999, 2000) {
    $q = Call 'POST' '/v1/admin/shipping-services/store_courier/operational-quote' @{
        scope = @{ countryId = $IR; administrativeAreaId = $ALBORZ; cityId = $KARAJ }
        cartSubtotalExclusive = 500000
        currencyCode = 'IRR'
        weightGrams = $grams
        itemCount = 2
    }
    $b = ($q.Body | ConvertFrom-Json).data
    Emit "weightGrams=$grams -> $($q.Status) outcome=$($b.outcome) freight=$($b.freightAmount) packaging=$($b.packagingAmount) total=$($b.totalServiceExpense) checkoutDue=$($b.amountPayableAtCheckout) codDue=$($b.amountCollectOnDelivery) payer=$($b.freightPayerMode) matchedWeight=$($b.matchedCondition.weightGrams | ConvertTo-Json -Compress) carrierReason=$($b.carrierFreight.reasonCode)"
}

Emit ""
Emit "--- cleanup: deactivate every task-owned fixture ---"
$after = Call 'GET' '/v1/admin/shipping-services/store_courier/rates' $null
$afterObj = ($after.Body | ConvertFrom-Json).data.ratePolicies
foreach ($row in $afterObj) {
    if ($row.ratePolicyId -ne '01a13d65-cf13-7000-b9bb-d73f95c9b65f' -and $row.isActive) {
        $off = Call 'PATCH' "/v1/admin/shipping-services/store_courier/rates/$($row.ratePolicyId)/active" @{ isActive = $false }
        Emit "deactivated tier $($row.ratePolicyId) [$($row.condition | ConvertTo-Json -Compress)] -> $($off.Status)"
    }
}
$packOff = Call 'PATCH' "/v1/admin/shipping-services/store_courier/packaging-policies/$packId/active" @{ isActive = $false }
Emit "deactivated packaging $packId -> $($packOff.Status)"

Emit ""
Emit "--- after cleanup ---"
Emit "rates after: $((Call 'GET' '/v1/admin/shipping-services/store_courier/rates' $null).Body)"
Emit "packaging after: $((Call 'GET' '/v1/admin/shipping-services/store_courier/packaging-policies' $null).Body)"

$out -join "`r`n" | Set-Content $log -Encoding UTF8
Write-Host "WROTE $log"
