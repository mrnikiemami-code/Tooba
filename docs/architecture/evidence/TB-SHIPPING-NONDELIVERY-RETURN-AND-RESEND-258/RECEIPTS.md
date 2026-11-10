# TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258 — RECEIPTS

All timestamps are worker-local. One canonical runtime only: FE `127.0.0.1:3000` → Host `127.0.0.1:5088` →
SingleStore / `store-alpha` / `tenant-alpha` → **`tooba_alpha` @ 127.0.0.1:5432**. No disposable/test/parallel
database, no Docker PostgreSQL, no port 15433, no second runtime.

## 1. Focused Fulfillment proof — pure + REAL storage on `tooba_alpha`

```powershell
$env:TB258_CONNECTION="Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha"
$env:TB258_EVIDENCE_DIR="D:\Users\User\source\repos\SarvNewVer\docs\architecture\evidence\TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258"
dotnet test src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Tooba.Fulfillment.Tests.csproj `
  --filter "FullyQualifiedName~PostDispatchException258|FullyQualifiedName~PostDispatchExceptionRelational258" `
  --no-restore -v q
```

```
Passed!  - Failed:     0, Passed:    19, Skipped:     0, Total:    19, Duration: 1 s - Tooba.Fulfillment.Tests.dll (net8.0)
```

- Pure domain/application (`PostDispatchException258Tests`, 13): non-delivery without fake delivery/cancel and zero
  resend capacity; idempotent replay vs changed-reason conflict; a genuine later delivery resolves non-delivery but
  never a returning parcel; Delivered can never become undelivered and Created cannot skip dispatch; return only
  after non-delivery and receipt only while returning; only usable units become resendable with excess/foreign
  refusal; resend creates a linked new parcel consuming capacity once and preserving the original; resend dispatch
  never inflates lifetime shipped and delivery counts once; cancelling a pre-dispatch resend releases capacity once;
  a resend-of-resend is judged against the root receipt capacity; same dispatch key replays the same parcel; resend
  before physical receipt refused; one other in-transit parcel keeps the unit undelivered.
- REAL storage (`PostDispatchExceptionRelational258Tests`, 6 = A…F):
  - A dispatched → non-delivery → return progress → usable/damaged receipt → resend → track → dispatch → deliver,
    with the original parcel and `DispatchedAt`/tracking preserved.
  - B Delivered cannot be marked undelivered; a pre-dispatch or foreign parcel is refused.
  - C non-delivery alone grants nothing; only one real usable receipt funds one resend.
  - D two independent contexts cannot consume the same returned unit; replay of the same dispatch key returns one
    parcel.
  - E delivery is per-parcel truth: one other in-transit parcel keeps the unit undelivered.
  - F a parcel locked in an active consolidated package is refused (`locked=refused`), not silently unlocked.

Task-labelled fixtures created through production domain/application paths (never raw SQL), recorded by the tests in
`tb258-relational.txt`. The file holds **two** consecutive runs with distinct ids (the second run was executed for
this receipt). Example identities from the latest run:

```
C original=1c4e7960-0dd0-4d46-9320-fdc535f7fcbd cancelledResend=0782421b-51d0-4fe8-910a-1b542406fb50
A original=f8a8da59-f044-4ac0-bab8-e8424d9fcc53 resend=97dbdc29-0a2d-47d6-a881-92bc9aa5dbeb lineA=7ccb6365-1a19-405e-943d-5effaa03cea5 lineB=37f0ac00-8008-48ac-8c82-a957e25422a5
D winner=be83e3e7-de56-455e-a165-2e62f5f796be loserKey=tb258-D-right resendRows=1 replay=same
E first=7b93682f-8497-4dd2-874b-70be5bec2040 secondInTransit=198d67e3-4b4e-4dbe-882f-60aac0658874 delivered=1
F package=680429ad-a653-4dc1-a3d0-23f27131b7f7 members=c737f064-8898-4f6a-a708-c3397ca47f90,7d8f7f9e-ef63-412c-9e0f-8d1602e82da8 locked=refused
B delivered=aaf92994-0b92-41dc-a4c5-43feee88d5e0 preDispatch=c28b3c29-6ae0-4e5f-b6a8-72adf9d2b887
```

Nothing was deleted; no user order, parcel, receipt, payment or publication row was touched.

## 2. Host admin authority/routing seam

```powershell
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~AdminOrderFulfillmentOperations" --no-restore -v q
```

```
Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 15 ms - Tooba.Host.Tests.dll (net8.0)
```

## 3. Mounted frontend proof

```powershell
cd src\frontend
npx tsx --test app/admin/admin-order-operational-delivery.mount.test.ts
```

