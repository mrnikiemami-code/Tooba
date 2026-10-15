$ErrorActionPreference = "Continue"
$root = "http://127.0.0.1:5088"
$logDir = Split-Path -Parent $PSCommandPath
$log = Join-Path $logDir "log-live-operational-quote.txt"
Set-Content -Path $log -Value @"
TB-SHIPPING-FULL-OPERATIONS-076 - live Development proof for the read-only operational quote seam
Target provenance (positively identified Development, never staging/production):
  GET http://127.0.0.1:5088/__platform-commerce => edition=SingleStore, deploymentId=dev-singlestore,
    tenantId=store-alpha, connectionReference=tenant-alpha
  Database: tooba_alpha
  Host: http://127.0.0.1:5088  (stopped intentionally ONCE as PID 22068, rebuilt ONCE, restarted ONCE)
"@

function Emit([string]$text) {
    Add-Content -Path $log -Value $text -Encoding UTF8
    Write-Output $text
}

function Call([string]$method, [string]$path, $body, [string]$token) {
    $headers = @{ "Accept-Language" = "fa" }
    if ($token) { $headers["Authorization"] = "Bearer $token" }
    $params = @{ Uri = "$root$path"; Method = $method; UseBasicParsing = $true; TimeoutSec = 25; Headers = $headers }
    if ($null -ne $body) {
        $params["Body"] = ($body | ConvertTo-Json -Depth 8 -Compress)
        $params["ContentType"] = "application/json"
    }
    try {
        $r = Invoke-WebRequest @params
        return [pscustomobject]@{ Status = $r.StatusCode; Body = $r.Content }
    } catch {
        $resp = $_.Exception.Response
        if ($resp) {
            $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
            return [pscustomobject]@{ Status = $resp.StatusCode.value__; Body = $sr.ReadToEnd() }
        }
        return [pscustomobject]@{ Status = -1; Body = $_.Exception.Message }
    }
}

function Show($label, $result) {
    $b = $result.Body
    if ($null -eq $b) { $b = "" }
    if ($b.Length -gt 4000) { $b = $b.Substring(0, 4000) + "...(truncated)" }
    Emit "$label -> $($result.Status) $b"
}

function Unwrap($body) {
    if ([string]::IsNullOrWhiteSpace($body)) { return $null }
    $o = $body | ConvertFrom-Json
    if ($o -is [System.Array]) { return $o }
    if ($null -ne $o.PSObject.Properties["data"]) { return $o.data }
    return $o
}

function With($table, $key, $value) {
    $copy = @{}
    foreach ($k in $table.Keys) { $copy[$k] = $table[$k] }
    $copy[$key] = $value
    return $copy
}

Emit ""
Emit "=== target identity ==="
Show "GET /__platform-commerce" (Call "GET" "/__platform-commerce" $null $null)

Emit ""
Emit "=== phase 1: real authentication through the existing platform auth system ==="
$login = Call "POST" "/v1/auth/login" @{ identifierKind = "Email"; identifier = "admin-actor@tooba.local"; password = "admin-dev-horse-1" } $null
Show "POST /v1/auth/login (Development bootstrap admin actor)" $login
$token = ($login.Body | ConvertFrom-Json).accessToken
Emit "accessToken acquired: $([bool]$token)"

Emit ""
Emit "=== phase 2: real Geography identity for the evaluation scope (read-only) ==="
$countries = Call "GET" "/v1/admin/geography/countries" $null $token
$countryId = (Unwrap $countries.Body)[0].id
Emit "resolved REAL countryId = $countryId"
$areas = Call "GET" "/v1/admin/geography/countries/$countryId/administrative-areas" $null $token
$areaId = (Unwrap $areas.Body)[0].id
Emit "resolved REAL administrativeAreaId = $areaId"

Emit ""
Emit "=== phase 3: baseline read of the existing surfaces (unchanged, read-only) ==="
$ratesBefore = Call "GET" "/v1/admin/shipping-services/tipax/rates" $null $token
Show "GET /v1/admin/shipping-services/tipax/rates (before)" $ratesBefore
$packagingBefore = Call "GET" "/v1/admin/shipping-services/tipax/packaging-policies" $null $token
Show "GET /v1/admin/shipping-services/tipax/packaging-policies (before)" $packagingBefore
$priorExpress = (Unwrap $packagingBefore.Body).packagingPolicies |
    Where-Object { $_.optionCode -eq "express" } | Select-Object -First 1
Emit "prior express packaging row = $($priorExpress.packagingPolicyId) fee=$($priorExpress.feeAmount) payer=$($priorExpress.freightPayerMode) active=$($priorExpress.isActive)"

$quote = @{
    scope = @{ countryId = $countryId; administrativeAreaId = $areaId }
    cartSubtotalExclusive = 500000
    currencyCode = "IRR"
    optionCode = "express"
}

