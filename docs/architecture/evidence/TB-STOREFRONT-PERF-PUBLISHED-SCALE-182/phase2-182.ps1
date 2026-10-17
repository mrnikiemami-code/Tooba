# TB-182 phase 2 — runs against the already-bootstrapped scratch Host (Host-owned Development authority),
# measures size A, performs bounded authorized publication through owner routes, then measures size B.
$ErrorActionPreference = 'Continue'
$repo = (Get-Location).Path
$Container = 'tooba-tb182-pg'; $Database = 'tooba_182_scratch'; $User = 'scratch182'; $HostPort = 5453
$Locale = 'fa'; $Target = 14
$runDir = Join-Path $repo '.tmp-182-run'
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-STOREFRONT-PERF-PUBLISHED-SCALE-182'
$progress = Join-Path $evidenceDir 'progress-182.log'
$password = (Get-Content (Join-Path $repo '.tmp-182-pw.txt') -Raw).Trim()
$actor = '01a14a24-c838-7000-a3c9-d9da9e25d460'
$hostExe = Join-Path $runDir 'host\Tooba.Host.exe'
function Log([string]$m) { Add-Content -LiteralPath $progress -Value "$(Get-Date -Format 'HH:mm:ss') $m" -Encoding utf8; }
function Psql([string]$sql) { (& docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -tAc $sql 2>$null | Out-String).Trim() }
function Wait-PortFree([int]$port) { for ($i=0; $i -lt 60; $i++) { if (-not (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)) { return $true }; Start-Sleep -Milliseconds 400 }; return $false }
function Get-Owner([int]$port) { (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1).OwningProcess }

# ---------- wait for the bounded seed to settle (no new seed loop) ----------
$t0 = Get-Date
do {
    $n = [int](Psql "select count(*) from catalog.products")
    if ($n -ge 145) { break }
    Start-Sleep -Seconds 4
} while (((Get-Date) - $t0).TotalSeconds -lt 90)
$rowsSeeded = Psql "select 'products='||(select count(*) from catalog.products)||' draft='||(select count(*) from catalog.products where status='Draft')||' published='||(select count(*) from catalog.products where status='Published')||' categories='||(select count(*) from catalog.categories)||' variants='||(select count(*) from catalog.variants)||' offers='||(select count(*) from offer.offers)||' active_offers='||(select count(*) from offer.offers where status='Active')||' prices='||(select count(*) from pricing.prices)||' stock='||(select count(*) from inventory.stock_positions)"
Log "seed settled: $rowsSeeded"

$uri = "http://127.0.0.1:$HostPort/v1/storefront/home?locale=$Locale"
$hdr = @{ Host = 'alpha.localhost' }
function Take-Home([string]$tag, [string]$reason) {
    $warm = Invoke-WebRequest -Uri $uri -UseBasicParsing -Headers $hdr -TimeoutSec 120
    $samples = @()
    for ($i=1; $i -le 3; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew(); $r = Invoke-WebRequest -Uri $uri -UseBasicParsing -Headers $hdr -TimeoutSec 120; $sw.Stop()
        $bytes = [System.Text.Encoding]::UTF8.GetByteCount($r.Content)
        $sha = (Get-FileHash -InputStream ([System.IO.MemoryStream]::new([System.Text.Encoding]::UTF8.GetBytes($r.Content))) -Algorithm SHA256).Hash
        if ($i -eq 1) { Set-Content -LiteralPath (Join-Path $evidenceDir "home-$tag.json") -Value $r.Content -Encoding utf8 }
        $samples += [pscustomobject]@{ request=$i; status=$r.StatusCode; wallMs=[math]::Round($sw.Elapsed.TotalMilliseconds,1); payloadBytes=$bytes; bodySha256=$sha; productIdOccurrences=([regex]::Matches($r.Content,'"productId"')).Count }
    }
    $payload = Get-Content (Join-Path $evidenceDir "home-$tag.json") -Raw | ConvertFrom-Json
    $ids = @($payload.data.featuredProducts + $payload.data.newestProducts + $payload.data.promotedProducts + $payload.data.specialOffers) | Where-Object { $_.productId } | Select-Object -ExpandProperty productId -Unique
    $rep = [pscustomobject]@{ tag=$tag; reason=$reason; warmupPayloadBytes=[System.Text.Encoding]::UTF8.GetByteCount($warm.Content); samples=$samples; distinctProductIds=@($ids); distinctProductCount=@($ids).Count; publishedProductsInDb=[int](Psql "select count(*) from catalog.products where status='Published'") }
    Log "${tag}: publishedDb=$($rep.publishedProductsInDb) distinctPayloadIds=$($rep.distinctProductCount) bytes=$($samples[0].payloadBytes) sha=$($samples[0].bodySha256.Substring(0,12)) wall=$($samples[0].wallMs)ms"
    return $rep
}

