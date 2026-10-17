# TB-182 phase 3 — bounded publication using the genuine Development seller memberships, then size B.
$ErrorActionPreference = 'Continue'
$repo = (Get-Location).Path
$Container = 'tooba-tb182-pg'; $Database = 'tooba_182_scratch'; $User = 'scratch182'; $HostPort = 5453; $Locale = 'fa'; $Target = 14
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-STOREFRONT-PERF-PUBLISHED-SCALE-182'
$progress = Join-Path $evidenceDir 'progress-182.log'
$password = (Get-Content (Join-Path $repo '.tmp-182-pw.txt') -Raw).Trim()
$admin = '01a14a24-c838-7000-a3c9-d9da9e25d460'
function Log([string]$m) { Add-Content -LiteralPath $progress -Value "$(Get-Date -Format 'HH:mm:ss') $m" -Encoding utf8 }
function Psql([string]$sql) { (& docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -tAc $sql 2>$null | Out-String).Trim() }

$pairs = @()
foreach ($line in ((Psql "select user_id||'|'||party_id from party.memberships where status='Active' and relation_code='member'") -split "`n")) {
    if ($line -match '^([0-9a-fA-F-]{36})\|([0-9a-fA-F-]{36})$') { $pairs += [pscustomobject]@{ actor=$Matches[1]; party=$Matches[2] } }
}
Log ("seller memberships: " + $pairs.Count)
if ($pairs.Count -eq 0) { throw 'no active seller memberships' }

$candidates = @()
foreach ($line in ((Psql "select p.product_id||'|'||v.variant_id from catalog.products p join catalog.variants v on v.product_id=p.product_id where p.status='Draft' and p.slug_seam like 'demo-prod-%' order by p.slug_seam, v.variant_id limit $Target") -split "`n")) {
    if ($line -match '^([0-9a-fA-F-]{36})\|([0-9a-fA-F-]{36})$') { $candidates += [pscustomobject]@{ productId=$Matches[1]; variantId=$Matches[2] } }
}
Log ("candidates: " + $candidates.Count)

$attempts = @(); $i = 0
foreach ($cand in $candidates) {
    $i++
    $pair = $pairs[($i - 1) % $pairs.Count]
    $sh = @{ Host='alpha.localhost'; 'X-Tooba-Dev-Actor-User-Id'=$pair.actor; 'X-Tooba-Seller-Party-Id'=$pair.party; 'Content-Type'='application/json' }
    $ah = @{ Host='alpha.localhost'; 'X-Tooba-Dev-Actor-User-Id'=$admin; 'Content-Type'='application/json' }
    $offerId = $null
    try {
        $r = Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/v1/seller/offers" -Method Post -Headers $sh -Body (@{ catalogVariantId=$cand.variantId; sellerSku="tb182-$i"; status='Active' } | ConvertTo-Json -Compress) -UseBasicParsing -TimeoutSec 60
        $j = $r.Content | ConvertFrom-Json
        $offerId = $j.data.offerId; if (-not $offerId) { $offerId = $j.offerId }
        $attempts += [pscustomobject]@{ step='offer'; index=$i; status=$r.StatusCode; offerId=$offerId; detail='' }
    } catch {
        $d=''; try { if ($_.Exception.Response) { $sr = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream()); $d = $sr.ReadToEnd() } } catch {}
        $attempts += [pscustomobject]@{ step='offer'; index=$i; status='ERR'; offerId=$null; detail=$d }
    }
    if (-not $offerId) { continue }
    foreach ($w in @(
        @{ step='price'; uri="http://127.0.0.1:$HostPort/v1/seller/offers/$offerId/price"; body=(@{ amount=1250000+($i*10000); currency='IRR'; market='IR' } | ConvertTo-Json -Compress) },
        @{ step='inventory'; uri="http://127.0.0.1:$HostPort/v1/seller/offers/$offerId/inventory"; body=(@{ onHand=25; reason='tb182 bounded scale fixture' } | ConvertTo-Json -Compress) })) {
        try {
            $r2 = Invoke-WebRequest -Uri $w.uri -Method Put -Headers $sh -Body $w.body -UseBasicParsing -TimeoutSec 60
            $attempts += [pscustomobject]@{ step=$w.step; index=$i; status=$r2.StatusCode; offerId=$offerId; detail='' }
        } catch {
            $d=''; try { if ($_.Exception.Response) { $sr = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream()); $d = $sr.ReadToEnd() } } catch {}
            $attempts += [pscustomobject]@{ step=$w.step; index=$i; status='ERR'; offerId=$offerId; detail=$d }
        }
    }
    try {
        $r3 = Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/v1/admin/products/$($cand.productId)/publish" -Method Post -Headers $ah -UseBasicParsing -TimeoutSec 60
        $attempts += [pscustomobject]@{ step='publish'; index=$i; status=$r3.StatusCode; offerId=$offerId; detail=$r3.Content }
    } catch {
        $d=''; try { if ($_.Exception.Response) { $sr = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream()); $d = $sr.ReadToEnd() } } catch {}
        $attempts += [pscustomobject]@{ step='publish'; index=$i; status='ERR'; offerId=$offerId; detail=$d }
    }
}
$attempts | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $evidenceDir 'publication-attempts-182.json') -Encoding utf8
$okPublish = @($attempts | Where-Object { $_.step -eq 'publish' -and $_.status -eq 200 }).Count
$offersCreated = @($attempts | Where-Object { $_.step -eq 'offer' -and $_.offerId }).Count
Log ("publication: publish200=" + $okPublish + "/" + $candidates.Count + " offersCreated=" + $offersCreated)
$rowsAfter = Psql "select 'products='||(select count(*) from catalog.products)||' published='||(select count(*) from catalog.products where status='Published')||' active_offers='||(select count(*) from offer.offers where status='Active')||' active_prices='||(select count(*) from pricing.prices where status='Active')||' stock='||(select count(*) from inventory.stock_positions)"
Log ("rows after publication: " + $rowsAfter)

