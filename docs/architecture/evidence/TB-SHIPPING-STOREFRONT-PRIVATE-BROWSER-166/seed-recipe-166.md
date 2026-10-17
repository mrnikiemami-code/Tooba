# TB-166 — self-contained scratch seed recipe (reusable)

Purpose: produce a disposable, authenticated, two-seller storefront fixture whose Shipping step can be
proven for real, without touching `:3000` / `:5088` / `:5432`, `tooba_alpha` or `tenant-alpha`.

All identifiers below are read from the scratch DB after the Host Development bootstrap; nothing business
critical is invented. Total wall time observed: ~4 minutes from `docker run` to an authenticated page.

## 0. Prerequisites

- Docker (Postgres 16 image available locally), `dotnet` SDK + `dotnet ef` 9.x, Node 20+.
- `HEAD == origin/main`; no shared database/session is read or written.

## 1. Disposable database (task-owned port/name)

```powershell
$pw = -join ((48..57)+(97..122) | Get-Random -Count 28 | ForEach-Object { [char]$_ })
$pw | Set-Content .tmp-166-scratch-password.txt -Encoding ascii -NoNewline   # never committed
docker run -d --name tooba-tb166-pg -e POSTGRES_USER=scratch166 -e POSTGRES_PASSWORD=$pw `
  -p 127.0.0.1:55486:5432 postgres:16-alpine
docker exec -e PGPASSWORD=$pw tooba-tb166-pg psql -U scratch166 -d postgres `
  -c "CREATE DATABASE tooba_166_scratch OWNER scratch166;" `
  -c "CREATE DATABASE tooba_166_messaging OWNER scratch166;"
```

## 2. Host publish + guarded overlay (gate ON for the main leg)

```powershell
dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Release -o .tmp-166-host-out
$env:TOOBA_166_SCRATCH_PASSWORD = $pw
$env:TOOBA_166_GATE = 'true'
node docs\architecture\evidence\TB-SHIPPING-STOREFRONT-PRIVATE-BROWSER-166\prepare-isolated-target-166.js
powershell -NoProfile -ExecutionPolicy Bypass -File docs\architecture\evidence\TB-SHIPPING-STOREFRONT-PRIVATE-BROWSER-166\launch-isolated-host-166.ps1 -Port 5421
```

The overlay is fail-closed (`isolation-guard-on-166.json`): one Active tenant `store-alpha -> tenant-166-scratch`,
`Edition=SingleStore`, no `tooba_alpha`/`tenant-alpha`, no `:5432`, no shadow gates, committed
`ShippingOwnedRateAuthority.Enabled=false` in all three committed appsettings, `AllowResetAndSeed=false`,
`RunLegacyBootstraps=true`, host port 5421, web port 3116.

## 3. Schema the Catalog bootstrap does not own

The Shipping and Geography modules intentionally register **no** runtime schema migrator, so their schemas
must be created with the canonical design-time context:

```powershell
$env:TOOBA_DESIGN_TIME_CONNECTION = "Host=127.0.0.1;Port=55486;Username=scratch166;Password=$pw;Database=tooba_166_scratch"
dotnet ef database update --project src\backend\Modules\Geography\Tooba.Geography.Infrastructure --startup-project src\backend\Modules\Geography\Tooba.Geography.Infrastructure
dotnet ef database update --project src\backend\Modules\Shipping\Tooba.Shipping.Infrastructure  --startup-project src\backend\Modules\Shipping\Tooba.Shipping.Infrastructure
```

Read back the seeded dependency chain (Development bootstrap already created it):

```sql
select offer_id, seller_party_id from offer.offers order by seller_party_id;
select offer_id, category_id from tax.offer_classifications;
select rule_id, category_id, rate, status from tax.rules;
```

Observed in this run:

| Role | Seller party | Offer | Category |
| --- | --- | --- | --- |
| seller A | `01a14803-3d1e-7000-a94b-84e8547bc0cc` | `01a14803-3d4a-7000-94d5-5db8871805b3` | `01a14803-3dc5-…` (no active rule) |
| seller B | `01a14803-3fb8-7000-b139-26472635ae4e` | `01a14803-3fbd-7000-8310-c9260fb93352` | `01a14803-3fd3-…` (active 9% rule) |

## 4. Shipping/Geography + tax closes (scratch only)

```powershell
Get-Content docs\architecture\evidence\TB-SHIPPING-STOREFRONT-PRIVATE-BROWSER-166\scratch-166-base-fixture.sql -Raw |
  docker exec -i -e PGPASSWORD=$pw tooba-tb166-pg psql -U scratch166 -d tooba_166_scratch -v ON_ERROR_STOP=1
Get-Content docs\architecture\evidence\TB-SHIPPING-STOREFRONT-PRIVATE-BROWSER-166\scratch-166-extra-fixture.sql -Raw |
  docker exec -i -e PGPASSWORD=$pw tooba-tb166-pg psql -U scratch166 -d tooba_166_scratch -v ON_ERROR_STOP=1
```

`scratch-166-base-fixture.sql` = country/administrative-area/city, the method-owned `tipax:standard 205000` and
`tipax:express 480000` policies at city scope, the provider-level decoy row that must never price a strict
option, and explicit `Unrestricted` coverage declarations.
`scratch-166-extra-fixture.sql` = the two missing active 9% rules for seller A's categories, plus the same two
owned amounts declared at administrative-area scope because the storefront address form sends province/city
**names** (name-based resolution stops at administrative area in this chain).

## 5. Private frontend pointed only at the private Host

```powershell
cd src\frontend
$env:TOOBA_HOST_ORIGIN = 'http://127.0.0.1:5421'
npx next dev -p 3116 -H 127.0.0.1
```

## 6. Genuine customer

- Development fixed OTP fixture (Development/Testing only): mobile `09111111111`, code `123456`.
- Register once (`POST /v1/auth/register`), then log in through the real `/fa/login` page.
- Build the cart with exactly one line per seller through the real cart endpoints **inside the authenticated
  page** (so the browser session and the cart belong to the same customer), then open `/fa/shipping`.
- Selecting province/city in the address form triggers the real `POST /v1/storefront/shipping/projection`
  with name-based destination.

## 7. OFF control

```powershell
Stop-Process -Id (Get-Content .tmp-166-run\host-true.pid)
$env:TOOBA_166_GATE = 'false'
node docs\architecture\evidence\TB-SHIPPING-STOREFRONT-PRIVATE-BROWSER-166\prepare-isolated-target-166.js
powershell -NoProfile -ExecutionPolicy Bypass -File docs\architecture\evidence\TB-SHIPPING-STOREFRONT-PRIVATE-BROWSER-166\launch-isolated-host-166.ps1 -Port 5421
```

Same cart, same session: the projection returns the legacy `ShippingMethods:Rates` amounts and the
`unavailableMethods` property is absent (`projection-off-166.json`).

## 8. Teardown

```powershell
Stop-Process -Id (Get-Content .tmp-166-run\host-true.pid) -Force          # and host-false.pid
Get-NetTCPConnection -LocalPort 3116 -State Listen | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }
docker stop tooba-tb166-pg; docker rm -f tooba-tb166-pg
Remove-Item .tmp-166-run,.tmp-166-host-out,.tmp-166-scratch-password.txt -Recurse -Force
```

Verify ports 3116/5421/55486 are closed and `docker ps -a --filter name=tooba-tb166-pg` is empty
(`cleanup-166.txt`).
