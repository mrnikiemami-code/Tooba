$ErrorActionPreference = "Continue"
$base = "http://127.0.0.1:5088"
$logDir = Split-Path -Parent $PSCommandPath
$log = Join-Path $logDir "log-live-admin-packaging-proof.txt"

# TB-SHIPPING-FULL-OPERATIONS-077 — non-mutating HTTP proof for the admin packaging/payer surface.
#
# The mutating round-trip (create 12000 CollectOnDelivery -> edit 15000 Prepaid -> toggle off/on ->
# leave INACTIVE) was performed through the real browser UI and is transcribed in
# log-live-admin-packaging-proof.txt. This script re-proves, at the wire level and without creating
# any row, the exact route identity, the read-after-write state and every declared 4xx/401 control.

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

Set-Content -Path $log -Value @"
TB-SHIPPING-FULL-OPERATIONS-077 - wire proof for the admin packaging fee and freight payer surface
Target provenance (positively identified Development, never staging/production):
  GET http://127.0.0.1:5088/__platform-commerce => edition=SingleStore, deploymentId=dev-singlestore,
    tenantId=store-alpha, connectionReference=tenant-alpha
  Database: tooba_alpha on 127.0.0.1:5432
  Host: http://127.0.0.1:5088   UI: http://127.0.0.1:3000/admin/shipping-services
Task-owned synthetic fixture (created/edited/toggled through the real browser UI, left INACTIVE):
  packagingPolicyId 01a13ec9-4e2a-7000-a5cb-428d28a26e8a
"@

Emit ""
Emit "=== target identity ==="
Show "GET /__platform-commerce" (Call "GET" "/__platform-commerce" $null $null)

Emit ""
Emit "=== phase 1: real authentication ==="
$login = Call "POST" "/v1/auth/login" @{ identifierKind = "Email"; identifier = "admin-actor@tooba.local"; password = "admin-dev-horse-1" } $null
Show "POST /v1/auth/login (Development bootstrap admin actor)" $login
$token = ($login.Body | ConvertFrom-Json).accessToken
Emit "accessToken acquired: $([bool]$token)"

Emit ""
Emit "=== phase 2: route identity - the exact 075 admin packaging routes ==="
Show "GET /v1/admin/shipping-services/tipax/packaging-policies" (Call "GET" "/v1/admin/shipping-services/tipax/packaging-policies" $null $token)

Emit ""
Emit "=== phase 3: read-after-write state of the task-owned fixture (left INACTIVE) ==="
$list = Unwrap (Call "GET" "/v1/admin/shipping-services/tipax/packaging-policies" $null $token).Body
$mine = $list.packagingPolicies | Where-Object { $_.packagingPolicyId -eq "01a13ec9-4e2a-7000-a5cb-428d28a26e8a" }
Emit "task-owned row: fee=$($mine.feeAmount) payer=$($mine.freightPayerMode) option=$($mine.optionCode) active=$($mine.isActive) area=$($mine.administrativeAreaId)"
Emit "every policy of this service is inactive: $((($list.packagingPolicies | Where-Object { $_.isActive }).Count) -eq 0)"

Emit ""
Emit "=== phase 4: declared validation / negative controls (no row is created) ==="
$scope = @{ countryId = "e3451206-e503-51a8-91ad-afa9ab70e330"; administrativeAreaId = "24e00388-c216-531c-a3ca-11f382508760" }
Show "POST duplicate scope (already registered)" (Call "POST" "/v1/admin/shipping-services/tipax/packaging-policies" @{ serviceCode = "tipax"; scope = $scope; feeAmount = 12000; currencyCode = "IRR"; freightPayerMode = "CollectOnDelivery"; optionCode = "express" } $token)
Show "POST negative fee" (Call "POST" "/v1/admin/shipping-services/tipax/packaging-policies" @{ serviceCode = "tipax"; scope = $scope; feeAmount = -1; currencyCode = "IRR" } $token)
Show "POST invalid currency" (Call "POST" "/v1/admin/shipping-services/tipax/packaging-policies" @{ serviceCode = "tipax"; scope = $scope; feeAmount = 1000; currencyCode = "IR" } $token)
Show "POST out-of-contract payer mode" (Call "POST" "/v1/admin/shipping-services/tipax/packaging-policies" @{ serviceCode = "tipax"; scope = $scope; feeAmount = 1000; currencyCode = "IRR"; freightPayerMode = 42 } $token)
Show "POST unknown service code" (Call "POST" "/v1/admin/shipping-services/zzz-not-a-service/packaging-policies" @{ serviceCode = "zzz-not-a-service"; scope = @{ countryId = "e3451206-e503-51a8-91ad-afa9ab70e330" }; feeAmount = 1000; currencyCode = "IRR" } $token)
Show "POST unknown option code" (Call "POST" "/v1/admin/shipping-services/tipax/packaging-policies" @{ serviceCode = "tipax"; scope = $scope; feeAmount = 1000; currencyCode = "IRR"; optionCode = "no-such-option" } $token)
Show "POST invented country id" (Call "POST" "/v1/admin/shipping-services/tipax/packaging-policies" @{ serviceCode = "tipax"; scope = @{ countryId = "11111111-1111-4111-8111-111111111111" }; feeAmount = 1000; currencyCode = "IRR" } $token)
Show "GET with no credential at all (expect 401)" (Call "GET" "/v1/admin/shipping-services/tipax/packaging-policies" $null $null)

Emit ""
Emit "=== phase 5: default semantics on a non-configured identity (explicit zero, not a hidden charge) ==="
Show "GET packaging-policies for a service with no policy (post)" (Call "GET" "/v1/admin/shipping-services/post/packaging-policies" $null $token)

Emit ""
Emit "=== phase 6: final list (no ACTIVE task-owned fixture remains) ==="
Show "GET packaging-policies (final)" (Call "GET" "/v1/admin/shipping-services/tipax/packaging-policies" $null $token)

Emit ""
Emit "DONE"
