$ErrorActionPreference = "Continue"
$base = "http://127.0.0.1:5099"
$logDir = Split-Path -Parent $PSCommandPath
$log = Join-Path $logDir "probe-phase4-crossservice.txt"
Set-Content -Path $log -Value "TB-SHIPPING-MVP-AUTHORIZED-ADMIN-059 cross-service mutation guard probe on a fresh area-scoped row"

function Emit([string]$text) { $text | Tee-Object -Append -FilePath $log | Out-Null; Write-Output $text }

function Call([string]$method, [string]$path, $body, [string]$token) {
    $headers = @{}
    if ($token) { $headers["Authorization"] = "Bearer $token" }
    $params = @{ Uri = "$base$path"; Method = $method; UseBasicParsing = $true; TimeoutSec = 25; Headers = $headers }
    if ($null -ne $body) { $params["Body"] = ($body | ConvertTo-Json -Depth 8 -Compress); $params["ContentType"] = "application/json" }
    try { $r = Invoke-WebRequest @params; return [pscustomobject]@{ Status = $r.StatusCode; Body = $r.Content } }
    catch {
        $resp = $_.Exception.Response
        if ($resp) { $sr = New-Object System.IO.StreamReader($resp.GetResponseStream()); return [pscustomobject]@{ Status = $resp.StatusCode.value__; Body = $sr.ReadToEnd() } }
        return [pscustomobject]@{ Status = -1; Body = $_.Exception.Message }
    }
}
function Show($label, $result) {
    $b = $result.Body; if ($null -eq $b) { $b = "" }
    if ($b.Length -gt 500) { $b = $b.Substring(0, 500) + "...(truncated)" }
    Emit "$label -> $($result.Status) $b"
}
function Unwrap($body) {
    if ([string]::IsNullOrWhiteSpace($body)) { return $null }
    $o = $body | ConvertFrom-Json
    if ($o -is [System.Array]) { return $o }
    if ($null -ne $o.PSObject.Properties["data"]) { return $o.data }
    return $o
}

$login = Call "POST" "/v1/auth/login" @{ identifierKind = "Email"; identifier = "admin-actor@tooba.local"; password = "admin-dev-horse-1" } $null
$token = ($login.Body | ConvertFrom-Json).accessToken
Emit "login -> $($login.Status); token acquired: $([bool]$token)"

$countries = Call "GET" "/v1/admin/geography/countries" $null $token
$countryId = (Unwrap $countries.Body)[0].id
$areas = Call "GET" "/v1/admin/geography/countries/$countryId/administrative-areas" $null $token
$areaId = (Unwrap $areas.Body)[0].id
Emit "resolved countryId=$countryId areaId=$areaId"

# Fresh scope (area level) for the post service so the cross-service guard can be exercised on a
# row that genuinely exists and is owned by a different route code.
$fresh = @{ serviceCode = "post"; scope = @{ countryId = $countryId; administrativeAreaId = $areaId }; chargeAmount = 0; currencyCode = "IRR"; preparationDays = 1; leadDays = 3 }
$created = Call "POST" "/v1/admin/shipping-services/post/rates" $fresh $token
Show "setup POST area-scoped post rate" $created
$freshId = (Unwrap $created.Body).ratePolicyId

$freshGrant = Call "POST" "/v1/admin/shipping-services/post/coverage-grants" @{ serviceCode = "post"; scope = @{ countryId = $countryId; administrativeAreaId = $areaId } } $token
Show "setup POST area-scoped post coverage grant" $freshGrant
$freshGrantId = (Unwrap $freshGrant.Body).coverageGrantId

Emit ""
Emit "=== cross-service mutation guard (post-owned row through the tipax route) ==="
$crossPut = Call "PUT" "/v1/admin/shipping-services/tipax/rates/$freshId" @{ serviceCode = "tipax"; scope = @{ countryId = $countryId; administrativeAreaId = $areaId }; chargeAmount = 0; currencyCode = "IRR"; preparationDays = 1; leadDays = 9 } $token
Show "PUT tipax route on a post-owned rate policy" $crossPut
$crossPatch = Call "PATCH" "/v1/admin/shipping-services/tipax/rates/$freshId/active" @{ isActive = $false } $token
Show "PATCH tipax route on a post-owned rate policy" $crossPatch
$crossGrant = Call "PATCH" "/v1/admin/shipping-services/tipax/coverage-grants/$freshGrantId/active" @{ isActive = $false } $token
Show "PATCH tipax route on a post-owned coverage grant" $crossGrant

Emit ""
Emit "=== scope immutability through the write seam ==="
$scopeChange = Call "PUT" "/v1/admin/shipping-services/post/rates/$freshId" @{ serviceCode = "post"; scope = @{ countryId = $countryId }; chargeAmount = 0; currencyCode = "IRR"; preparationDays = 1; leadDays = 3 } $token
Show "PUT with a different (wider) scope on an existing policy" $scopeChange

Emit ""
Emit "=== affected row state after guard attempts ==="
$post = Call "GET" "/v1/admin/shipping-services/post/rates" $null $token
Show "GET post view" $post
$tipax = Call "GET" "/v1/admin/shipping-services/tipax/rates" $null $token
Show "GET tipax view (must stay empty)" $tipax
Emit "DONE"