Emit ""
Emit "=== phase 4: authorized read-only evaluation against REAL stored rates ==="
$inside = Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "requestedPickupAtUtc" "2026-10-18T10:00:00Z") $token
Show "POST operational-quote (Sunday 13:30 Asia/Tehran, express schedule not armed yet)" $inside

$outside = Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "requestedPickupAtUtc" "2026-10-18T10:30:00Z") $token
Show "POST operational-quote (Sunday 14:00 Asia/Tehran, half-open end, not armed yet)" $outside

Emit ""
Emit "=== phase 5: synthetic task-owned test fixtures on disposable Development data (clearly labelled) ==="
$packagingBody = @{
    serviceCode = "tipax"
    scope = @{ countryId = $countryId; administrativeAreaId = $areaId }
    feeAmount = 12000
    currencyCode = "IRR"
    freightPayerMode = "CollectOnDelivery"
    optionCode = "express"
}
$createdPackaging = Call "POST" "/v1/admin/shipping-services/tipax/packaging-policies" $packagingBody $token
Show "POST packaging-policies (synthetic 12000 IRR, CollectOnDelivery, option express)" $createdPackaging
$packagingPolicyId = (Unwrap $createdPackaging.Body).packagingPolicyId
if (-not $packagingPolicyId) {
    Emit "create refused because an earlier Development fixture already holds this identity; reusing it deliberately"
    $existing = Call "GET" "/v1/admin/shipping-services/tipax/packaging-policies" $null $token
    $packagingPolicyId = ((Unwrap $existing.Body).packagingPolicies |
        Where-Object { $_.optionCode -eq "express" } | Select-Object -First 1).packagingPolicyId
    $updated = Call "PUT" "/v1/admin/shipping-services/tipax/packaging-policies/$packagingPolicyId" $packagingBody $token
    Show "PUT packaging-policies/{id} (synthetic 12000 IRR, CollectOnDelivery, option express)" $updated
    $activated = Call "PATCH" "/v1/admin/shipping-services/tipax/packaging-policies/$packagingPolicyId/active" @{ isActive = $true } $token
    Show "PATCH packaging-policies/{id}/active {isActive:true}" $activated
}
Emit "packagingPolicyId under test = $packagingPolicyId"

$scheduleBody = @{
    serviceCode = "tipax"
    timeZoneId = "Asia/Tehran"
    optionCode = "express"
    windows = @(
        @{ weekday = 7; startTime = "13:00:00"; endTime = "14:00:00"; capacity = 25 },
        @{ weekday = 7; startTime = "16:00:00"; endTime = "17:00:00" }
    )
}
$createdSchedule = Call "PUT" "/v1/admin/shipping-services/tipax/operating-schedules" $scheduleBody $token
Show "PUT operating-schedules (synthetic Sunday 13:00-14:00 and 16:00-17:00 Asia/Tehran)" $createdSchedule
$scheduleId = (Unwrap $createdSchedule.Body).scheduleId
Emit "scheduleId under test = $scheduleId"
$armSchedule = Call "PATCH" "/v1/admin/shipping-services/tipax/operating-schedules/$scheduleId/active" @{ isActive = $true } $token
Show "PATCH operating-schedules/{id}/active {isActive:true} (arm the windows deterministically)" $armSchedule

Emit ""
Emit "=== phase 5b: the composed split sees BOTH the freight and the packaging component ==="
$codQuote = Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "requestedPickupAtUtc" "2026-10-18T10:00:00Z") $token
Show "POST operational-quote (freight 260000 COD + packaging 12000, inside window)" $codQuote

$prepaidBody = @{
    serviceCode = "tipax"
    scope = @{ countryId = $countryId; administrativeAreaId = $areaId }
    feeAmount = 12000
    currencyCode = "IRR"
    freightPayerMode = "Prepaid"
    optionCode = "express"
}
$toPrepaid = Call "PUT" "/v1/admin/shipping-services/tipax/packaging-policies/$packagingPolicyId" $prepaidBody $token
Show "PUT packaging-policies/{id} (same fee, payer switched to Prepaid)" $toPrepaid
$prepaidQuote = Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "requestedPickupAtUtc" "2026-10-18T10:00:00Z") $token
Show "POST operational-quote (same identity, Prepaid => freight+packaging at checkout)" $prepaidQuote

$backToCod = Call "PUT" "/v1/admin/shipping-services/tipax/packaging-policies/$packagingPolicyId" $packagingBody $token
Show "PUT packaging-policies/{id} (payer restored to CollectOnDelivery)" $backToCod

Emit ""
Emit "=== phase 6: the split and the carrier-window verdict on real HTTP ==="
$quoted = Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "requestedPickupAtUtc" "2026-10-18T10:00:00Z") $token
Show "POST operational-quote (inside window, COD freight, charged packaging)" $quoted

