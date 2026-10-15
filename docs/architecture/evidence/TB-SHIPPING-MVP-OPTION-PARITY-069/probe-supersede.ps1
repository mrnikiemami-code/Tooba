$ErrorActionPreference = 'Continue'
$ha = @{ 'X-Tooba-Dev-Actor-User-Id' = '01a036c2-970e-7000-8eb7-94bf5cc2d8db' }
$id = '01a13d65-cf37-7000-ad21-ac8e9b2c89d7'
$body = '{"isActive":false}'
try {
    $r = Invoke-WebRequest -Uri "http://127.0.0.1:5088/v1/admin/shipping-services/tipax/rates/$id/active" -Method PATCH -Headers $ha -ContentType 'application/json' -Body $body -UseBasicParsing -TimeoutSec 25
    "DEACTIVATE_GENERIC_TIPAX => $($r.StatusCode) $($r.Content)"
} catch {
    $resp = $_.Exception.Response
    if ($resp) { $sr = New-Object System.IO.StreamReader($resp.GetResponseStream()); "DEACTIVATE_GENERIC_TIPAX => $([int]$resp.StatusCode) $($sr.ReadToEnd())" }
    else { "DEACTIVATE_GENERIC_TIPAX => ERR $($_.Exception.Message)" }
}
try {
    $r2 = Invoke-WebRequest -Uri "http://127.0.0.1:5088/v1/admin/shipping-services/tipax/rates" -Headers $ha -UseBasicParsing -TimeoutSec 25
    "AFTER => $($r2.Content)"
} catch { "AFTER => ERR $($_.Exception.Message)" }
