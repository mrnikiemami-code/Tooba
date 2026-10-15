$ErrorActionPreference = 'Continue'
$B = 'http://127.0.0.1:5088'
$A = '01a036c2-970e-7000-8eb7-94bf5cc2d8db'
$E = Join-Path $PSScriptRoot ''
$out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { $script:out.Add($s); Write-Host $s }

$ha = @{ 'X-Tooba-Dev-Actor-User-Id' = $A }

Emit "=== TB-SHIPPING-MVP-SHADOW-PARITY-068 :: target provenance + baseline (mission preflight/B) ==="
Emit "captured at $(Get-Date -Format o)"
Emit ""

$p = Get-CimInstance Win32_Process -Filter "Name='Tooba.Host.exe'" -ErrorAction SilentlyContinue
foreach ($x in $p) {
    Emit "PROCESS pid=$($x.ProcessId) start=$($x.CreationDate)"
    Emit "  cmdline=$($x.CommandLine)"
}
$l = Get-NetTCPConnection -State Listen -LocalPort 5088 -ErrorAction SilentlyContinue
foreach ($x in $l) { Emit "LISTEN $($x.LocalAddress):$($x.LocalPort) ownerPid=$($x.OwningProcess)" }
Emit ""

Emit "--- environment / tenant / edition proof (Development-only route) ---"
try {
    $r = Invoke-WebRequest -Uri "$B/v1/admin/dev-context" -Headers $ha -UseBasicParsing -TimeoutSec 20
    Emit "GET /v1/admin/dev-context => $($r.StatusCode)"
    Emit $r.Content
} catch {
    $resp = $_.Exception.Response
    if ($resp) { $sr = New-Object System.IO.StreamReader($resp.GetResponseStream()); Emit "dev-context => $([int]$resp.StatusCode) $($sr.ReadToEnd())" }
    else { Emit "dev-context => ERR $($_.Exception.Message)" }
}
try {
    $h = Invoke-WebRequest -Uri "$B/health" -UseBasicParsing -TimeoutSec 15
    Emit "GET /health => $($h.StatusCode) $($h.Content)"
} catch { Emit "health => ERR $($_.Exception.Message)" }
Emit ""

Emit "--- baseline Shipping rate policies / coverage grants per active service code ---"
$services = @('post', 'tipax', 'store_courier', 'in_person', 'snapp_courier')
$baseline = @{}
foreach ($s in $services) {
    try {
        $r = Invoke-WebRequest -Uri "$B/v1/admin/shipping-services/$s/rates" -Headers $ha -UseBasicParsing -TimeoutSec 25
        $j = $r.Content | ConvertFrom-Json
        $rates = @($j.ratePolicies); $grants = @($j.coverageGrants)
        $activeRates = @($rates | Where-Object { $_.isActive })
        $activeGrants = @($grants | Where-Object { $_.isActive })
        Emit "$s : ratePolicies=$($rates.Count) activeRates=$($activeRates.Count) coverageGrants=$($grants.Count) activeGrants=$($activeGrants.Count)"
        foreach ($x in $rates) { Emit ("   RATE  {0} level={1} area={2} city={3} charge={4} {5} prep={6} lead={7} free={8} active={9}" -f $x.ratePolicyId, $x.level, $x.administrativeAreaId, $x.cityId, $x.chargeAmount, $x.currencyCode, $x.preparationDays, $x.leadDays, $x.freeAboveThreshold, $x.isActive) }
        foreach ($x in $grants) { Emit ("   GRANT {0} level={1} area={2} city={3} active={4}" -f $x.coverageGrantId, $x.level, $x.administrativeAreaId, $x.cityId, $x.isActive) }
    } catch {
        $resp = $_.Exception.Response
        if ($resp) { $sr = New-Object System.IO.StreamReader($resp.GetResponseStream()); Emit "$s => $([int]$resp.StatusCode) $($sr.ReadToEnd())" }
        else { Emit "$s => ERR $($_.Exception.Message)" }
    }
}
Emit ""
Emit "--- policy/grant state BEFORE this task (065 fixtures must be accounted for) ---"
Emit "065 fixtures known from evidence: 3 rate policies + 3 coverage grants on service 'post', all INACTIVE."
Emit ""

$out -join "`r`n" | Set-Content "$PSScriptRoot\log-baseline.txt" -Encoding UTF8
Write-Host "WROTE log-baseline.txt"
