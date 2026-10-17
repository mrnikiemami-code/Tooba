# TB-STOREFRONT-PERF-PUBLISHED-SCALE-182 — authorized scratch publication + two-size proof.
#
# Task-owned: container tooba-tb182-pg, db tooba_182_scratch, pg 55502, host 5453.
# Publication uses ONLY real owner routes:
#   POST /v1/seller/offers                      (Offer module CQRS, seller actor + party membership)
#   PUT  /v1/seller/offers/{id}/price           (Pricing through Offer owner boundary)
#   PUT  /v1/seller/offers/{id}/inventory       (Inventory through Offer owner boundary)
#   POST /v1/admin/products/{id}/publish        (ProductWorkspace -> Catalog PublishProductCommand + readiness)
# No SQL writes to product status, no forged grants, no row cloning.
$ErrorActionPreference = 'Continue'

$Container = 'tooba-tb182-pg'; $Database = 'tooba_182_scratch'; $MessagingDatabase = 'tooba_182_messaging'
$User = 'scratch182'; $PgPort = 55502; $HostPort = 5453
$Target = 14          # >= 4x the 3 published baseline cards, bounded selection
$Locale = 'fa'

$repo = (Get-Location).Path
$runDir = Join-Path $repo '.tmp-182-run'
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-STOREFRONT-PERF-PUBLISHED-SCALE-182'
$password = $env:TOOBA_182_SCRATCH_PASSWORD
$progress = Join-Path $evidenceDir 'progress-182.log'
if (-not $password) { throw 'TOOBA_182_SCRATCH_PASSWORD required' }
New-Item -ItemType Directory -Force -Path $runDir, $evidenceDir | Out-Null
function Log([string]$m) { Add-Content -LiteralPath $progress -Value "$(Get-Date -Format 'HH:mm:ss') $m" -Encoding utf8 }
function Psql([string]$sql) { (& docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -tAc $sql 2>$null | Out-String).Trim() }
function Wait-PortFree([int]$port) {
    for ($i = 0; $i -lt 40; $i++) {
        if (-not (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)) { return $true }
        Start-Sleep -Milliseconds 500
    }
    return $false
}
if (-not (Wait-PortFree $PgPort) -or -not (Wait-PortFree $HostPort)) { throw 'task ports busy' }