# ---------- SIZE A (before any publication) ----------
$sizeA = Take-Home 'size-a' 'baseline: only pre-existing Published products visible'

# ---------- bounded authorized publication through owner CQRS routes ----------
$sellers = (Psql "select l.normalized_value||'|'||p.party_id||'|'||l.user_id from identity.login_identifiers l join party.user_party_links p on p.user_id=l.user_id where l.normalized_value like 'seller-actor-%' order by l.normalized_value")
$sellerPairs = @()
foreach ($line in ($sellers -split "`n")) { if ($line -match '^([^|]+)\|([0-9a-fA-F-]{36})\|([0-9a-fA-F-]{36})$') { $sellerPairs += [pscustomobject]@{ email=$Matches[1]; party=$Matches[2]; actor=$Matches[3] } } }
Log ("seller parties: " + (($sellerPairs | ForEach-Object { $_.email }) -join ','))
if ($sellerPairs.Count -eq 0) { throw 'no seller party links in scratch DB' }

$candidates = @()
$raw = Psql "select p.product_id||'|'||v.variant_id from catalog.products p join catalog.variants v on v.product_id=p.product_id where p.status='Draft' and p.slug_seam like 'demo-prod-%' order by p.slug_seam, v.variant_id limit $Target"
foreach ($line in ($raw -split "`n")) { if ($line -match '^([0-9a-fA-F-]{36})\|([0-9a-fA-F-]{36})$') { $candidates += [pscustomobject]@{ productId=$Matches[1]; variantId=$Matches[2] } } }
if ($candidates.Count -lt $Target) { throw "only $($candidates.Count) demo draft candidates" }
Log "candidates=$($candidates.Count)"

$attempts = @()
$i = 0
foreach ($cand in $candidates) {
    $i++
    $pair = $sellerPairs[($i - 1) % $sellerPairs.Count]
    $sh = @{ Host='alpha.localhost'; 'X-Tooba-Dev-Actor-User-Id'=$pair.actor; 'X-Tooba-Seller-Party-Id'=$pair.party; 'Content-Type'='application/json' }
    $ah = @{ Host='alpha.localhost'; 'X-Tooba-Dev-Actor-User-Id'=$actor; 'Content-Type'='application/json' }
    $offerId = $null
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $r = Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/v1/seller/offers" -Method Post -Headers $sh -Body (@{ catalogVariantId=$cand.variantId; sellerSku="tb182-$i"; status='Active' } | ConvertTo-Json -Compress) -UseBasicParsing -TimeoutSec 60
        $j = $r.Content | ConvertFrom-Json
        $offerId = $j.data.offerId; if (-not $offerId) { $offerId = $j.offerId }
        $attempts += [pscustomobject]@{ step='offer'; index=$i; status=$r.StatusCode; offerId=$offerId; ms=[math]::Round($sw.Elapsed.TotalMilliseconds,1); detail='' }
    } catch {
        $d=''; try { if ($_.Exception.Response) { $sr = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream()); $d = $sr.ReadToEnd() } } catch {}
        $attempts += [pscustomobject]@{ step='offer'; index=$i; status='ERR'; offerId=$null; ms=[math]::Round($sw.Elapsed.TotalMilliseconds,1); detail=$d }
    }
    if (-not $offerId) { continue }
    foreach ($w in @(
        @{ step='price'; uri="http://127.0.0.1:$HostPort/v1/seller/offers/$offerId/price"; body=(@{ amount=1250000+($i*10000); currency='IRR'; market='IR' } | ConvertTo-Json -Compress) },
        @{ step='inventory'; uri="http://127.0.0.1:$HostPort/v1/seller/offers/$offerId/inventory"; body=(@{ onHand=25; reason='tb182 bounded scale fixture' } | ConvertTo-Json -Compress) })) {
        $sw2 = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            $r2 = Invoke-WebRequest -Uri $w.uri -Method Put -Headers $sh -Body $w.body -UseBasicParsing -TimeoutSec 60
            $attempts += [pscustomobject]@{ step=$w.step; index=$i; status=$r2.StatusCode; offerId=$offerId; ms=[math]::Round($sw2.Elapsed.TotalMilliseconds,1); detail='' }
        } catch {
            $d=''; try { if ($_.Exception.Response) { $sr = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream()); $d = $sr.ReadToEnd() } } catch {}
            $attempts += [pscustomobject]@{ step=$w.step; index=$i; status='ERR'; offerId=$offerId; ms=[math]::Round($sw2.Elapsed.TotalMilliseconds,1); detail=$d }
        }
    }
    $sw3 = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $r3 = Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/v1/admin/products/$($cand.productId)/publish" -Method Post -Headers $ah -UseBasicParsing -TimeoutSec 60
        $attempts += [pscustomobject]@{ step='publish'; index=$i; status=$r3.StatusCode; offerId=$offerId; ms=[math]::Round($sw3.Elapsed.TotalMilliseconds,1); detail=$r3.Content }
    } catch {
        $d=''; try { if ($_.Exception.Response) { $sr = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream()); $d = $sr.ReadToEnd() } } catch {}
        $attempts += [pscustomobject]@{ step='publish'; index=$i; status='ERR'; offerId=$offerId; ms=[math]::Round($sw3.Elapsed.TotalMilliseconds,1); detail=$d }
    }
}
$attempts | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $evidenceDir 'publication-attempts-182.json') -Encoding utf8
$okPublish = @($attempts | Where-Object { $_.step -eq 'publish' -and $_.status -eq 200 }).Count
Log "publication: publish200=$okPublish / attempts=$($attempts.Count)"
$rowsAfter = Psql "select 'products='||(select count(*) from catalog.products)||' published='||(select count(*) from catalog.products where status='Published')||' active_offers='||(select count(*) from offer.offers where status='Active')||' active_prices='||(select count(*) from pricing.prices where status='Active')||' stock='||(select count(*) from inventory.stock_positions)"
Log "rows after publication: $rowsAfter"

