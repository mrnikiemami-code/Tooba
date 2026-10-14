$ErrorActionPreference = "Continue"
$base = "http://127.0.0.1:5099"
$logDir = Split-Path -Parent $PSCommandPath
$log = Join-Path $logDir "probe-phase2-roundtrip.txt"
Set-Content -Path $log -Value "TB-SHIPPING-MVP-AUTHORIZED-ADMIN-059 authorized admin real-HTTP roundtrip (isolated scratch instance 127.0.0.1:5099, scratch DB tooba_059_scratch)"

function Emit([string]$text) {
    $text | Tee-Object -Append -FilePath $log | Out-Null
    Write-Output $text
}

function Call([string]$method, [string]$path, $body, [string]$token) {
    $headers = @{}
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
    if ($b.Length -gt 900) { $b = $b.Substring(0, 900) + "...(truncated)" }
    Emit "$label -> $($result.Status) $b"
}

# API envelopes observed on this build: Geo reads return a bare JSON array, Shipping view a bare object.
function Unwrap($body) {
    if ([string]::IsNullOrWhiteSpace($body)) { return $null }
    $o = $body | ConvertFrom-Json
    if ($o -is [System.Array]) { return $o }
    if ($null -ne $o.PSObject.Properties["data"]) { return $o.data }
    return $o
}

Emit ""
Emit "=== phase 1: real authentication through the existing platform auth system ==="
$login = Call "POST" "/v1/auth/login" @{ identifierKind = "Email"; identifier = "admin-actor@tooba.local"; password = "admin-dev-horse-1" } $null
Show "POST /v1/auth/login (Development bootstrap admin actor)" $login
$token = ($login.Body | ConvertFrom-Json).accessToken
Emit "accessToken acquired: $([bool]$token) (opaque session token issued by /v1/auth/login)"

Emit ""
Emit "=== phase 2: authorized admin reads with the authenticated session ==="
$countries = Call "GET" "/v1/admin/geography/countries" $null $token
Show "GET /v1/admin/geography/countries" $countries
$countryId = (Unwrap $countries.Body)[0].id
Emit "resolved REAL countryId = $countryId"

$areas = Call "GET" "/v1/admin/geography/countries/$countryId/administrative-areas" $null $token
Show "GET /v1/admin/geography/countries/{countryId}/administrative-areas" $areas
$areaId = (Unwrap $areas.Body)[0].id
Emit "resolved REAL administrativeAreaId = $areaId"

$cities = Call "GET" "/v1/admin/geography/countries/$countryId/cities" $null $token
Show "GET /v1/admin/geography/countries/{countryId}/cities" $cities
$cityId = (Unwrap $cities.Body)[0].id
Emit "resolved REAL cityId = $cityId"

$svcAreas = Call "GET" "/v1/admin/geography/cities/$cityId/service-areas" $null $token
Show "GET /v1/admin/geography/cities/{cityId}/service-areas" $svcAreas

$before = Call "GET" "/v1/admin/shipping-services/post/rates" $null $token
Show "GET /v1/admin/shipping-services/post/rates (state before writes)" $before

Emit ""
Emit "=== phase 3: authorized admin writes into scratch only (TEST-ONLY zero charge) ==="
$rateBody = @{ serviceCode = "post"; scope = @{ countryId = $countryId }; chargeAmount = 0; currencyCode = "IRR"; preparationDays = 1; leadDays = 2 }
$created = Call "POST" "/v1/admin/shipping-services/post/rates" $rateBody $token
Show "POST /v1/admin/shipping-services/post/rates" $created
$ratePolicyId = (Unwrap $created.Body).ratePolicyId
Emit "created ratePolicyId = $ratePolicyId"

$grantBody = @{ serviceCode = "post"; scope = @{ countryId = $countryId } }
$grant = Call "POST" "/v1/admin/shipping-services/post/coverage-grants" $grantBody $token
Show "POST /v1/admin/shipping-services/post/coverage-grants" $grant
$coverageGrantId = (Unwrap $grant.Body).coverageGrantId
Emit "created coverageGrantId = $coverageGrantId"

Emit ""
Emit "=== phase 4: read-after-write ==="
$afterCreate = Call "GET" "/v1/admin/shipping-services/post/rates" $null $token
Show "GET /v1/admin/shipping-services/post/rates (after create)" $afterCreate

Emit ""
Emit "=== phase 5: PUT update then PATCH activation ==="
$updateBody = @{ serviceCode = "post"; scope = @{ countryId = $countryId }; chargeAmount = 0; currencyCode = "IRR"; preparationDays = 1; leadDays = 5 }
$updated = Call "PUT" "/v1/admin/shipping-services/post/rates/$ratePolicyId" $updateBody $token
Show "PUT /v1/admin/shipping-services/post/rates/{ratePolicyId}" $updated

$afterUpdate = Call "GET" "/v1/admin/shipping-services/post/rates" $null $token
Show "GET rates after PUT (expect leadDays changed 2->5)" $afterUpdate

$deact = Call "PATCH" "/v1/admin/shipping-services/post/rates/$ratePolicyId/active" @{ isActive = $false } $token
Show "PATCH /v1/admin/shipping-services/post/rates/{ratePolicyId}/active {isActive:false}" $deact

$grantDeact = Call "PATCH" "/v1/admin/shipping-services/post/coverage-grants/$coverageGrantId/active" @{ isActive = $false } $token
Show "PATCH /v1/admin/shipping-services/post/coverage-grants/{coverageGrantId}/active {isActive:false}" $grantDeact

$afterMutations = Call "GET" "/v1/admin/shipping-services/post/rates" $null $token
Show "GET rates after PUT+PATCH (expect isActive false on both)" $afterMutations

Emit ""
Emit "=== phase 6: negative controls / fail-closed checks ==="
$badService = Call "POST" "/v1/admin/shipping-services/zzz-not-a-service/rates" @{ serviceCode = "zzz-not-a-service"; scope = @{ countryId = $countryId }; chargeAmount = 0; currencyCode = "IRR"; preparationDays = 1; leadDays = 1 } $token
Show "POST rate for unknown service code" $badService

$invented = Call "POST" "/v1/admin/shipping-services/post/coverage-grants" @{ serviceCode = "post"; scope = @{ countryId = "11111111-1111-4111-8111-111111111111" } } $token
Show "POST coverage grant for invented country id" $invented

$noToken = Call "GET" "/v1/admin/shipping-services/post/rates" $null $null
Show "GET rates with no credential at all" $noToken

$crossService = Call "GET" "/v1/admin/shipping-services/tipax/rates" $null $token
Show "GET rates for a different real service code (isolation)" $crossService

Emit ""
Emit "=== phase 7: checkout/storefront authority must stay legacy (no cutover) ==="
$provinces = Call "GET" "/v1/storefront/geography/provinces" $null $null
$provincesShort = $provinces.Body
if ($provincesShort -and $provincesShort.Length -gt 300) { $provincesShort = $provincesShort.Substring(0, 300) + "...(truncated)" }
Emit "GET /v1/storefront/geography/provinces -> $($provinces.Status) $provincesShort"
$checkout = Call "GET" "/v1/storefront/cart/00000000-0000-0000-0000-000000000000/payment-draft" $null $null
Show "GET /v1/storefront/cart/{unknown}/payment-draft (legacy Order path, unreachable id)" $checkout

Emit ""
Emit "DONE"
