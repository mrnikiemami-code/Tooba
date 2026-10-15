$ErrorActionPreference = 'Continue'
$Base = 'http://127.0.0.1:5088'
$log = Join-Path $PSScriptRoot 'log-final-dev-inventory.txt'
$out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { $script:out.Add($s); Write-Host $s }

$login = Invoke-RestMethod -Uri "$Base/v1/auth/login" -Method POST -ContentType 'application/json' `
    -Body '{"identifierKind":"Email","identifier":"admin-actor@tooba.local","password":"admin-dev-horse-1"}' -TimeoutSec 30
$AH = @{ 'Authorization' = "Bearer $($login.accessToken)"; 'Accept-Language' = 'fa' }

function Get2($path) {
    try { $r = Invoke-WebRequest -Uri "$Base$path" -Headers $AH -UseBasicParsing -TimeoutSec 30; return $r.Content }
    catch { $resp = $_.Exception.Response; if ($resp) { $sr = New-Object System.IO.StreamReader($resp.GetResponseStream()); return "HTTP $([int]$resp.StatusCode) $($sr.ReadToEnd())" }; return "ERR $($_.Exception.Message)" }
}

Emit "=== TB-SHIPPING-FULL-OPERATIONS-080 :: final read-only Development fixture inventory : $(Get-Date -Format o) ==="
Emit "target: dev-singlestore / store-alpha / tenant-alpha -> tooba_alpha on 127.0.0.1:5088"
Emit ""

foreach ($svc in 'post', 'tipax', 'snapp_courier', 'store_courier', 'in_person') {
    Emit "--- $svc ---"
    Emit "rates: $(Get2 "/v1/admin/shipping-services/$svc/rates")"
    Emit "packaging: $(Get2 "/v1/admin/shipping-services/$svc/packaging-policies")"
    Emit "schedules: $(Get2 "/v1/admin/shipping-services/$svc/operating-schedules")"
    Emit ""
}

$out -join "`r`n" | Set-Content $log -Encoding UTF8
Write-Host "WROTE $log"
