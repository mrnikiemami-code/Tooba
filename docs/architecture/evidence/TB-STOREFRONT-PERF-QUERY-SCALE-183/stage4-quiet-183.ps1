# TB-183 stage 4 — quiet default leg (committed Warning EF level) on the same size-B state.
$ErrorActionPreference = 'Continue'
$repo = (Get-Location).Path
$runDir = Join-Path $repo '.tmp-183-run'
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-STOREFRONT-PERF-QUERY-SCALE-183'
$progress = Join-Path $evidenceDir 'progress-183.log'
$HostPort = 5454; $Locale = 'fa'
$hostOut = Join-Path $runDir 'host'
function Log([string]$m) { Add-Content -LiteralPath $progress -Value "$(Get-Date -Format 'HH:mm:ss') $m" -Encoding utf8 }
if (Get-NetTCPConnection -LocalPort $HostPort -State Listen -ErrorAction SilentlyContinue) { throw "port $HostPort busy" }

Remove-Item -Path 'Env:Logging__LogLevel__Microsoft.EntityFrameworkCore' -ErrorAction SilentlyContinue
$env:ASPNETCORE_URLS = "http://127.0.0.1:$HostPort"
$env:DOTNET_ENVIRONMENT = 'Development'
$stdout = Join-Path $runDir 'h-quiet.jsonl'; $stderr = Join-Path $runDir 'h-quiet-err.txt'
Remove-Item -Force -ErrorAction SilentlyContinue $stdout, $stderr
$proc = Start-Process -FilePath (Join-Path $hostOut 'Tooba.Host.exe') -WorkingDirectory $hostOut -PassThru -NoNewWindow -RedirectStandardOutput $stdout -RedirectStandardError $stderr
$ok = $false; $t0 = Get-Date
while (((Get-Date) - $t0).TotalSeconds -lt 300) {
    if ($proc.HasExited) { break }
    try { if ((Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/health" -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200) { $ok = $true; break } } catch { }
    Start-Sleep -Milliseconds 700
}
if (-not $ok) { try { Stop-Process -Id $proc.Id -Force } catch { }; throw 'quiet host not healthy' }
$owner = (Get-NetTCPConnection -LocalPort $HostPort -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1).OwningProcess
if ($owner -ne $proc.Id) { try { Stop-Process -Id $proc.Id -Force } catch { }; throw "listener pid $owner != $($proc.Id)" }
Set-Content -LiteralPath (Join-Path $runDir 'host-pid-quiet.txt') -Value $proc.Id
Log "quiet host up pid=$($proc.Id) (committed Warning EF default)"
Start-Sleep -Seconds 15

$uri = "http://127.0.0.1:$HostPort/v1/storefront/home?locale=$Locale"
$reqs = @()
foreach ($phase in @('warmup','m1','m2','m3')) {
    $tid = -join ((1..32) | ForEach-Object { '0123456789abcdef'[(Get-Random -Maximum 16)] })
    $sid = -join ((1..16) | ForEach-Object { '0123456789abcdef'[(Get-Random -Maximum 16)] })
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $r = Invoke-WebRequest -Uri $uri -UseBasicParsing -Headers @{ Host='alpha.localhost'; traceparent="00-$tid-$sid-01" } -TimeoutSec 120
    $sw.Stop()
    $bytes = [System.Text.Encoding]::UTF8.GetByteCount($r.Content)
    $sha = (Get-FileHash -InputStream ([System.IO.MemoryStream]::new([System.Text.Encoding]::UTF8.GetBytes($r.Content))) -Algorithm SHA256).Hash
    if ($phase -eq 'm1') { Set-Content -LiteralPath (Join-Path $evidenceDir 'home-size-b-quiet.json') -Value $r.Content -Encoding utf8 }
    $reqs += [pscustomobject]@{ tag='size-b-quiet'; phase=$phase; traceId=$tid; status=$r.StatusCode; wallMs=[math]::Round($sw.Elapsed.TotalMilliseconds,1); payloadBytes=$bytes; bodySha256=$sha }
    Log ("size-b-quiet/${phase}: status=" + $r.StatusCode + " ms=" + [math]::Round($sw.Elapsed.TotalMilliseconds,1) + " bytes=" + $bytes + " sha=" + $sha.Substring(0,12))
}
$reqs | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $runDir 'requests-quiet-183.json') -Encoding utf8
Start-Sleep -Seconds 2
try { Stop-Process -Id $proc.Id -Force } catch { }
Start-Sleep -Seconds 2
$logBytes = (Get-Item $stdout).Length
$efLines = @(Select-String -Path $stdout -Pattern 'Microsoft.EntityFrameworkCore.Database.Command' -SimpleMatch).Count
$hitTotal = 0
foreach ($r in $reqs) { $hitTotal += @(Select-String -Path $stdout -Pattern $r.traceId -SimpleMatch).Count }
Log ("quiet leg: logBytes=$logBytes efCommandLines=$efLines traceHits=$hitTotal")
"logBytes=$logBytes efCommandLines=$efLines traceHits=$hitTotal"
"port5454=" + [bool](Get-NetTCPConnection -LocalPort 5454 -State Listen -ErrorAction SilentlyContinue)
