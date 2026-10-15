$ErrorActionPreference = "Continue"
$root = "http://127.0.0.1:5088"
$logDir = Split-Path -Parent $PSCommandPath
$log = Join-Path $logDir "log-dev-restore.txt"
Set-Content -Path $log -Value @"
TB-SHIPPING-FULL-OPERATIONS-076 - Development fixture restoration ledger.

The 076 probe needs an ACTIVE packaging row and an ACTIVE express schedule to prove the composed split
and the window verdicts. Both are Development-only synthetic data on tooba_alpha and both are returned
to the state recorded by TB-SHIPPING-FULL-OPERATIONS-075 before this task closes.
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
    if ($b.Length -gt 1200) { $b = $b.Substring(0, 1200) + "...(truncated)" }
    Emit "$label -> $($result.Status) $b"
}

$login = Call "POST" "/v1/auth/login" @{ identifierKind = "Email"; identifier = "admin-actor@tooba.local"; password = "admin-dev-horse-1" } $null
$token = ($login.Body | ConvertFrom-Json).accessToken
Emit "accessToken acquired: $([bool]$token)"

Emit ""
Emit "=== target identity ==="
Show "GET /__platform-commerce" (Call "GET" "/__platform-commerce" $null $null)

Emit ""
Emit "=== restore the express packaging row to the 075-recorded ledger state (15,000 IRR Prepaid, inactive) ==="
$restore = @{
    serviceCode = "tipax"
    scope = @{ countryId = "e3451206-e503-51a8-91ad-afa9ab70e330"; administrativeAreaId = "1f8050a8-c325-59fa-959f-82d76b5b2bc5" }
    feeAmount = 15000
    currencyCode = "IRR"
    freightPayerMode = "Prepaid"
    optionCode = "express"
}
Show "PUT packaging-policies/01a13e7c-cfde-7000-98cc-f79246cf97b0" (Call "PUT" "/v1/admin/shipping-services/tipax/packaging-policies/01a13e7c-cfde-7000-98cc-f79246cf97b0" $restore $token)
Show "PATCH packaging-policies/01a13e7c-cfde-7000-98cc-f79246cf97b0/active {isActive:false}" (Call "PATCH" "/v1/admin/shipping-services/tipax/packaging-policies/01a13e7c-cfde-7000-98cc-f79246cf97b0/active" @{ isActive = $false } $token)

Emit ""
Emit "=== the 076-created express schedule stays inactive (no delete surface exists) ==="
Show "PATCH operating-schedules/01a13eaf-2710-7000-9147-ae5a28967251/active {isActive:false}" (Call "PATCH" "/v1/admin/shipping-services/tipax/operating-schedules/01a13eaf-2710-7000-9147-ae5a28967251/active" @{ isActive = $false } $token)

Emit ""
Emit "=== final Development state ==="
Show "GET packaging-policies" (Call "GET" "/v1/admin/shipping-services/tipax/packaging-policies" $null $token)
Show "GET operating-schedules" (Call "GET" "/v1/admin/shipping-services/tipax/operating-schedules" $null $token)
Show "GET rates" (Call "GET" "/v1/admin/shipping-services/tipax/rates" $null $token)

Emit ""
Emit "DONE"
