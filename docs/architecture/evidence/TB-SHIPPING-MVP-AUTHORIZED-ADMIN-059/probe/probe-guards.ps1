$ErrorActionPreference = "Continue"
$base = "http://127.0.0.1:5099"
$logDir = Split-Path -Parent $PSCommandPath
$log = Join-Path $logDir "probe-phase3-guards.txt"
Set-Content -Path $log -Value "TB-SHIPPING-MVP-AUTHORIZED-ADMIN-059 authorization + cross-service/scope guard probes (scratch instance :5099)"

function Emit([string]$text) { $text | Tee-Object -Append -FilePath $log | Out-Null; Write-Output $text }

function Call([string]$method, [string]$path, $body, [string]$token) {
    $headers = @{}
    if ($token) { $headers["Authorization"] = "Bearer $token" }
    $params = @{ Uri = "$base$path"; Method = $method; UseBasicParsing = $true; TimeoutSec = 25; Headers = $headers }
    if ($null -ne $body) { $params["Body"] = ($body | ConvertTo-Json -Depth 8 -Compress); $params["ContentType"] = "application/json" }
    try {
        $r = Invoke-WebRequest @params
        return [pscustomobject]@{ Status = $r.StatusCode; Body = $r.Content }
    } catch {
        $resp = $_.Exception.Response
        if ($resp) { $sr = New-Object System.IO.StreamReader($resp.GetResponseStream()); return [pscustomobject]@{ Status = $resp.StatusCode.value__; Body = $sr.ReadToEnd() } }
        return [pscustomobject]@{ Status = -1; Body = $_.Exception.Message }
    }
}
function Show($label, $result) {
    $b = $result.Body; if ($null -eq $b) { $b = "" }
    if ($b.Length -gt 600) { $b = $b.Substring(0, 600) + "...(truncated)" }
    Emit "$label -> $($result.Status) $b"
}
function Unwrap($body) {
    if ([string]::IsNullOrWhiteSpace($body)) { return $null }
    $o = $body | ConvertFrom-Json
    if ($o -is [System.Array]) { return $o }
    if ($null -ne $o.PSObject.Properties["data"]) { return $o.data }
    return $o
}

Emit "=== the two Development actor seams, compared (is the dev header authorization?) ==="
$devHeaderOnly = Invoke-WebRequest -Uri "$base/v1/admin/shipping-services/post/rates" -UseBasicParsing -TimeoutSec 20 -Headers @{ "X-Tooba-Dev-Actor-User-Id" = "01a036c2-970e-7000-8eb7-94bf5cc2d8db" } -ErrorAction SilentlyContinue
if ($devHeaderOnly) { Emit "GET rates with only the Development actor header -> $($devHeaderOnly.StatusCode)" }

$login = Call "POST" "/v1/auth/login" @{ identifierKind = "Email"; identifier = "admin-actor@tooba.local"; password = "admin-dev-horse-1" } $null
$token = ($login.Body | ConvertFrom-Json).accessToken
Emit "authenticated session token acquired: $([bool]$token)"

$countries = Call "GET" "/v1/admin/geography/countries" $null $token
$countryId = (Unwrap $countries.Body)[0].id
$areas = Call "GET" "/v1/admin/geography/countries/$countryId/administrative-areas" $null $token
$areaId = (Unwrap $areas.Body)[0].id

$rateBody = @{ serviceCode = "post"; scope = @{ countryId = $countryId }; chargeAmount = 0; currencyCode = "IRR"; preparationDays = 1; leadDays = 2 }
$created = Call "POST" "/v1/admin/shipping-services/post/rates" $rateBody $token
$ratePolicyId = (Unwrap $created.Body).ratePolicyId
Emit "setup: post rate policy created = $ratePolicyId ($($created.Status))"

$grant = Call "POST" "/v1/admin/shipping-services/post/coverage-grants" @{ serviceCode = "post"; scope = @{ countryId = $countryId } } $token
$coverageGrantId = (Unwrap $grant.Body).coverageGrantId
Emit "setup: post coverage grant created = $coverageGrantId ($($grant.Status))"

Emit ""
Emit "=== guard A: cross-service mutation must be denied (post-owned row through the tipax route) ==="
$crossPut = Call "PUT" "/v1/admin/shipping-services/tipax/rates/$ratePolicyId" @{ serviceCode = "tipax"; scope = @{ countryId = $countryId }; chargeAmount = 0; currencyCode = "IRR"; preparationDays = 1; leadDays = 9 } $token
Show "PUT tipax route on a post-owned rate policy" $crossPut
$crossPatch = Call "PATCH" "/v1/admin/shipping-services/tipax/rates/$ratePolicyId/active" @{ isActive = $false } $token
Show "PATCH tipax route on a post-owned rate policy" $crossPatch
$crossGrant = Call "PATCH" "/v1/admin/shipping-services/tipax/coverage-grants/$coverageGrantId/active" @{ isActive = $false } $token
Show "PATCH tipax route on a post-owned coverage grant" $crossGrant

Emit ""
Emit "=== guard B: scope of an existing policy is immutable through the write seam ==="
$scopeChange = Call "PUT" "/v1/admin/shipping-services/post/rates/$ratePolicyId" @{ serviceCode = "post"; scope = @{ countryId = $countryId; administrativeAreaId = $areaId }; chargeAmount = 0; currencyCode = "IRR"; preparationDays = 1; leadDays = 2 } $token
Show "PUT with a different (narrower) scope on an existing policy" $scopeChange

Emit ""
Emit "=== guard C: duplicate scope for the same service must be refused ==="
$dup = Call "POST" "/v1/admin/shipping-services/post/rates" $rateBody $token
Show "POST duplicate (service, scope) rate policy" $dup
$dupGrant = Call "POST" "/v1/admin/shipping-services/post/coverage-grants" @{ serviceCode = "post"; scope = @{ countryId = $countryId } } $token
Show "POST duplicate (service, scope) coverage grant" $dupGrant

Emit ""
Emit "=== guard D: unknown ids must be refused, and empty coverage must never read as unrestricted ==="
$unknownRate = Call "PUT" "/v1/admin/shipping-services/post/rates/11111111-1111-4111-8111-111111111111" $rateBody $token
Show "PUT unknown ratePolicyId" $unknownRate
$unknownGrant = Call "PATCH" "/v1/admin/shipping-services/post/coverage-grants/11111111-1111-4111-8111-111111111111/active" @{ isActive = $false } $token
Show "PATCH unknown coverageGrantId" $unknownGrant
$tipax = Call "GET" "/v1/admin/shipping-services/tipax/rates" $null $token
Show "GET tipax (must stay empty; no cross-service leakage)" $tipax

Emit ""
Emit "=== guard E: malformed / missing scope must be refused ==="
$missingScope = Call "POST" "/v1/admin/shipping-services/post/rates" @{ serviceCode = "post"; chargeAmount = 0; currencyCode = "IRR"; preparationDays = 1; leadDays = 1 } $token
Show "POST rate without scope" $missingScope

Emit ""
Emit "DONE"
