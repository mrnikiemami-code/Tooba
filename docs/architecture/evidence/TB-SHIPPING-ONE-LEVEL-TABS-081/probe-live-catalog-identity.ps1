$ErrorActionPreference = "Continue"
$root = "http://127.0.0.1:5088"
$logDir = Split-Path -Parent $PSCommandPath
$log = Join-Path $logDir "log-live-catalog-identity.txt"
Set-Content -Path $log -Value @"
TB-SHIPPING-ONE-LEVEL-TABS-081 - READ-ONLY live Development catalog identity inventory
Target: GET /__platform-commerce on http://127.0.0.1:5088 (Development; host NOT restarted).
READ-ONLY: only GETs. No service, option, rate, coverage, schedule or packaging row is written.
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
Emit "=== phase 0: target identity ==="
Show "GET /__platform-commerce" (Call "GET" "/__platform-commerce" $null $null)

Emit ""
Emit "=== phase 1: read-only authenticated catalog read ==="
$login = Call "POST" "/v1/auth/login" @{ identifierKind = "Email"; identifier = "admin-actor@tooba.local"; password = "admin-dev-horse-1" } $null
Show "POST /v1/auth/login" $login
$token = ($login.Body | ConvertFrom-Json).accessToken
Emit "accessToken acquired: $([bool]$token)"

Show "GET /v1/admin/shipping-services (authenticated)" (Call "GET" "/v1/admin/shipping-services" $null $token)
Show "GET /v1/admin/shipping-methods?language=fa" (Call "GET" "/v1/admin/shipping-methods?language=fa" $null $token)
Show "GET /v1/admin/shipping-methods?language=en" (Call "GET" "/v1/admin/shipping-methods?language=en" $null $token)

Emit ""
Emit "=== phase 2: per-service rate/coverage/packaging/schedule identity (read-only) ==="
$list = Unwrap (Call "GET" "/v1/admin/shipping-services" $null $token).Body
if ($list) {
    foreach ($row in $list) {
        $code = $row.code
        Show "GET /v1/admin/shipping-services/$code/rates" (Call "GET" "/v1/admin/shipping-services/$code/rates" $null $token)
        Show "GET /v1/admin/shipping-services/$code/packaging-policies" (Call "GET" "/v1/admin/shipping-services/$code/packaging-policies" $null $token)
        Show "GET /v1/admin/shipping-services/$code/operating-schedules" (Call "GET" "/v1/admin/shipping-services/$code/operating-schedules" $null $token)
    }
}

Emit ""
Emit "=== phase 3: storefront legacy projection shape (read-only, gate OFF) ==="
Emit "(No cart id is fabricated; the projection endpoint is not called with a synthetic cart.)"
