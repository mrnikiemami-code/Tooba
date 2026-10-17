# TB-183 stage 2 — same-process size A -> authorized owner publication -> size B, all with trace-scoped EF=Information.
$ErrorActionPreference = 'Continue'
$repo = (Get-Location).Path
$Container = 'tooba-tb183-pg'; $Database = 'tooba_183_scratch'; $User = 'scratch183'; $HostPort = 5454; $Locale = 'fa'
$runDir = Join-Path $repo '.tmp-183-run'
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-STOREFRONT-PERF-QUERY-SCALE-183'
$progress = Join-Path $evidenceDir 'progress-183.log'
$password = (Get-Content (Join-Path $repo '.tmp-183-pw.txt') -Raw).Trim()
$admin = (Get-Content (Join-Path $runDir 'admin-actor.txt') -Raw).Trim()
function Log([string]$m) { Add-Content -LiteralPath $progress -Value "$(Get-Date -Format 'HH:mm:ss') $m" -Encoding utf8 }
function Psql([string]$sql) { (& docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -tAc $sql 2>$null | Out-String).Trim() }
$uri = "http://127.0.0.1:$HostPort/v1/storefront/home?locale=$Locale"
$requests = @()

function Home-Request([string]$tag, [string]$phase) {
    $tid = -join ((1..32) | ForEach-Object { '0123456789abcdef'[(Get-Random -Maximum 16)] })
    $sid = -join ((1..16) | ForEach-Object { '0123456789abcdef'[(Get-Random -Maximum 16)] })
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $r = Invoke-WebRequest -Uri $uri -UseBasicParsing -Headers @{ Host = 'alpha.localhost'; traceparent = "00-$tid-$sid-01" } -TimeoutSec 120
    $sw.Stop()
    $bytes = [System.Text.Encoding]::UTF8.GetByteCount($r.Content)
    $sha = (Get-FileHash -InputStream ([System.IO.MemoryStream]::new([System.Text.Encoding]::UTF8.GetBytes($r.Content))) -Algorithm SHA256).Hash
    if ($phase -eq 'warmup') { Set-Content -LiteralPath (Join-Path $runDir "home-$tag-warmup.json") -Value $r.Content -Encoding utf8 }
    if ($phase -eq 'm1') { Set-Content -LiteralPath (Join-Path $evidenceDir "home-$tag.json") -Value $r.Content -Encoding utf8 }
    $script:requests += [pscustomobject]@{ tag=$tag; phase=$phase; traceId=$tid; status=$r.StatusCode; wallMs=[math]::Round($sw.Elapsed.TotalMilliseconds,1); payloadBytes=$bytes; bodySha256=$sha }
    Log ("${tag}/${phase}: status=" + $r.StatusCode + " ms=" + [math]::Round($sw.Elapsed.TotalMilliseconds,1) + " bytes=" + $bytes + " sha=" + $sha.Substring(0,12) + " tid=" + $tid)
}

function Measure-Size([string]$tag) {
    Home-Request $tag 'warmup'
    Home-Request $tag 'm1'
    Home-Request $tag 'm2'
    Home-Request $tag 'm3'
}

$publishedBefore = [int](Psql "select count(*) from catalog.products where status='Published'")
Log "size A start: published=$publishedBefore products=$([int](Psql 'select count(*) from catalog.products')) categories=$([int](Psql 'select count(*) from catalog.categories')) variants=$([int](Psql 'select count(*) from catalog.variants'))"
Measure-Size 'size-a'

# ---------- bounded owner-authorized publication ----------
$pairs = @()
foreach ($line in ((Psql "select user_id||'|'||party_id from party.memberships where status='Active' and relation_code='member'") -split "\r?\n")) {
    if ($line -match '^([0-9a-fA-F-]{36})\|([0-9a-fA-F-]{36})$') { $pairs += [pscustomobject]@{ actor=$Matches[1]; party=$Matches[2] } }
}
Log ("seller memberships: " + $pairs.Count)
if ($pairs.Count -eq 0) { throw 'no active seller memberships' }

$candidates = @()
foreach ($line in ((Psql "select p.product_id||'|'||v.variant_id from catalog.products p join catalog.variants v on v.product_id=p.product_id where p.status='Draft' order by p.slug_seam, v.variant_id limit 40") -split "\r?\n")) {
    if ($line -match '^([0-9a-fA-F-]{36})\|([0-9a-fA-F-]{36})$') { $candidates += [pscustomobject]@{ productId=$Matches[1]; variantId=$Matches[2] } }
}
Log ("candidates: " + $candidates.Count)

$attempts = @(); $i = 0; $publishedNow = $publishedBefore
foreach ($cand in $candidates) {
    if ($publishedNow -ge 13) { break }
    $i++
    $pair = $pairs[($i - 1) % $pairs.Count]
    $sh = @{ Host='alpha.localhost'; 'X-Tooba-Dev-Actor-User-Id'=$pair.actor; 'X-Tooba-Seller-Party-Id'=$pair.party; 'Content-Type'='application/json' }
    $ah = @{ Host='alpha.localhost'; 'X-Tooba-Dev-Actor-User-Id'=$admin; 'Content-Type'='application/json' }
    $offerId = $null
    try {
        $r = Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/v1/seller/offers" -Method Post -Headers $sh -Body (@{ catalogVariantId=$cand.variantId; sellerSku="tb183-$i"; status='Active' } | ConvertTo-Json -Compress) -UseBasicParsing -TimeoutSec 60
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
        @{ step='inventory'; uri="http://127.0.0.1:$HostPort/v1/seller/offers/$offerId/inventory"; body=(@{ onHand=25; reason='tb183 request-scoped query evidence fixture' } | ConvertTo-Json -Compress) })) {
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
    $publishedNow = [int](Psql "select count(*) from catalog.products where status='Published'")
}
$attempts | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $evidenceDir 'publication-attempts-183.json') -Encoding utf8
Log ("publication: attempted=" + $i + " publishedNow=" + $publishedNow)
$rowsB = Psql "select 'products='||(select count(*) from catalog.products)||' published='||(select count(*) from catalog.products where status='Published')||' active_offers='||(select count(*) from offer.offers where status='Active')||' active_prices='||(select count(*) from pricing.prices where status='Active')||' stock='||(select count(*) from inventory.stock_positions)||' variants='||(select count(*) from catalog.variants)||' categories='||(select count(*) from catalog.categories)"
Log ("size B rows: " + $rowsB)

Start-Sleep -Seconds 3
Measure-Size 'size-b'

$requests | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $runDir 'requests-183.json') -Encoding utf8
$a = Get-Content (Join-Path $evidenceDir 'home-size-a.json') -Raw | ConvertFrom-Json
$b = Get-Content (Join-Path $evidenceDir 'home-size-b.json') -Raw | ConvertFrom-Json
function Ids($o) { @($o.featuredProducts + $o.specialOffers + $o.campaignProducts + $o.newArrivals + $o.productRail + $o.mostViewedProducts) | Where-Object { $_.productId } | Select-Object -ExpandProperty productId -Unique }
$idsA = @(Ids $a); $idsB = @(Ids $b)
"idsA=$($idsA.Count) idsB=$($idsB.Count) ratio=$([math]::Round($idsB.Count / [math]::Max(1,$idsA.Count),2))"
$idsA -join ',' | Set-Content (Join-Path $runDir 'ids-a.txt')
$idsB -join ',' | Set-Content (Join-Path $runDir 'ids-b.txt')
"publishedA=$publishedBefore publishedB=$publishedNow"