$sizeB = Take-Home 'size-b' 'after authorized bounded publication via seller Offer/Pricing/Inventory + ProductWorkspace publish'

# ---------- payload integrity ----------
$attempts | ConvertTo-Json -Depth 4 | Out-Null
$integrity = $null
try {
    $b = Get-Content (Join-Path $evidenceDir 'home-size-b.json') -Raw | ConvertFrom-Json
    $cards = @($b.data.featuredProducts + $b.data.newestProducts + $b.data.promotedProducts + $b.data.specialOffers) | Where-Object { $_.productId } | Group-Object productId | ForEach-Object { $_.Group[0] }
    $integrity = $cards | Select-Object -First 40 | ForEach-Object {
        [pscustomobject]@{ productId=$_.productId; title=$_.title; seller=$_.sellerName; amount=$_.offerAmountExclusiveOfTax; currency=$_.currency; available=$_.availableUnits; slug=$_.slug; hasMedia=([bool]($_.mediaUrl -or $_.imageUrl)); variantId=$_.variantId }
    }
    $integrity | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $evidenceDir 'payload-integrity-182.json') -Encoding utf8
} catch { Log "integrity capture failed: $($_.Exception.Message)" }

$summary = [pscustomobject]@{
    baseSha = (& git rev-parse HEAD); route = $uri; target = $Target
    seeded = $rowsSeeded; sizeA = $sizeA; sizeB = $sizeB
    publication = [pscustomobject]@{ candidates=$candidates.Count; publish200=$okPublish; rowsAfterPublication=$rowsAfter; attempts=$attempts }
    integritySample = $integrity
}
$summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $evidenceDir 'measurement-182.json') -Encoding utf8
"sizA_published=$($sizeA.publishedProductsInDb) sizA_distinct=$($sizeA.distinctProductCount)"
"sizB_published=$($sizeB.publishedProductsInDb) sizB_distinct=$($sizeB.distinctProductCount)"
"sizA_bytes=$($sizeA.samples[0].payloadBytes) sizB_bytes=$($sizeB.samples[0].payloadBytes)"
"publish200=$okPublish/$($candidates.Count)"
"sizeB_sha=$($sizeB.samples[0].bodySha256)"

