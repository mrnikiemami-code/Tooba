# TB-262-R1 — receipts

Every command below was run once on the canonical machine/DB. No repeated green loops.

## Environment

- Source: `D:\Users\User\source\repos\SarvNewVer`
- Base: `a67e514ead76a058788add1be571512791db6646`
- Code: `e6cb0378edaf0986c8f7c1f656494ed6e8abc569`
- ONE database: `Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha` (SingleStore / `store-alpha`)
- Evidence env: `TB262_EVIDENCE_DIR=<this folder>`, `TB258_CONNECTION=<same DB>`, `TB262_CONNECTION=<same DB>`

## 1. Fulfillment — pure domain/behavior (no DB)

```
$env:TB258_CONNECTION='Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha'
dotnet test src\backend\Modules\Fulfillment\Tooba.Fulfillment.Tests\Tooba.Fulfillment.Tests.csproj -c Release `
  --filter "FullyQualifiedName~DamagedResolution262Tests"
```

Result: **Passed 8 / Failed 0**. (`A_stale_cancel_replay_never_detaches_the_new_replacement_parcel` is the new case:
cancel P1 → retry P2 → cancel P1 again → resolution still links P2, attempt unchanged.)

## 2. Fulfillment — retained 258 behavior

```
dotnet test src\backend\Modules\Fulfillment\Tooba.Fulfillment.Tests\Tooba.Fulfillment.Tests.csproj -c Release `
  --filter "FullyQualifiedName~PostDispatchException"
```

Result: **Passed 36 / Failed 0**.

## 3. Host — real DB, real Inventory directory

```
$env:TB262_CONNECTION='Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha'
$env:TB262_EVIDENCE_DIR='...\TB-SHIPPING-DAMAGED-ORIGIN-RECEIPT-RESOLUTION-AND-REPLACEMENT-262-R1'
dotnet test src\backend\Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj -c Release `
  --filter "FullyQualifiedName~DamagedGoodsResolution262"
```

Result: **Passed 8 / Failed 0** — A, B, C, D, D2, E retained plus:

- **F** `F_stale_cancellation_replay_never_detaches_the_current_replacement`
- **G** `G_the_replacement_hold_is_non_expiring_from_its_first_durable_commit`

Fixture ids appended to `tb262-relational.txt` (final run): `F firstParcel=01a1cb1c-bd8f-7000-9c49-0656f135ea20
resumedParcel=01a1cb1c-bdbd-7000-8e41-6726fad6964c staleCancelReplay=noDetach linked=01a1cb1c-bdbd-… hold=01a1cb1c-bd76-… reserved=1 rows=1 holds=1`,
`G reservation=01a1cb1c-b64c-7000-9769-c6dd6918eae0 … firstCommitExpiresAt=null retry=same reclaimerCandidate=0 reserved=1 holds=1`.
The two earlier `reclaimerReleased=0` lines are the exploratory invocations of the shared global reclaimer (see note below).

Note: the first exploratory execution of case G called `InventoryDirectory.ReleaseExpiredHoldsAsync(now+400d)` once. It
selected only already-elapsed TTL holds of the whole shared table (routine reclaimer behaviour; no order-bound hold is a
candidate) and the later invocation released 0 rows. The committed case replaces that call with the identical selection
predicate evaluated read-only, so no shared-table mutation is part of the proof.

## 4. Order — PUBLIC command against real owning storage

```
dotnet test src\backend\Modules\Order\Tooba.Order.Tests\Tooba.Order.Tests.csproj -c Release `
  --filter "FullyQualifiedName~AdminOrderPostDispatchReplay"
```

Result: **Passed 11 / Failed 0** — 10 retained (incl. `H` damaged resolution) plus:

- **I** `Relational_pending_damaged_replacement_resumes_after_a_cancelled_attempt_with_the_retained_hold`

`tb262r1-orchestrator-relational.txt`:

```
I damagedPending fulfillment=25d9cd2c-d5b8-4a8c-8222-db757514941f checkout=41869376-ce0c-4de7-8428-5a533465a9d2
source=ddb03603-ec95-4658-acca-9559e4dddf72 key=TB262R1-resume-bff56ca1ca704e3ea8cc85fb011f7996
firstAttempt=replacement_unavailable(zeroParcel) retry=pendingResume firstParcel=99bb93a3-d88d-4a1a-887c-31520c474b59
retainedReservation=97412259-b533-45bc-b861-0d2479b94441 cancelledParcel=allocationOnly(noRelease)
resumedParcel=4b59ca5d-9d67-49b7-aae4-a99e58806f2a secondReserve=0 staleCancelReplay=noDetach
rows=shipments3/decisions1 state=Created
```

The same run re-recorded the retained public cases into this folder (`tb262-orchestrator-relational.txt`,
`tb258r2-orchestrator-relational.txt`, `tb258r3-orchestrator-relational.txt`) because the evidence directory variable was
pointed at this task's folder.

## 5. Frontend

```
cd src\frontend
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
```

Result: **tests 54 / pass 54 / fail 0** — 52 retained plus:

- `TB-262-R1 mounted: a server-recorded pending decision resumes with its OWN occurrence key`
- `TB-262-R1 static: a pending damaged decision is a durable sentence with a resume entry`

```
npx tsc --noEmit          -> 0 errors
npm run build             -> 0 errors, BUILD_ID HH5Ghzd51vaj3oewQY1qF
```

## 6. Solution build

```
cd src\backend
dotnet build Tooba.slnx -c Release
```

Result: **0 Error(s)**, 263 warnings (pre-existing analyzer/NU1900 offline-vulnerability noise).

## 7. Deploy

```
Stop-Process -Id 30928 -Force
dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Release -o artifacts\publish-primary
Start-Process dotnet "Tooba.Host.dll" -WorkingDirectory artifacts\publish-primary   # new PID 36116
```

- `GET http://127.0.0.1:5088/health` → **200**
- `GET http://127.0.0.1:5088/health/ready` → **200**
  `{"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured","authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}`

```
Stop-Process -Id 13608 -Force
node node_modules/next/dist/bin/next start -p 3000 -H 127.0.0.1    # new PID 30688
GET http://127.0.0.1:3000/fa -> 200
```

- BUILD_ID `HH5Ghzd51vaj3oewQY1qF`; served chunk `7825-cec5013e3aaa3dc7.js` contains «ادامهٔ تعیین تکلیف کالای آسیب‌دیده».
- No migration added/applied by this repair (no `Migrations` change); nothing re-applied.
