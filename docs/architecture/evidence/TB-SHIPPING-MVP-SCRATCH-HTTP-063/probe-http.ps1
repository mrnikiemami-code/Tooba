$ErrorActionPreference = 'Continue'
$B = 'http://127.0.0.1:5100'
$log = "$PSScriptRoot\log-http-probes-063.txt"
$script:out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { $script:out.Add($s); Write-Host $s }
function Req($name, $m, $u, $h, $b) {
    try {
        $p = @{ Uri = $u; Method = $m; UseBasicParsing = $true; TimeoutSec = 30 }
        if ($h) { $p.Headers = $h }
        if ($b) { $p.Body = $b; $p.ContentType = 'application/json' }
        $r = Invoke-WebRequest @p
        $body = $r.Content
        if ($body.Length -gt 900) { $body = $body.Substring(0, 900) + '...<truncated>' }
        Emit "$name|STATUS=$($r.StatusCode)|BODY=$body"
    } catch {
        $resp = $_.Exception.Response
        if ($resp) {
            $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $t = $sr.ReadToEnd()
            if ($t.Length -gt 900) { $t = $t.Substring(0, 900) + '...<truncated>' }
            Emit "$name|STATUS=$([int]$resp.StatusCode)|BODY=$t"
        } else {
            Emit "$name|STATUS=TRANSPORT_ERROR|BODY=$($_.Exception.Message)"
        }
    }
}

Emit "=== TB-SHIPPING-MVP-SCRATCH-HTTP-063 authorized HTTP probe matrix (scratch only) ==="
Emit ""

$dcJson = (Invoke-WebRequest -Uri "$B/v1/admin/dev-context" -UseBasicParsing -TimeoutSec 20).Content
$dc = $dcJson | ConvertFrom-Json
$A = $dc.actorUserId
Emit "DEV_CONTEXT=$dcJson"
Emit ""

$ha = @{ 'X-Tooba-Dev-Actor-User-Id' = $A }
$hu = @{ 'X-Tooba-Dev-Actor-User-Id' = '00000000-0000-0000-0000-000000000123' }

Emit "--- A. authorization boundary ---"
Req 'A1 GET shipping rates, no credential'      'GET' "$B/v1/admin/shipping-services/svc-063/rates" $null $null
Req 'A2 GET shipping rates, unknown actor'      'GET' "$B/v1/admin/shipping-services/svc-063/rates" $hu $null
Req 'A3 GET geography countries, no credential' 'GET' "$B/v1/admin/geography/countries" $null $null
Req 'A4 GET geography countries, unknown actor' 'GET' "$B/v1/admin/geography/countries" $hu $null
Emit ""

Emit "--- B. authorized reads (must be 200, not 500) ---"
Req 'B1 GET shipping rates, admin'    'GET' "$B/v1/admin/shipping-services/svc-063/rates" $ha $null
Req 'B2 GET geography countries, admin' 'GET' "$B/v1/admin/geography/countries" $ha $null
Emit ""

Emit "--- C. NULL-SCOPE regression (must be HTTP 400 validation.failed, never 500) ---"
Req 'C1 POST rate WITHOUT scope'           'POST' "$B/v1/admin/shipping-services/svc-063/rates" $ha '{"chargeAmount":25000,"currencyCode":"IRR","preparationDays":2,"leadDays":3,"isActive":true}'
Req 'C2 POST coverage-grant WITHOUT scope' 'POST' "$B/v1/admin/shipping-services/svc-063/coverage-grants" $ha '{}'
Req 'C3 POST rate with EXPLICIT null scope'           'POST' "$B/v1/admin/shipping-services/svc-063/rates" $ha '{"scope":null,"chargeAmount":25000,"currencyCode":"IRR","preparationDays":2,"leadDays":3,"isActive":true}'
Req 'C4 POST coverage-grant with EXPLICIT null scope' 'POST' "$B/v1/admin/shipping-services/svc-063/coverage-grants" $ha '{"scope":null}'
Emit ""

Emit "--- D. invalid / cross-service mutations (must be stable errors, never 500) ---"
Req 'D1 POST rate with UNKNOWN geography id' 'POST' "$B/v1/admin/shipping-services/svc-063/rates" $ha '{"scope":{"countryId":"11111111-2222-3333-4444-555555555555"},"chargeAmount":25000,"currencyCode":"IRR","preparationDays":2,"leadDays":3,"isActive":true}'
Req 'D2 POST grant with UNKNOWN geography id' 'POST' "$B/v1/admin/shipping-services/svc-063/coverage-grants" $ha '{"scope":{"countryId":"11111111-2222-3333-4444-555555555555"}}'
Req 'D3 PATCH rate active under a DIFFERENT service code' 'PATCH' "$B/v1/admin/shipping-services/other-service-063/rates/11111111-2222-3333-4444-555555555555/active" $ha '{"isActive":false}'
Emit ""

Emit "--- E. scope validity checks that need no geography rows ---"
Req 'E1 POST rate with scope but no countryId' 'POST' "$B/v1/admin/shipping-services/svc-063/rates" $ha '{"scope":{},"chargeAmount":25000,"currencyCode":"IRR","preparationDays":2,"leadDays":3,"isActive":true}'
Req 'E2 POST rate with cityId but serviceAreaId' 'POST' "$B/v1/admin/shipping-services/svc-063/rates" $ha '{"scope":{"countryId":"11111111-2222-3333-4444-555555555555","cityId":"21111111-2222-3333-4444-555555555555","serviceAreaId":"31111111-2222-3333-4444-555555555555"},"chargeAmount":25000,"currencyCode":"IRR","preparationDays":2,"leadDays":3,"isActive":true}'

Set-Content -Path $log -Value ($script:out -join "`r`n") -Encoding UTF8
Write-Host "WROTE $log"