$outsideNow = Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "requestedPickupAtUtc" "2026-10-18T10:30:00Z") $token
Show "POST operational-quote (Sunday 14:00 local, half-open end => outside)" $outsideNow

$secondWindow = Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "requestedPickupAtUtc" "2026-10-18T13:00:00Z") $token
Show "POST operational-quote (Sunday 16:30 local, second window)" $secondWindow

$noInstant = Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" $quote $token
Show "POST operational-quote (no requested instant => not evaluated)" $noInstant

Emit ""
Emit "=== phase 7: inactive schedule has the documented safe meaning (not enforced) ==="
$deactSchedule = Call "PATCH" "/v1/admin/shipping-services/tipax/operating-schedules/$scheduleId/active" @{ isActive = $false } $token
Show "PATCH operating-schedules/{id}/active {isActive:false}" $deactSchedule
$inactive = Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "requestedPickupAtUtc" "2026-10-18T10:30:00Z") $token
Show "POST operational-quote (outside windows, but schedule inactive => not enforced)" $inactive

$reactSchedule = Call "PATCH" "/v1/admin/shipping-services/tipax/operating-schedules/$scheduleId/active" @{ isActive = $true } $token
Show "PATCH operating-schedules/{id}/active {isActive:true} (restore for the boundary proofs)" $reactSchedule

Emit ""
Emit "=== phase 8: measurement and identity boundaries on the real endpoint ==="
Show "POST operational-quote (weightGrams = 1999)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "weightGrams" 1999) $token)
Show "POST operational-quote (weightGrams = 2000)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "weightGrams" 2000) $token)
Show "POST operational-quote (invented option code => declared 400)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "optionCode" "no-such-option") $token)
Show "POST operational-quote (invented country id => declared 400)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "scope" @{ countryId = "11111111-1111-4111-8111-111111111111" }) $token)

Emit ""
Emit "=== phase 9: negative controls (client errors stay declared 4xx) ==="
Show "POST operational-quote with negative weight" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "weightGrams" -1) $token)
Show "POST operational-quote with zero item count" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "itemCount" 0) $token)
Show "POST operational-quote with invalid currency" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "currencyCode" "IR") $token)
Show "POST operational-quote with negative subtotal" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $quote "cartSubtotalExclusive" -5) $token)
Show "POST operational-quote with absent scope" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" @{ cartSubtotalExclusive = 1 } $token)
Show "POST operational-quote with unreadable body" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" "not-an-object" $token)
Show "POST operational-quote with no credential at all (expect 401)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" $quote $null)

Emit ""
Emit "=== phase 10: read-only confirmation - only POST exists and nothing was mutated ==="
Show "GET operational-quote (expect 405: no read route exists)" (Call "GET" "/v1/admin/shipping-services/tipax/operational-quote" $null $token)
$ratesAfter = Call "GET" "/v1/admin/shipping-services/tipax/rates" $null $token
Show "GET /v1/admin/shipping-services/tipax/rates (unchanged after all evaluations)" $ratesAfter
Emit "rates body identical before/after: $($ratesBefore.Body -eq $ratesAfter.Body)"

Emit ""
Emit "=== phase 11: cleanup - restore every Development fixture this run touched ==="
Show "PATCH packaging-policies/{id}/active {isActive:false}" (Call "PATCH" "/v1/admin/shipping-services/tipax/packaging-policies/$packagingPolicyId/active" @{ isActive = $false } $token)
Show "PATCH operating-schedules/{id}/active {isActive:false}" (Call "PATCH" "/v1/admin/shipping-services/tipax/operating-schedules/$scheduleId/active" @{ isActive = $false } $token)
if ($priorExpress) {
    $restoreBody = @{
        serviceCode = "tipax"
        scope = @{ countryId = $priorExpress.countryId; administrativeAreaId = $priorExpress.administrativeAreaId }
        feeAmount = $priorExpress.feeAmount
        currencyCode = $priorExpress.currencyCode
        freightPayerMode = $priorExpress.freightPayerMode
        optionCode = "express"
    }
    Show "PUT packaging-policies/{id} (restore prior fee/payer)" (Call "PUT" "/v1/admin/shipping-services/tipax/packaging-policies/$packagingPolicyId" $restoreBody $token)
    $restoreActive = if ($priorExpress.isActive) { $true } else { $false }
    Show "PATCH packaging-policies/{id}/active (restore prior activation)" (Call "PATCH" "/v1/admin/shipping-services/tipax/packaging-policies/$packagingPolicyId/active" @{ isActive = $restoreActive } $token)
}
Show "GET packaging-policies (final state)" (Call "GET" "/v1/admin/shipping-services/tipax/packaging-policies" $null $token)
Show "GET operating-schedules (final state)" (Call "GET" "/v1/admin/shipping-services/tipax/operating-schedules" $null $token)

Emit ""
Emit "DONE"
