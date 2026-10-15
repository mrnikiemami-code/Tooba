$ErrorActionPreference = "Continue"
$base = "http://127.0.0.1:5088"
$logDir = Split-Path -Parent $PSCommandPath
$log = Join-Path $logDir "log-live-packaging-proof.txt"
Set-Content -Path $log -Value @"
TB-SHIPPING-FULL-OPERATIONS-075 - live Development proof for Shipping-owned packaging fee policy
Target provenance (positively identified Development, never staging/production):
  GET http://127.0.0.1:5088/__platform-commerce => edition=SingleStore, deploymentId=dev-singlestore,
    tenantId=store-alpha, connectionReference=tenant-alpha
  Database: tooba_alpha on 127.0.0.1:5432 (docker container postgres-db)
  Host: http://127.0.0.1:5088
"@

function Emit([string]$text) {
    Add-Content -Path $log -Value $text -Encoding UTF8
    Write-Output $text
}

function Call([string]$method, [string]$path, $body, [string]$token) {
    $headers = @{ "Accept-Language" = "fa" }
    if ($token) { $headers["Authorization"] = "Bearer $token" }
    $params = @{ Uri = "$base$path"; Method = $method; UseBasicParsing = $true; TimeoutSec = 25; Headers = $headers }
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
    if ($b.Length -gt 700) { $b = $b.Substring(0, 700) + "...(truncated)" }
    Emit "$label -> $($result.Status) $b"
}

function Unwrap($body) {
    if ([string]::IsNullOrWhiteSpace($body)) { return $null }
    $o = $body | ConvertFrom-Json
    if ($o -is [System.Array]) { return $o }
    if ($null -ne $o.PSObject.Properties["data"]) { return $o.data }
    return $o
}

Emit ""
Emit "=== target identity ==="
$commerce = Call "GET" "/__platform-commerce" $null $null
Show "GET /__platform-commerce" $commerce

Emit ""
Emit "=== phase 1: real authentication through the existing platform auth system ==="
$login = Call "POST" "/v1/auth/login" @{ identifierKind = "Email"; identifier = "admin-actor@tooba.local"; password = "admin-dev-horse-1" } $null
Show "POST /v1/auth/login (Development bootstrap admin actor)" $login
$token = ($login.Body | ConvertFrom-Json).accessToken
Emit "accessToken acquired: $([bool]$token) (opaque session token issued by /v1/auth/login)"

Emit ""
Emit "=== phase 2: real Geography identity for the packaging scope (read-only) ==="
$countries = Call "GET" "/v1/admin/geography/countries" $null $token
$countryId = (Unwrap $countries.Body)[0].id
Emit "resolved REAL countryId = $countryId"
$areas = Call "GET" "/v1/admin/geography/countries/$countryId/administrative-areas" $null $token
$areaId = (Unwrap $areas.Body)[0].id
Emit "resolved REAL administrativeAreaId = $areaId"

Emit ""
Emit "=== phase 3: baseline read of the packaging surface for a real service ==="
$before = Call "GET" "/v1/admin/shipping-services/tipax/packaging-policies" $null $token
Show "GET /v1/admin/shipping-services/tipax/packaging-policies (before)" $before

Emit ""
Emit "=== phase 4: authorized admin create of a clearly marked synthetic packaging policy (12,000 IRR) ==="
$createBody = @{
    serviceCode = "tipax"
    scope = @{ countryId = $countryId; administrativeAreaId = $areaId }
    feeAmount = 12000
    currencyCode = "IRR"
    freightPayerMode = "CollectOnDelivery"
    optionCode = "express"
}
$created = Call "POST" "/v1/admin/shipping-services/tipax/packaging-policies" $createBody $token
Show "POST /v1/admin/shipping-services/tipax/packaging-policies" $created
$packagingPolicyId = (Unwrap $created.Body).packagingPolicyId
Emit "created packagingPolicyId = $packagingPolicyId"

Emit ""
Emit "=== phase 5: read-after-write ==="
$afterCreate = Call "GET" "/v1/admin/shipping-services/tipax/packaging-policies" $null $token
Show "GET packaging-policies (after create)" $afterCreate

