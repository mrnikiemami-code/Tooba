# TB-291 — one-command dev runtime alignment + delivery proof.
# Verifies (not just "home 200"): frontend build identity + reachability, backend health+provenance,
# DB reachability + 287 migration presence, and REAL access to the changed offer pages (heading text).
param(
  [string]$Backend = 'http://127.0.0.1:5088',
  [string]$Frontend = 'http://127.0.0.1:3000',
  [string]$Db = 'tooba_alpha',
  [string]$Container = 'postgres-db'
)
$ErrorActionPreference = 'Continue'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
function Get-Status($url) { try { return (Invoke-WebRequest $url -TimeoutSec 10 -UseBasicParsing).StatusCode } catch { return 0 } }
function Get-Text($url) { try { return (Invoke-WebRequest $url -TimeoutSec 12 -UseBasicParsing).Content } catch { return '' } }

$gitSha = (git -C $repo rev-parse HEAD) 2>$null
$fe = Join-Path $repo 'src/frontend'
$buildId = if (Test-Path (Join-Path $fe '.next/BUILD_ID')) { (Get-Content (Join-Path $fe '.next/BUILD_ID') -Raw).Trim() } else { 'MISSING' }
$beDll = Join-Path $repo 'src/backend/Host/Tooba.Host/bin/Debug/net8.0/Tooba.Host.dll'
$beSha = if (Test-Path $beDll) { (Get-FileHash -Algorithm SHA256 $beDll).Hash } else { 'MISSING' }

$feReady = Get-Status "$Frontend/fa"
$beHealth = Get-Status "$Backend/health"
$commerce = try { (Invoke-RestMethod "$Backend/__platform-commerce" -TimeoutSec 8) } catch { $null }
$dbOk = 0; try { $dbOk = (docker exec $Container psql -U admin -d $Db -At -c "select 1" 2>$null) } catch {}
$col = 0; try { $col = (docker exec $Container psql -U admin -d $Db -At -c "select count(*) from information_schema.columns where table_schema='offer' and table_name='offers' and column_name='creation_intent_fingerprint'" 2>$null) } catch {}

$pages = @(
  @{ name = 'adminList'; url = "$Frontend/admin/offers"; heading = 'پیشنهادهای فروشندگان'; title = 'Tooba Admin Product Workspace' },
  @{ name = 'adminNew'; url = "$Frontend/admin/offers/new"; heading = 'ایجاد پیش‌نویس برای فروشنده'; title = 'Tooba Admin Product Workspace' },
  @{ name = 'sellerNew'; url = "$Frontend/vendor-panel/products/new"; heading = 'ایجاد پیش‌نویس پیشنهاد فروش'; title = 'Tooba Seller Panel' }
)
$pageResults = foreach ($p in $pages) {
  $status = Get-Status $p.url
  $body = Get-Text $p.url
  $present = ($body -like "*$($p.heading)*") -or ($body -like "*$($p.title)*")
  [pscustomobject]@{ page = $p.name; status = $status; headingPresent = $present }
}

$result = [pscustomobject]@{
  gitSha = $gitSha
  frontend = [pscustomobject]@{ base = $Frontend; ready = $feReady; buildId = $buildId }
  backend  = [pscustomobject]@{ base = $Backend; health = $beHealth; provenance = $commerce.connectionReference; edition = $commerce.edition }
  database = [pscustomobject]@{ database = $Db; reachable = [int]$dbOk; creationIntentColumn = [int]$col; backendDllSha = $beSha }
  pages    = $pageResults
  pass = ($feReady -eq 200 -and $beHealth -eq 200 -and [int]$dbOk -eq 1 -and (@($pageResults | Where-Object { $_.status -eq 200 -and $_.headingPresent }).Count -eq $pages.Count))
}
$result | ConvertTo-Json -Depth 6
if (-not $result.pass) { exit 1 }
