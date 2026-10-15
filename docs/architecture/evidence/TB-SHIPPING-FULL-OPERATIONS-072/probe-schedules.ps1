$ErrorActionPreference = 'Continue'
$B = 'http://127.0.0.1:5088'
$A = '01a036c2-970e-7000-8eb7-94bf5cc2d8db'
$ha = @{ 'X-Tooba-Dev-Actor-User-Id' = $A }

function Emit([string]$s) { Write-Host $s }

function Get-Schedules([string]$code, [hashtable]$headers) {
    try {
        $r = Invoke-WebRequest -Uri "$B/v1/admin/shipping-services/$code/operating-schedules" -Headers $headers -UseBasicParsing -TimeoutSec 30
        Emit "GET $code => $($r.StatusCode)"
        Emit "   $($r.Content)"
    } catch {
        $resp = $_.Exception.Response
        if ($resp) {
            $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
            Emit "GET $code => $([int]$resp.StatusCode)"
            Emit "   $($sr.ReadToEnd())"
        } else {
            Emit "GET $code => ERR $($_.Exception.Message)"
        }
    }
}

Emit "=== TB-SHIPPING-FULL-OPERATIONS-072 :: read-only operating-schedule isolation probe ==="
Emit "target: existing Development host :5088, tenant store-alpha, DB tooba_alpha"
Emit "actor: existing canonical Development admin actor header"
Emit "This probe performs GET only. It never writes; the live browser proof in ui-*.png did the writes."
Emit ""

Emit "--- 1. tipax: the 071 fixture restored by the UI (Sunday 13:00-14:00 + 16:30-18:00, capacity 25) ---"
Get-Schedules 'tipax' $ha
Emit ""

Emit "--- 2. isolation: the other shipping services must have no schedule row ---"
foreach ($code in @('post', 'snapp_courier', 'store_courier', 'in_person')) {
    Get-Schedules $code $ha
    Emit ""
}

Emit "--- 3. authorization boundary: no actor header must be 401 admin.actor.missing ---"
Get-Schedules 'tipax' @{}
Emit ""

Emit "--- 4. provenance: Development platform descriptor, never production/staging ---"
try {
    $pc = Invoke-WebRequest -Uri "$B/__platform-commerce" -Headers $ha -UseBasicParsing -TimeoutSec 15
    Emit "GET /__platform-commerce => $($pc.StatusCode)"
    Emit "   $($pc.Content)"
} catch { Emit "GET /__platform-commerce => ERR $($_.Exception.Message)" }