Emit ""
Emit "=== phase 6: PUT update (fee + payer mode) then PATCH activation toggle ==="
$updateBody = @{
    serviceCode = "tipax"
    scope = @{ countryId = $countryId; administrativeAreaId = $areaId }
    feeAmount = 15000
    currencyCode = "IRR"
    freightPayerMode = "Prepaid"
    optionCode = "express"
}
$updated = Call "PUT" "/v1/admin/shipping-services/tipax/packaging-policies/$packagingPolicyId" $updateBody $token
Show "PUT /v1/admin/shipping-services/tipax/packaging-policies/{id}" $updated

$deact = Call "PATCH" "/v1/admin/shipping-services/tipax/packaging-policies/$packagingPolicyId/active" @{ isActive = $false } $token
Show "PATCH .../packaging-policies/{id}/active {isActive:false}" $deact

$afterDeact = Call "GET" "/v1/admin/shipping-services/tipax/packaging-policies" $null $token
Show "GET packaging-policies (after deactivate)" $afterDeact

$react = Call "PATCH" "/v1/admin/shipping-services/tipax/packaging-policies/$packagingPolicyId/active" @{ isActive = $true } $token
Show "PATCH .../packaging-policies/{id}/active {isActive:true}" $react

Emit ""
Emit "=== phase 7: validation / negative controls ==="
$negative = Call "POST" "/v1/admin/shipping-services/tipax/packaging-policies" @{ serviceCode = "tipax"; scope = @{ countryId = $countryId; administrativeAreaId = $areaId }; feeAmount = -1; currencyCode = "IRR" } $token
Show "POST packaging policy with negative fee" $negative

$badCurrency = Call "POST" "/v1/admin/shipping-services/tipax/packaging-policies" @{ serviceCode = "tipax"; scope = @{ countryId = $countryId; administrativeAreaId = $areaId }; feeAmount = 1000; currencyCode = "IR" } $token
Show "POST packaging policy with invalid currency" $badCurrency

$badMode = Call "POST" "/v1/admin/shipping-services/tipax/packaging-policies" @{ serviceCode = "tipax"; scope = @{ countryId = $countryId; administrativeAreaId = $areaId }; feeAmount = 1000; currencyCode = "IRR"; freightPayerMode = 42 } $token
Show "POST packaging policy with out-of-contract payer mode" $badMode

$unknownService = Call "POST" "/v1/admin/shipping-services/zzz-not-a-service/packaging-policies" @{ serviceCode = "zzz-not-a-service"; scope = @{ countryId = $countryId }; feeAmount = 1000; currencyCode = "IRR" } $token
Show "POST packaging policy for unknown service code" $unknownService

$unknownOption = Call "POST" "/v1/admin/shipping-services/tipax/packaging-policies" @{ serviceCode = "tipax"; scope = @{ countryId = $countryId; administrativeAreaId = $areaId }; feeAmount = 1000; currencyCode = "IRR"; optionCode = "no-such-option" } $token
Show "POST packaging policy for unknown option code" $unknownOption

$inventedScope = Call "POST" "/v1/admin/shipping-services/tipax/packaging-policies" @{ serviceCode = "tipax"; scope = @{ countryId = "11111111-1111-4111-8111-111111111111" }; feeAmount = 1000; currencyCode = "IRR" } $token
Show "POST packaging policy for invented country id" $inventedScope

$duplicate = Call "POST" "/v1/admin/shipping-services/tipax/packaging-policies" $createBody $token
Show "POST packaging policy duplicating the same service/option/scope" $duplicate

$noToken = Call "GET" "/v1/admin/shipping-services/tipax/packaging-policies" $null $null
Show "GET packaging-policies with no credential at all (expect 401)" $noToken

Emit ""
Emit "=== phase 8: zero/default behaviour on a non-configured identity ==="
$notConfigured = Call "GET" "/v1/admin/shipping-services/post/packaging-policies" $null $token
Show "GET packaging-policies for a service with no packaging policy (post)" $notConfigured

Emit ""
Emit "=== phase 9: cleanup - deactivate the task-owned synthetic fixture ==="
$cleanup = Call "PATCH" "/v1/admin/shipping-services/tipax/packaging-policies/$packagingPolicyId/active" @{ isActive = $false } $token
Show "PATCH .../packaging-policies/{id}/active {isActive:false} (cleanup)" $cleanup

$final = Call "GET" "/v1/admin/shipping-services/tipax/packaging-policies" $null $token
Show "GET packaging-policies (final state)" $final

Emit ""
Emit "DONE"

