$ErrorActionPreference = 'Continue'
$B = 'http://127.0.0.1:5101'
$log = "$PSScriptRoot\log-http-064.txt"
$script:out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { $script:out.Add($s); Write-Host $s }
function Req($name, $m, $u, $h, $b, $ct) {
    try {
        $p = @{ Uri = $u; Method = $m; UseBasicParsing = $true; TimeoutSec = 30 }
        if ($h) { $p.Headers = $h }
        if ($b -ne $null) { $p.Body = $b; $p.ContentType = if ($ct) { $ct } else { 'application/json' } }
        $r = Invoke-WebRequest @p
        $body = $r.Content
        if ($body.Length -gt 600) { $body = $body.Substring(0, 600) + '...<truncated>' }
        Emit "$name|STATUS=$($r.StatusCode)|BODY=$body"
        return $r.Content
    } catch {
        $resp = $_.Exception.Response
        if ($resp) {
            $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $t = $sr.ReadToEnd()
            if ($t.Length -gt 600) { $t = $t.Substring(0, 600) + '...<truncated>' }
            Emit "$name|STATUS=$([int]$resp.StatusCode)|BODY=$t"
        } else { Emit "$name|STATUS=TRANSPORT_ERROR|BODY=$($_.Exception.Message)" }
        return $null
    }
}

Emit "=== TB-SHIPPING-MVP-OPERATIONAL-REPAIR-064 isolated HTTP matrix (scratch only) ==="
Emit ""

$A = ((Invoke-WebRequest -Uri "$B/v1/admin/dev-context" -UseBasicParsing -TimeoutSec 20).Content | ConvertFrom-Json).actorUserId
$ha = @{ 'X-Tooba-Dev-Actor-User-Id' = $A }
$hu = @{ 'X-Tooba-Dev-Actor-User-Id' = '00000000-0000-0000-0000-000000000123' }
Emit "ADMIN_ACTOR=$A"

$svc = 'svc-064'
Emit ""
Emit "--- 1. F-063-1 AFTER: malformed / unreadable body must be HTTP 400 stable declared code ---"
Req 'S1 syntax-invalid JSON (063 S1)'            'POST' "$B/v1/admin/shipping-services/$svc/rates" $ha '{not-json'
Req 'S2 invalid/empty GUID in scope (063 S2)'    'POST' "$B/v1/admin/shipping-services/$svc/rates" $ha '{"serviceCode":"svc-064","scope":{"countryId":""},"chargeAmount":1000,"currencyCode":"IRR","preparationDays":1,"leadDays":1}'
Req 'S3 unparsable non-GUID in scope'            'POST' "$B/v1/admin/shipping-services/$svc/rates" $ha '{"serviceCode":"svc-064","scope":{"countryId":"not-a-guid"},"chargeAmount":1000,"currencyCode":"IRR","preparationDays":1,"leadDays":1}'
Req 'S4 literal null body'                       'POST' "$B/v1/admin/shipping-services/$svc/rates" $ha 'null'
Req 'S5 wrong content type'                      'POST' "$B/v1/admin/shipping-services/$svc/rates" $ha 'not json at all' 'text/plain'
Req 'S6 malformed body on coverage-grant'        'POST' "$B/v1/admin/shipping-services/$svc/coverage-grants" $ha '{oops'
Req 'S7 malformed body on rate PUT edit'         'PUT'  "$B/v1/admin/shipping-services/$svc/rates/11111111-2222-3333-4444-555555555555" $ha '{bad'
Req 'S8 malformed body on rate active PATCH'     'PATCH' "$B/v1/admin/shipping-services/$svc/rates/11111111-2222-3333-4444-555555555555/active" $ha '{bad'
Req 'S9 malformed body on grant active PATCH'    'PATCH' "$B/v1/admin/shipping-services/$svc/coverage-grants/11111111-2222-3333-4444-555555555555/active" $ha '{bad'
Emit ""

Emit "--- 2. authorization stays fail-closed (including with an unreadable body) ---"
Req 'A1 GET rates, no credential'                'GET' "$B/v1/admin/shipping-services/$svc/rates" $null $null
Req 'A2 GET rates, unknown actor'                'GET' "$B/v1/admin/shipping-services/$svc/rates" $hu $null
Req 'A3 POST malformed body, no credential'      'POST' "$B/v1/admin/shipping-services/$svc/rates" $null '{bad'
Req 'A4 POST malformed body, unknown actor'      'POST' "$B/v1/admin/shipping-services/$svc/rates" $hu '{bad'
Emit ""

Emit "--- 3. 063 missing-scope behavior must be unchanged (400 shipping.validation.scope_required) ---"
Req 'M1 POST rate WITHOUT scope'                 'POST' "$B/v1/admin/shipping-services/$svc/rates" $ha '{"chargeAmount":25000,"currencyCode":"IRR","preparationDays":2,"leadDays":3}'
Req 'M2 POST coverage-grant {}'                  'POST' "$B/v1/admin/shipping-services/$svc/coverage-grants" $ha '{}'
Req 'M3 POST rate scope:{}'                      'POST' "$B/v1/admin/shipping-services/$svc/rates" $ha '{"scope":{},"chargeAmount":25000,"currencyCode":"IRR","preparationDays":2,"leadDays":3}'
Emit ""

Emit "--- 4. authorized read still 200 (no 500) and F-063-2: languages auto-seeded ---"
Req 'B1 GET rates, admin'                        'GET' "$B/v1/admin/shipping-services/$svc/rates" $ha $null
Req 'B2 GET geography countries, admin'          'GET' "$B/v1/admin/geography/countries" $ha $null
Req 'L1 GET languages (no manual POST in 064)'   'GET' "$B/v1/admin/languages" $ha $null
Emit ""

Emit "--- 5. valid authenticated create still works with a real geography id ---"
$countries = (Invoke-WebRequest -Uri "$B/v1/admin/geography/countries" -Headers $ha -UseBasicParsing -TimeoutSec 25).Content | ConvertFrom-Json
$countryId = if ($countries -is [array]) { $countries[0].id } else { $countries.id }
Emit "COUNTRY_ID=$countryId"
if ($countryId) {
    $body = '{"serviceCode":"' + $svc + '","scope":{"countryId":"' + $countryId + '"},"chargeAmount":41000,"currencyCode":"IRR","preparationDays":2,"leadDays":3,"isActive":true}'
    $created = Req 'V1 POST rate valid scope (synthetic scratch value)' 'POST' "$B/v1/admin/shipping-services/$svc/rates" $ha $body
    Req 'V2 GET rates read-after-write'          'GET' "$B/v1/admin/shipping-services/$svc/rates" $ha $null
    $grantBody = '{"serviceCode":"' + $svc + '","scope":{"countryId":"' + $countryId + '"}}'
    Req 'V3 POST coverage-grant (synthetic scratch value)' 'POST' "$B/v1/admin/shipping-services/$svc/coverage-grants" $ha $grantBody
    Req 'V4 GET rates+coverage read-after-write' 'GET' "$B/v1/admin/shipping-services/$svc/rates" $ha $null
} else {
    Emit 'V1..V4 SKIPPED (no seeded country id)'
}

Set-Content -Path $log -Value ($script:out -join "`r`n") -Encoding UTF8
Write-Host "WROTE $log"
