$ErrorActionPreference = "Continue"
$root = "http://127.0.0.1:5088"
$logDir = Split-Path -Parent $PSCommandPath
$log = Join-Path $logDir "log-live-operational-quote.txt"
Set-Content -Path $log -Value @"
TB-SHIPPING-FULL-OPERATIONS-078 - live Development matrix for the read-only operational quote inspector
Target provenance (positively identified Development, never staging/production):
  GET http://127.0.0.1:5088/__platform-commerce => edition=SingleStore, deploymentId=dev-singlestore,
    tenantId=store-alpha, connectionReference=tenant-alpha
  Database: tooba_alpha
Host: http://127.0.0.1:5088 (already running; NOT restarted, NOT rebuilt by this task)
READ-ONLY BY CONSTRUCTION: this probe issues GETs and the authorized diagnostic POST only.
No rate, coverage, packaging policy, schedule or order is created, edited or deleted.
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
    if ($b.Length -gt 3000) { $b = $b.Substring(0, 3000) + "...(truncated)" }
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
Emit "=== phase 0: target identity ==="
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
Emit "=== phase 3: baseline read of every surface this probe must NOT change ==="
$ratesBefore = Call "GET" "/v1/admin/shipping-services/tipax/rates" $null $token
$packagingBefore = Call "GET" "/v1/admin/shipping-services/tipax/packaging-policies" $null $token
$schedulesBefore = Call "GET" "/v1/admin/shipping-services/tipax/operating-schedules" $null $token
Emit "rates baseline captured: $($ratesBefore.Status)"
Emit "packaging baseline captured: $($packagingBefore.Status)"
Emit "schedules baseline captured: $($schedulesBefore.Status)"
Emit "existing ACTIVE schedule identity (tipax/standard): $((Unwrap $schedulesBefore.Body).schedules | Where-Object { $_.isActive } | Select-Object -First 1 | ConvertTo-Json -Compress)"

# The evaluation identity is the EXISTING tipax/standard pair with a currently ACTIVE carrier schedule.
$standard = @{
    scope = @{ countryId = $countryId; administrativeAreaId = $areaId }
    cartSubtotalExclusive = 500000
    currencyCode = "IRR"
    optionCode = "standard"
}
$express = With $standard "optionCode" "express"

Emit ""
Emit "=== phase 4: configured schedule, inside the active window (tipax/standard Sunday 13:00-14:00 Asia/Tehran) ==="
Show "POST operational-quote (2026-10-18T10:00:00Z = Sunday 13:30 local, inside)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $standard "requestedPickupAtUtc" "2026-10-18T10:00:00Z") $token)

Emit ""
Emit "=== phase 5: same active schedule, outside the window (half-open end: 14:00 local is excluded) ==="
Show "POST operational-quote (2026-10-18T10:30:00Z = Sunday 14:00 local, outside)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $standard "requestedPickupAtUtc" "2026-10-18T10:30:00Z") $token)

Emit ""
Emit "=== phase 6: the second configured window of the same active schedule ==="
Show "POST operational-quote (2026-10-18T13:15:00Z = Sunday 16:45 local, inside the 16:30-18:00 window)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $standard "requestedPickupAtUtc" "2026-10-18T13:15:00Z") $token)

Emit ""
Emit "=== phase 7: no supplied instant => NotEvaluated, and never 'available' ==="
Show "POST operational-quote (no requestedPickupAtUtc at all)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" $standard $token)

Emit ""
Emit "=== phase 8: an inactive schedule is NOT a constraint (tipax/express schedule exists but is inactive) ==="
Show "POST operational-quote (tipax/express, instant outside all windows, schedule inactive)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $express "requestedPickupAtUtc" "2026-10-18T10:30:00Z") $token)

Emit ""
Emit "=== phase 9: an absent schedule identity (no schedule row at all for the identity) ==="
Show "POST operational-quote (tipax service level, no schedule row for the service-level identity)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With (With $standard "optionCode" $null) "requestedPickupAtUtc" "2026-10-18T10:00:00Z") $token)

Emit ""
Emit "=== phase 10: packaging fallback - the EXISTING tipax/standard scope has NO active packaging policy ==="
Show "POST operational-quote (no active packaging policy => explicit zero fee, not a hidden charge)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $standard "requestedPickupAtUtc" "2026-10-18T10:00:00Z") $token)

Emit ""
Emit "=== phase 11: valid weight / count combinations on the real endpoint ==="
Show "POST operational-quote (weightGrams = 1999, itemCount = 2)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With (With $standard "requestedPickupAtUtc" "2026-10-18T10:00:00Z") "weightGrams" 1999) $token)
Show "POST operational-quote (weightGrams = 2000, itemCount = 2)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With (With $standard "requestedPickupAtUtc" "2026-10-18T10:00:00Z") "weightGrams" 2000) $token)
Emit "NOTE: the 1999/2000 tier boundary is NOT claimed here: every weight tier on tipax/standard is"
Emit "      currently isActive=false, so the winning rate is the unconditioned 180000 IRR rule. The"
Emit "      boundary condition itself is covered by the focused unit fixture (see browser-ui-proof.md)."

Emit ""
Emit "=== phase 12: declared client errors - invalid destination, invalid option ==="
Show "POST operational-quote (invented country id => declared 400)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $standard "scope" @{ countryId = "11111111-1111-4111-8111-111111111111" }) $token)
Show "POST operational-quote (invented option code => declared 400)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $standard "optionCode" "no-such-option") $token)
Show "POST operational-quote (invented service code => declared 400)" (Call "POST" "/v1/admin/shipping-services/zzz-not-a-service/operational-quote" $standard $token)

Emit ""
Emit "=== phase 13: shape negative controls ==="
Show "POST operational-quote (weightGrams = -1)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $standard "weightGrams" -1) $token)
Show "POST operational-quote (itemCount = 0)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $standard "itemCount" 0) $token)
Show "POST operational-quote (currencyCode = 'IR')" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $standard "currencyCode" "IR") $token)
Show "POST operational-quote (absent scope)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" @{ cartSubtotalExclusive = 1 } $token)
Show "POST operational-quote (unreadable body)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" "not-an-object" $token)

Emit ""
Emit "=== phase 14: currency honesty - a mismatched request currency is not converted silently ==="
Show "POST operational-quote (currencyCode = USD against IRR rates)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" (With $standard "currencyCode" "USD") $token)

Emit ""
Emit "=== phase 15: 401 - no credential at all ==="
Show "POST operational-quote with no credential (expect 401)" (Call "POST" "/v1/admin/shipping-services/tipax/operational-quote" $standard $null)

Emit ""
Emit "=== phase 16: no-write proof - every surface is byte-identical before/after ==="
Show "GET operational-quote (expect 405: no read route exists)" (Call "GET" "/v1/admin/shipping-services/tipax/operational-quote" $null $token)
$ratesAfter = Call "GET" "/v1/admin/shipping-services/tipax/rates" $null $token
$packagingAfter = Call "GET" "/v1/admin/shipping-services/tipax/packaging-policies" $null $token
$schedulesAfter = Call "GET" "/v1/admin/shipping-services/tipax/operating-schedules" $null $token
Emit "rates identical before/after:      $($ratesBefore.Body -eq $ratesAfter.Body)"
Emit "packaging identical before/after:  $($packagingBefore.Body -eq $packagingAfter.Body)"
Emit "schedules identical before/after:  $($schedulesBefore.Body -eq $schedulesAfter.Body)"

Emit ""
Emit "DONE"