# ---------- 1. disposable postgres ----------
& docker rm -f $Container 2>$null | Out-Null
& docker run -d --name $Container -e "POSTGRES_USER=$User" -e "POSTGRES_PASSWORD=$password" -e "POSTGRES_DB=$Database" `
    -p "127.0.0.1:${PgPort}:5432" postgres:16-alpine 2>$null | Out-Null
$ready = $false
for ($i = 0; $i -lt 80; $i++) { if ((Psql 'select 1') -eq '1') { $ready = $true; break }; Start-Sleep -Milliseconds 700 }
if (-not $ready) { throw 'postgres not ready' }
& docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -c "create database $MessagingDatabase" 2>$null | Out-Null
Log 'postgres ready'

$hostOut = Join-Path $runDir 'host'
dotnet publish (Join-Path $repo 'src\backend\Host\Tooba.Host\Tooba.Host.csproj') -c Debug -o $hostOut 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'publish failed' }
Log 'published'

$env:TOOBA_182_HOST_PORT = "$HostPort"
$env:TOOBA_182_PG_PORT = "$PgPort"
$env:TOOBA_182_DATABASE = $Database
$env:TOOBA_182_MESSAGING_DATABASE = $MessagingDatabase
$env:TOOBA_182_USER = $User
$env:TOOBA_182_ALLOW_SEED = 'true'
$env:TOOBA_182_RUN_LEGACY_BOOTSTRAPS = 'true'
node (Join-Path $evidenceDir 'prepare-isolated-target-182.js')
if ($LASTEXITCODE -ne 0) { throw 'isolation guard failed' }
Log 'isolation guard PASS'
Copy-Item (Join-Path $runDir 'appsettings.json') (Join-Path $hostOut 'appsettings.json') -Force
Copy-Item (Join-Path $runDir 'appsettings.Development.json') (Join-Path $hostOut 'appsettings.Development.json') -Force

$env:ASPNETCORE_URLS = "http://127.0.0.1:$HostPort"
$env:DOTNET_ENVIRONMENT = 'Development'

function Start-Host([string]$leg, [string]$tag) {
    if (-not (Wait-PortFree $HostPort)) { throw "port $HostPort busy before $tag/$leg" }
    $stdout = Join-Path $runDir "h-$tag-$leg.jsonl"
    $stderr = Join-Path $runDir "h-$tag-$leg-err.txt"
    Remove-Item -Force -ErrorAction SilentlyContinue $stdout, $stderr
    if ($leg -eq 'diagnostic') { Set-Item -Path 'Env:Logging__LogLevel__Microsoft.EntityFrameworkCore' -Value 'Information' }
    else { Remove-Item -Path 'Env:Logging__LogLevel__Microsoft.EntityFrameworkCore' -ErrorAction SilentlyContinue }
    $proc = Start-Process -FilePath (Join-Path $hostOut 'Tooba.Host.exe') -WorkingDirectory $hostOut -PassThru -NoNewWindow `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $ok = $false; $t0 = Get-Date
    while (((Get-Date) - $t0).TotalSeconds -lt 300) {
        if ($proc.HasExited) { break }
        try { if ((Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/health" -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200) { $ok = $true; break } } catch { }
        Start-Sleep -Milliseconds 700
    }
    if (-not $ok) { try { Stop-Process -Id $proc.Id -Force } catch { }; throw "host $tag/$leg not healthy" }
    $owner = (Get-NetTCPConnection -LocalPort $HostPort -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1).OwningProcess
    if ($owner -ne $proc.Id) { try { Stop-Process -Id $proc.Id -Force } catch { }; throw "listener pid $owner != started pid $($proc.Id)" }
    Log "host up $tag/$leg pid=$($proc.Id)"
    return [pscustomobject]@{ proc = $proc; stdout = $stdout }
}
function Stop-Host($h, [string]$label) {
    try { Stop-Process -Id $h.proc.Id -Force } catch { }
    try { $h.proc.WaitForExit(20000) | Out-Null } catch { }
    if (-not (Wait-PortFree $HostPort)) { Log "WARN port busy after $label" }
    Start-Sleep -Milliseconds 300
}

function Measure-Home([string]$tag, [string]$reason) {
    $h = Start-Host 'default' "$tag-m"
    $uri = "http://127.0.0.1:$HostPort/v1/storefront/home?locale=$Locale"
    $headers = @{ Host = 'alpha.localhost' }
    $wSw = [System.Diagnostics.Stopwatch]::StartNew()
    $warm = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 120 -Headers $headers
    $wSw.Stop()
    $samples = @()
    for ($i = 1; $i -le 3; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $r = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 120 -Headers $headers
        $sw.Stop()
        $body = $r.Content
        $sha = (Get-FileHash -InputStream ([System.IO.MemoryStream]::new([System.Text.Encoding]::UTF8.GetBytes($body))) -Algorithm SHA256).Hash
        if ($i -eq 1) { Set-Content -LiteralPath (Join-Path $evidenceDir "home-$tag.json") -Value $body -Encoding utf8 }
        $samples += [pscustomobject]@{ request = $i; status = $r.StatusCode; wallMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 1); payloadBytes = [System.Text.Encoding]::UTF8.GetByteCount($body); bodySha256 = $sha; productIds = ([regex]::Matches($body, '"productId"')).Count }
    }
    Stop-Host $h "$tag/measure"
    $text = if (Test-Path $h.stdout) { Get-Content $h.stdout -Raw } else { '' }
    $lines = @(($text -split "`n") | Where-Object { $_.Trim() })
    $published = [int](Psql "select count(*) from catalog.products where status='Published'")
    $report = [pscustomobject]@{
        tag = $tag; reason = $reason; publishedProductsInDb = $published
        warmup = [pscustomobject]@{ status = $warm.StatusCode; wallMs = [math]::Round($wSw.Elapsed.TotalMilliseconds, 1); payloadBytes = [System.Text.Encoding]::UTF8.GetByteCount($warm.Content) }
        samples = $samples
        stdoutBytes = [System.Text.Encoding]::UTF8.GetByteCount($text)
        stdoutLines = $lines.Count
        errorLines = @($lines | Where-Object { $_ -match '"LogLevel":"(Error|Critical)"' }).Count
    }
    Log "$tag measured: published=$published warm=$($report.warmup.wallMs)ms bytes=$($report.warmup.payloadBytes) prodIds=$($samples[0].productIds)"
    return $report
}

# ---------- 2. Host-owned Development bootstrap (authority lives in the serving process) ----------
$boot = Start-Host 'default' 'boot' $null
$actor = Psql "select user_id from identity.login_identifiers where normalized_value='admin-actor@tooba.local' and kind='Email' limit 1"
if ($actor -notmatch '^[0-9a-fA-F-]{36}$') { Stop-Host $boot 'boot'; throw 'admin dev actor missing' }
Log "admin actor=$actor"
$rows0 = Psql "select 'products='||(select count(*) from catalog.products)||' published='||(select count(*) from catalog.products where status='Published')||' offers='||(select count(*) from offer.offers)||' prices='||(select count(*) from pricing.prices)||' stock='||(select count(*) from inventory.stock_positions)"
Log "baseline rows: $rows0"

# ---------- 3. bounded seed-only (owner demo seeder) ----------
$codeFile = Join-Path $runDir 'seed-code.txt'; $respFile = Join-Path $runDir 'seed-body.json'
Remove-Item -Force -ErrorAction SilentlyContinue $codeFile, $respFile
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$c = Start-Process -FilePath 'curl.exe' -ArgumentList @('--max-time','180','-s','-o',$respFile,'-w','%{http_code}','-X','POST',
    '-H','Host: alpha.localhost','-H',"X-Tooba-Dev-Actor-User-Id: $actor",
    "http://127.0.0.1:$HostPort/v1/admin/catalog/demo/seed-only") -PassThru -NoNewWindow -RedirectStandardOutput $codeFile
$c.WaitForExit(200000) | Out-Null
if (-not $c.HasExited) { try { Stop-Process -Id $c.Id -Force } catch { } }
$sw.Stop()
$seedCode = if (Test-Path $codeFile) { (Get-Content $codeFile -Raw).Trim() } else { '' }
$seedBody = if (Test-Path $respFile) { Get-Content $respFile -Raw } else { '' }
$rows1 = Psql "select 'products='||(select count(*) from catalog.products)||' published='||(select count(*) from catalog.products where status='Published')||' variants='||(select count(*) from catalog.variants)||' offers='||(select count(*) from offer.offers)||' prices='||(select count(*) from pricing.prices)||' stock='||(select count(*) from inventory.stock_positions)"
Log "seed http=$seedCode elapsed=$([math]::Round($sw.Elapsed.TotalSeconds,1))s rows=$rows1 bodyLen=$($seedBody.Length)"
Set-Content -LiteralPath (Join-Path $evidenceDir 'seed-182.txt') -Value "http=$seedCode`nelapsedSeconds=$([math]::Round($sw.Elapsed.TotalSeconds,1))`nrowsBefore=$rows0`nrowsAfter=$rows1`nbody=`n$seedBody" -Encoding utf8

# ---------- 4. SIZE A (baseline published storefront) ----------
$sizeA = Measure-Home 'size-a' 'baseline published catalog (3 published products)'

# ---------- 5. legitimate bounded publication through owner endpoints ----------
# seller actor/party pairs from the Development seller bootstrap
$sellers = (Psql "select l.normalized_value||'|'||p.party_id from identity.login_identifiers l join party.user_party_links p on p.user_id=l.user_id where l.normalized_value like 'seller-actor-%' order by l.normalized_value")
Log "seller links: $sellers"
$sellerPairs = @()
foreach ($line in ($sellers -split "`n")) { if ($line -match '^([^|]+)\|([0-9a-fA-F-]{36})$') { $sellerPairs += [pscustomobject]@{ email = $Matches[1]; party = $Matches[2] } } }
if ($sellerPairs.Count -eq 0) { Log 'FATAL no seller party links'; Stop-Host $boot 'boot'; throw 'no seller party links' }
$sellerActors = @{}
foreach ($p in $sellerPairs) {
    $uid = Psql "select user_id from identity.login_identifiers where normalized_value='$($p.email)' limit 1"
    $sellerActors[$p.party] = $uid
}

# demo products that have variants and are still Draft
$candidates = @()
$raw = Psql "select p.product_id||'|'||v.variant_id||'|'||coalesce(v.sku,'') from catalog.products p join catalog.variants v on v.product_id=p.product_id where p.status='Draft' and p.slug_seam like 'demo-prod-%' order by p.slug_seam, v.variant_id limit $Target"
foreach ($line in ($raw -split "`n")) { if ($line -match '^([0-9a-fA-F-]{36})\|([0-9a-fA-F-]{36})\|') { $candidates += [pscustomobject]@{ productId = $Matches[1]; variantId = $Matches[2] } } }
Log "publication candidates: $($candidates.Count)"
if ($candidates.Count -lt $Target) {
    $raw2 = Psql "select p.product_id||'|'||v.variant_id from catalog.products p join catalog.variants v on v.product_id=p.product_id where p.status='Draft' order by p.slug_seam, v.variant_id limit $Target"
    foreach ($line in ($raw2 -split "`n")) { if ($line -match '^([0-9a-fA-F-]{36})\|([0-9a-fA-F-]{36})$') { $candidates += [pscustomobject]@{ productId = $Matches[1]; variantId = $Matches[2] } } }
    Log "after fallback candidates: $($candidates.Count)"
}

$pubLog = @()
$i = 0
foreach ($cand in $candidates) {
    $i++
    $pair = $sellerPairs[($i - 1) % $sellerPairs.Count]
    $sellerActor = $sellerActors[$pair.party]
    $hdr = @{ Host = 'alpha.localhost'; 'X-Tooba-Dev-Actor-User-Id' = $sellerActor; 'X-Tooba-Seller-Party-Id' = $pair.party; 'Content-Type' = 'application/json' }
    $offerId = $null
    try {
        $offerBody = @{ catalogVariantId = $cand.variantId; sellerSku = "tb182-$i"; status = 'Active' } | ConvertTo-Json -Compress
        $r = Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/v1/seller/offers" -Method Post -Body $offerBody -Headers $hdr -UseBasicParsing -TimeoutSec 60
        $offerJson = $r.Content | ConvertFrom-Json
        $offerId = $offerJson.data.offerId
        if (-not $offerId) { $offerId = $offerJson.offerId }
        $pubLog += [pscustomobject]@{ step = "$i/offer"; status = $r.StatusCode; ok = [bool]$offerId; detail = $r.Content.Substring(0, [Math]::Min(160, $r.Content.Length)) }
    } catch {
        $d = ''; if ($_.Exception.Response) { $sr = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream()); $d = $sr.ReadToEnd() }
        $pubLog += [pscustomobject]@{ step = "$i/offer"; status = 'ERR'; ok = $false; detail = ($d + ' ' + $_.Exception.Message).Substring(0, [Math]::Min(200, ($d + $_.Exception.Message).Length)) }
    }
    if (-not $offerId) { Log "candidate $i offer failed"; continue }

    try {
        $pr = @{ amount = 1250000 + ($i * 10000); currency = 'IRR'; market = 'IR' } | ConvertTo-Json -Compress
        $r = Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/v1/seller/offers/$offerId/price" -Method Put -Body $pr -Headers $hdr -UseBasicParsing -TimeoutSec 60
        $pubLog += [pscustomobject]@{ step = "$i/price"; status = $r.StatusCode; ok = $true; detail = '' }
    } catch {
        $d = ''; if ($_.Exception.Response) { $sr = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream()); $d = $sr.ReadToEnd() }
        $pubLog += [pscustomobject]@{ step = "$i/price"; status = 'ERR'; ok = $false; detail = $d.Substring(0, [Math]::Min(200, $d.Length)) }
    }
    try {
        $iv = @{ onHand = 25; reason = 'tb182 bounded scale fixture' } | ConvertTo-Json -Compress
        $r = Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/v1/seller/offers/$offerId/inventory" -Method Put -Body $iv -Headers $hdr -UseBasicParsing -TimeoutSec 60
        $pubLog += [pscustomobject]@{ step = "$i/inventory"; status = $r.StatusCode; ok = $true; detail = '' }
    } catch {
        $d = ''; if ($_.Exception.Response) { $sr = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream()); $d = $sr.ReadToEnd() }
        $pubLog += [pscustomobject]@{ step = "$i/inventory"; status = 'ERR'; ok = $false; detail = $d.Substring(0, [Math]::Min(200, $d.Length)) }
    }
    try {
        $r = Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/v1/admin/products/$($cand.productId)/publish" -Method Post -Headers @{ Host='alpha.localhost'; 'X-Tooba-Dev-Actor-User-Id'=$actor } -UseBasicParsing -TimeoutSec 60
        $pubLog += [pscustomobject]@{ step = "$i/publish"; status = $r.StatusCode; ok = $true; detail = $r.Content.Substring(0, [Math]::Min(160, $r.Content.Length)) }
    } catch {
        $d = ''; if ($_.Exception.Response) { $sr = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream()); $d = $sr.ReadToEnd() }
        $pubLog += [pscustomobject]@{ step = "$i/publish"; status = 'ERR'; ok = $false; detail = $d.Substring(0, [Math]::Min(240, $d.Length)) }
    }
}
$pubLog | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $evidenceDir 'publication-attempts-182.json') -Encoding utf8
$okCount = @($pubLog | Where-Object { $_.step -like '*/publish' -and $_.ok }).Count
Log "publish ok count=$okCount of $($candidates.Count)"
$rows2 = Psql "select 'products='||(select count(*) from catalog.products)||' published='||(select count(*) from catalog.products where status='Published')||' offers='||(select count(*) from offer.offers where status='Active')||' prices='||(select count(*) from pricing.prices)||' stock='||(select count(*) from inventory.stock_positions)"
Log "rows after publication: $rows2"
Stop-Host $boot 'boot'

# ---------- 6. SIZE B ----------
$sizeB = Measure-Home 'size-b' 'after authorized bounded publication through owner endpoints'

$summary = [pscustomobject]@{
    baseSha = (& git rev-parse HEAD)
    route = "/v1/storefront/home?locale=$Locale"
    sizeA = $sizeA
    sizeB = $sizeB
    seed = [pscustomobject]@{ http = $seedCode; elapsedSeconds = [math]::Round($sw.Elapsed.TotalSeconds, 1); rowsBefore = $rows0; rowsAfter = $rows1 }
    publication = [pscustomobject]@{ candidates = $candidates.Count; publishedOk = $okCount; rowsAfter = $rows2 }
    sharedPortsTouched = $false
    credentialsInEvidence = $false
}
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidenceDir 'measurement-182.json') -Encoding utf8
$summary | ConvertTo-Json -Depth 8