$uri = "http://127.0.0.1:$HostPort/v1/storefront/home?locale=$Locale"; $hdr = @{ Host='alpha.localhost' }
$warm = Invoke-WebRequest -Uri $uri -UseBasicParsing -Headers $hdr -TimeoutSec 120
$samples = @()
for ($k=1; $k -le 3; $k++) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew(); $r = Invoke-WebRequest -Uri $uri -UseBasicParsing -Headers $hdr -TimeoutSec 120; $sw.Stop()
    if ($k -eq 1) { Set-Content -LiteralPath (Join-Path $evidenceDir 'home-size-b.json') -Value $r.Content -Encoding utf8 }
    $samples += [pscustomobject]@{ request=$k; status=$r.StatusCode; wallMs=[math]::Round($sw.Elapsed.TotalMilliseconds,1); payloadBytes=[System.Text.Encoding]::UTF8.GetByteCount($r.Content); bodySha256=(Get-FileHash -InputStream ([System.IO.MemoryStream]::new([System.Text.Encoding]::UTF8.GetBytes($r.Content))) -Algorithm SHA256).Hash }
}
$b = Get-Content (Join-Path $evidenceDir 'home-size-b.json') -Raw | ConvertFrom-Json
$cards = @($b.featuredProducts + $b.specialOffers + $b.campaignProducts + $b.newArrivals + $b.productRail + $b.mostViewedProducts) | Where-Object { $_.productId }
$distinct = @($cards | Select-Object -ExpandProperty productId -Unique)
$integrity = $cards | Group-Object productId | ForEach-Object { $_.Group[0] } | Select-Object -First 40 | ForEach-Object {
    [pscustomobject]@{ productId=$_.productId; title=$_.title; seller=$_.sellerName; amount=$_.offerAmountExclusiveOfTax; currency=$_.currency; available=$_.availableUnits; slug=$_.slug; promoted=$_.promotionLabel }
}
$integrity | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $evidenceDir 'payload-integrity-182.json') -Encoding utf8
$sizeA = Get-Content (Join-Path $evidenceDir 'home-size-a.json') -Raw | ConvertFrom-Json
$cardsA = @($sizeA.featuredProducts + $sizeA.specialOffers + $sizeA.campaignProducts + $sizeA.newArrivals + $sizeA.productRail + $sizeA.mostViewedProducts) | Where-Object { $_.productId }
$distinctA = @($cardsA | Select-Object -ExpandProperty productId -Unique)
Log ("sizeB: distinct=" + $distinct.Count + " bytes=" + $samples[0].payloadBytes + " sha=" + $samples[0].bodySha256.Substring(0,12) + " wall=" + $samples[0].wallMs)
Log ("sizeA: distinct=" + $distinctA.Count + " bytes=" + [System.Text.Encoding]::UTF8.GetByteCount((Get-Content (Join-Path $evidenceDir 'home-size-a.json') -Raw)))

$summary = [pscustomobject]@{
    baseSha = (& git rev-parse HEAD); route=$uri
    sizeA = [pscustomobject]@{ publishedInDb=3; distinctPayloadProductIds=$distinctA; count=$distinctA.Count; payloadBytes=[System.Text.Encoding]::UTF8.GetByteCount((Get-Content (Join-Path $evidenceDir 'home-size-a.json') -Raw)) }
    sizeB = [pscustomobject]@{ publishedInDb=[int](Psql "select count(*) from catalog.products where status='Published'"); distinctPayloadProductIds=$distinct; count=$distinct.Count; warmupPayloadBytes=[System.Text.Encoding]::UTF8.GetByteCount($warm.Content); samples=$samples }
    publication = [pscustomobject]@{ candidates=$candidates.Count; offersCreated=$offersCreated; publish200=$okPublish; rowsAfterPublication=$rowsAfter; sellerMemberships=$pairs.Count; attempts=$attempts }
    integrity = $integrity
    sizeRatio = [math]::Round($distinct.Count / [math]::Max(1,$distinctA.Count), 2)
}
$summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $evidenceDir 'measurement-182.json') -Encoding utf8
"sizA_distinct=$($distinctA.Count) sizB_distinct=$($distinct.Count) ratio=$($summary.sizeRatio)"
"sizB_bytes=$($samples[0].payloadBytes) sha=$($samples[0].bodySha256)"
"publish200=$okPublish/$($candidates.Count) offers=$offersCreated"