```
✔ TB-258 static: only the post-dispatch action the server projected is actionable
✔ TB-258 static: a returned parcel with usable capacity offers resend; facts stay readable
✔ TB-258 mounted: non-delivery posts one explicit reason and keeps the dispatch history
✔ TB-258 mounted: origin receipt records per-line quantity AND condition
✔ TB-258 mounted: resend is bounded by usable received quantity and replays one stable key
✔ TB-258 mounted: a refused exception keeps the review open with one inline error
ℹ tests 33   ℹ pass 33   ℹ fail 0
```

`npx tsc --noEmit` → exit 0. Production frontend build → exit 0 (served BUILD_ID below). `dotnet publish` of the
Host → exit 0.

## 4. Schema / migration state (repository MigrationRunner, no manual DDL)

```powershell
dotnet run --project src/backend/Host/Tooba.MigrationRunner/Tooba.MigrationRunner.csproj -- plan --tenant store-alpha
```

```
Migration Plan module=Fulfillment edition=SingleStore tenantId=store-alpha database=tooba_alpha
  connectionRef=tenant-alpha schema=fulfillment
  current=20261110120000_AddShipmentNonDeliveryAndReturnReceipt pendingCount=0 durationMs=74 result=ok
```

Every other module reported `pendingCount=0` (Offer, Pricing, Inventory, Tax, Party, Identity, Cart, Shipping,
Returns, Settlement, Support, Wallet, Notification, AccessControl, Story, Geography). Private pre-migration backup:
`D:\temp\tb258-tooba_alpha.dump`. No DDL was typed by hand, no business data SQL, no reset/reseed/backfill.

## 5. Deployed runtime identity

```powershell
Get-Content src\frontend\.next\BUILD_ID              # 8ck4YFJYTlUDkk9CZp87c
(Get-Process -Id 10304).Id                           # 10304  (next / FE :3000)
(Get-Process -Id 5184).Id                            # 5184   (Tooba.Host :5088, artifacts/publish-primary)
(Invoke-WebRequest http://127.0.0.1:5088/health).Content            # {"status":"ok"}
(Invoke-WebRequest http://127.0.0.1:3000/fa).StatusCode   # 200
```

Host was republished because the previous binary held the file lock; it was stopped, published to
`artifacts/publish-primary` and restarted (PID 5184, `/health` 200). Frontend was rebuilt and restarted (PID 10304)
serving the build above. No edition/connection/port change, no appsettings change, no DataProtection key or
configuration write, no session store rewrite. CapturedPreview local ON / checked-in OFF; 255 parked; live Tipax
deferred.

## 6. Authorized read-only DOM on the existing review order

Route `http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf`, fresh-profile headless Chrome, no
click, no form submit, no screenshot, no mutation. Because the parcel capabilities arrive from an **async** request,
the probe waits for the first projected action before measuring (the first attempt measured an empty section and
cancelled the in-flight `GET /v1/admin/shipping-services/shipments/capabilities` — Host log `- 499`, a probe
artifact, not an application defect; it is disclosed here rather than hidden).

```
ready=true  groups=3  parcels=3  overflowX=0  dir=rtl  dialogOpen=false
parcel 01a1b93f-71d4-7000-9500-e21af4a160e3: «ثبت تحویل» 44px + «ثبت عدم تحویل» 44px
parcel 01a1b942-ea82-7000-b34b-2df4be8d62bc: «ثبت تحویل» 44px + «ثبت عدم تحویل» 44px
parcel 01a1b942-beef-7000-972d-99a79c68dc30: «ثبت تحویل» 44px + «ثبت عدم تحویل» 44px
postDispatchButtonCount=3 (no return/resend exit before a recorded return)
```

Full capture: `dom/tb258-dom.json`.

## 8. Commits

```
base                 4a3a241d2f75e0081a8ed8a375a061a4f987694b   (== issued expected base)
code                 6bb99afe9b0a5ac80e12d298dee4d92e2af86441   258: record post-dispatch non-delivery, origin return receipt and quantity-safe resend
merge                407150afe                                   merge of the six issue/review docs commits (docs only)
evidence             ee0efd4c3e6b01d79b945b214555c025284360b1   258: record worker result, receipts, DOM capture and TOP/closure-plan ledger
```

Pushed to `origin/main`; local `main` == `origin/main` after the push. No force/amend/reset/stash/rebase.

## 9. Disclosures

- The `capabilities` 499 above is a probe-side cancellation; re-running with the async wait returned 3 parcels with
  both intended actions.
- No live Tipax/post request was made; no provider cancellation/label/return state exists.
- No Inventory/Returns contract write: damaged-vs-usable is durable on the receipt and drives resend capacity, but
  saleable stock is deliberately not increased while the goods stay reserved for this order.
- Shared-package safety is a refusal for a locked child; a parent-level return workflow for every member is issue-4
  territory and was not built.
- Unrelated module suites were not run; pre-existing unrelated reds were not re-baselined.
- Not Architect ACCEPT and not user VISUAL/FUNCTIONAL ACCEPT.

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
