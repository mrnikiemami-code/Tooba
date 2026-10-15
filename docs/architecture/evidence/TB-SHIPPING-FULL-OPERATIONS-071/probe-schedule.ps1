$ErrorActionPreference = 'Continue'
$B = 'http://127.0.0.1:5088'
$A = '01a036c2-970e-7000-8eb7-94bf5cc2d8db'
$ha = @{ 'X-Tooba-Dev-Actor-User-Id' = $A }
function Emit([string]$s) { Write-Host $s }

function Call([string]$method, [string]$url, [string]$body, [hashtable]$headers) {
    try {
        if ($body) {
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($body)
            $r = Invoke-WebRequest -Uri $url -Method $method -Headers $headers -ContentType 'application/json; charset=utf-8' -Body $bytes -UseBasicParsing -TimeoutSec 30
        } else {
            $r = Invoke-WebRequest -Uri $url -Method $method -Headers $headers -UseBasicParsing -TimeoutSec 30
        }
        Emit "$method $url => $($r.StatusCode)"
        Emit "   $($r.Content)"
        return $r
    } catch {
        $resp = $_.Exception.Response
        if ($resp) {
            $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
            Emit "$method $url => $([int]$resp.StatusCode)"
            Emit "   $($sr.ReadToEnd())"
        } else {
            Emit "$method $url => ERR $($_.Exception.Message)"
        }
        return $null
    }
}

Emit "=== TB-SHIPPING-FULL-OPERATIONS-071 :: live operating-schedule admin roundtrip ==="
Emit "target: existing Development host :5088, tenant store-alpha, DB tooba_alpha"
Emit "actor: existing canonical Development admin actor header"
Emit ""

Emit "--- 0. positive target provenance (Development, no production/staging) ---"
try {
    $pc = Invoke-WebRequest -Uri "$B/__platform-commerce" -Headers $ha -UseBasicParsing -TimeoutSec 15
    Emit "GET /__platform-commerce => $($pc.StatusCode)"
    Emit "   $($pc.Content)"
} catch { Emit "GET /__platform-commerce => ERR $($_.Exception.Message)" }
Emit ""

Emit "--- 1. read: no schedule exists yet (empty set is a real answer, never a default) ---"
Call 'GET' "$B/v1/admin/shipping-services/tipax/operating-schedules" $null $ha | Out-Null
Emit ""

Emit "--- 2. create: Sunday-only 13:00-14:00 plus a second distinct window 16:30-18:00 (capacity 25) ---"
$createBody = @'
{"serviceCode":"tipax","timeZoneId":"Asia/Tehran","optionCode":"standard","windows":[{"weekday":7,"startTime":"13:00:00","endTime":"14:00:00"},{"weekday":7,"startTime":"16:30:00","endTime":"18:00:00","capacity":25}]}
'@
$created = Call 'PUT' "$B/v1/admin/shipping-services/tipax/operating-schedules" $createBody $ha
$scheduleId = $null
if ($created) {
    $parsed = $created.Content | ConvertFrom-Json
    $scheduleId = $parsed.data.scheduleId
    if (-not $scheduleId) { $scheduleId = $parsed.scheduleId }
    Emit "   SCHEDULE_ID=$scheduleId"
    Emit "   WINDOWS=$($parsed.data.windows.Count) TZ=$($parsed.data.timeZoneId) CAP_TOTAL=$($parsed.data.configuredCapacityTotal)"
}
Emit ""

Emit "--- 3. persisted and readable: real row in tooba_alpha ---"
Call 'GET' "$B/v1/admin/shipping-services/tipax/operating-schedules" $null $ha | Out-Null
Emit ""

Emit "--- 4. repeat the same body: idempotent (same schedule id, same window set) ---"
$again = Call 'PUT' "$B/v1/admin/shipping-services/tipax/operating-schedules" $createBody $ha
if ($again) {
    $parsedAgain = $again.Content | ConvertFrom-Json
    $againId = $parsedAgain.data.scheduleId
    if (-not $againId) { $againId = $parsedAgain.scheduleId }
    Emit "   SAME_ID=$($againId -eq $scheduleId) WINDOWS=$($parsedAgain.data.windows.Count)"
}
Emit ""

Emit "--- 5. refusals with stable codes ---"
Emit "5a. overlapping windows on the same weekday (13:00-15:00 vs 14:00-16:00)"
$overlapBody = '{"serviceCode":"tipax","timeZoneId":"Asia/Tehran","optionCode":"standard","windows":[{"weekday":7,"startTime":"13:00:00","endTime":"15:00:00"},{"weekday":7,"startTime":"14:00:00","endTime":"16:00:00"}]}'
Call 'PUT' "$B/v1/admin/shipping-services/tipax/operating-schedules" $overlapBody $ha | Out-Null
Emit ""
Emit "5b. reversed window (14:00-13:00)"
$reversedBody = '{"serviceCode":"tipax","timeZoneId":"Asia/Tehran","optionCode":"standard","windows":[{"weekday":7,"startTime":"14:00:00","endTime":"13:00:00"}]}'
Call 'PUT' "$B/v1/admin/shipping-services/tipax/operating-schedules" $reversedBody $ha | Out-Null
Emit ""
Emit "5c. overnight window (22:00-02:00), explicitly not supported"
$overnightBody = '{"serviceCode":"tipax","timeZoneId":"Asia/Tehran","optionCode":"standard","windows":[{"weekday":7,"startTime":"22:00:00","endTime":"02:00:00"}]}'
Call 'PUT' "$B/v1/admin/shipping-services/tipax/operating-schedules" $overnightBody $ha | Out-Null
Emit ""
Emit "5d. weekday outside ISO 1..7 (0)"
$weekdayBody = '{"serviceCode":"tipax","timeZoneId":"Asia/Tehran","optionCode":"standard","windows":[{"weekday":0,"startTime":"13:00:00","endTime":"14:00:00"}]}'
Call 'PUT' "$B/v1/admin/shipping-services/tipax/operating-schedules" $weekdayBody $ha | Out-Null
Emit ""
Emit "5e. unknown option code the tipax service does not own (drone)"
$unknownOptionBody = '{"serviceCode":"tipax","timeZoneId":"Asia/Tehran","optionCode":"drone","windows":[{"weekday":7,"startTime":"13:00:00","endTime":"14:00:00"}]}'
Call 'PUT' "$B/v1/admin/shipping-services/tipax/operating-schedules" $unknownOptionBody $ha | Out-Null
Emit ""
Emit "5f. unresolvable timezone"
$badTzBody = '{"serviceCode":"tipax","timeZoneId":"Nowhere/Fake","optionCode":"standard","windows":[{"weekday":7,"startTime":"13:00:00","endTime":"14:00:00"}]}'
Call 'PUT' "$B/v1/admin/shipping-services/tipax/operating-schedules" $badTzBody $ha | Out-Null
Emit ""

Emit "--- 6. authorization boundary: unauthenticated (no actor header) must be 401 ---"
Call 'GET' "$B/v1/admin/shipping-services/tipax/operating-schedules" $null @{} | Out-Null
Emit ""

Emit "--- 7. deactivate + reactivate through the same route the UI toggle uses ---"
if ($scheduleId) {
    Call 'PATCH' "$B/v1/admin/shipping-services/tipax/operating-schedules/$scheduleId/active" '{"isActive":false}' $ha | Out-Null
    Emit ""
    Call 'PATCH' "$B/v1/admin/shipping-services/tipax/operating-schedules/$scheduleId/active" '{"isActive":true}' $ha | Out-Null
    Emit ""
    Emit "--- 8. missing schedule id and cross-service toggle must be refused ---"
    Call 'PATCH' "$B/v1/admin/shipping-services/tipax/operating-schedules/01a13000-0000-7000-8000-000000000001/active" '{"isActive":false}' $ha | Out-Null
    Emit ""
    Call 'PATCH' "$B/v1/admin/shipping-services/post/operating-schedules/$scheduleId/active" '{"isActive":false}' $ha | Out-Null
    Emit ""
}

Emit "--- 9. final state ---"
Call 'GET' "$B/v1/admin/shipping-services/tipax/operating-schedules" $null $ha | Out-Null
