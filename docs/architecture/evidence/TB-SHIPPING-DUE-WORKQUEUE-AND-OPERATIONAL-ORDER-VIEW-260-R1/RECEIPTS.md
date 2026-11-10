# TB-SHIPPING-DUE-WORKQUEUE-AND-OPERATIONAL-ORDER-VIEW-260-R1 — RECEIPTS

Point-in-time observations on the canonical instance. One database, one runtime, no parallel deployment.

## Environment

| Item | Value |
| --- | --- |
| Base SHA | `a5de61258f71ce5cceef30729a35ffac0751a972` |
| Database | `tooba_alpha` @ `127.0.0.1:5432`, user `admin` |
| Edition / tenant | `SingleStore` / `store-alpha` (dev-context `tenantId=store-alpha`) |
| Host | PID `20052`, `artifacts\publish-primary\Tooba.Host.exe`, `/health` 200, `/health/ready` 200 |
| Frontend | PID `28184`, `next start -p 3000 -H 127.0.0.1`, `BUILD_ID` `MoBhYSN-pLcFMKWSta7ls`, `/fa` 200 |
| Previous FE chunk | `/_next/static/chunks/7825-19f75933ca8b9455.js` → 404 |
| Served chunk | `/_next/static/chunks/992-24accfc967f39328.js` → 200 (contains «بدون گروه پذیرش») |
| Migration in this task | NONE (no `Migrations` file in the change set; nothing applied) |
| DataProtection | unchanged (`App_Data\dataprotection-keys\…` untouched) |
| CapturedPreview | local ON / checked-in OFF, untouched |

## Case A / A2 / B / C / D — owning projection on real storage

```
$env:TB260R1_CONNECTION = "Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha"
$env:TB260R1_EVIDENCE_DIR = docs\architecture\evidence\TB-SHIPPING-DUE-WORKQUEUE-AND-OPERATIONAL-ORDER-VIEW-260-R1
dotnet test src\backend\Modules\Order\Tooba.Order.Tests\Tooba.Order.Tests.csproj \
  --filter "FullyQualifiedName~AdminFulfillmentWorkUnitProjection260R1RelationalTests"

Passed!  - Failed: 0, Passed: 5, Skipped: 0, Total: 5, Duration: 1 s - Tooba.Order.Tests.dll (net8.0)
```

Raw emitted lines (`tb260r1-relational.txt`):

```
TB260R1-C1 caseC packageRows=1 kind=Package locator=aa8e6a4c-680f-460f-9a8d-83a4478c8112 number=MP-AA8E6A4C680F childParcelRows=0 scopeQty=2 counts=active1/upcoming1 separateRows=2
TB260R1-B   caseB rows=2 distinctKeys=2 locators=e9a720f8…,f9033fb4… bucket=Upcoming plan=shared-own-group pageSize1=2x1 groupRow=0
TB260R1-D   caseD rows=4 groupRemaining=2 openParcelScope=0 resendBucket=Unspecified resendPlan=null returnedResendable=0 returnedBlocker=true total=4
TB260R1-A2  caseA2 rows=2 noPlanGroup=Unspecified/null plannedGroup=Upcoming/handoff+2 noLending=true
TB260R1-A   caseA rows=2 parcel=InTransit/9fc0c94d… group=Upcoming/2c550347… counts=active1/today0/overdue0/upcoming1/unspecified0/transit1/due0 targetB=groupB
```

## Pure work-unit identity

```
dotnet test src\backend\Modules\Fulfillment\Tooba.Fulfillment.Tests\Tooba.Fulfillment.Tests.csproj \
  --filter "FullyQualifiedName~AdminFulfillmentWorkUnitIdentity260R1Tests"

Passed!  - Failed: 0, Passed: 5, Skipped: 0, Total: 5, Duration: 13 ms
```

## Frontend contract + mounted real components

```
node --experimental-strip-types --test app/fulfillment/fulfillment-work-queue-260.test.ts app/fulfillment/fulfillment-api.test.ts
  tests 12 / pass 12 / fail 0        (queue contract 6/6 — 4 retained + 2 TB-260-R1)

npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operations-view.mount.test.ts
  tests 5 / pass 5 / fail 0          (new: exact package number + Created-awaiting-tracking is real work)

npx tsx --tsconfig tsconfig.mount-tests.json "app/admin/orders/[checkoutId]/operations/admin-order-operations-route.mount.test.ts"
  ✔ mounted: a Created parcel awaiting tracking is real work and resolves to ITS OWN group
  ✔ mounted: a tracked Created parcel reads as ready, and a package locator names it exactly
  ✔ mounted: a stale parcel/package locator is reported instead of showing another group
  tests 3 / pass 3 / fail 0
```

(The bracket directory is a glob character for `node --test`, so that file is executed through `tsx` directly.)

## Case F — live canonical work-queue query (read-only, zero writes)

```
GET  /v1/admin/dev-context → actor=01a036c2-970e-7000-8eb7-94bf5cc2d8db tenant=store-alpha
POST /v1/admin/fulfillments/work-queue/query  {"page":1,"pageSize":20,"sort":[],"filters":[]}

status=200 total=919 page=1 size=20 items=20
distinctKeys=20 kinds=Group:8,Parcel:11,Package:1
counts totalActive=670 due=5 today=5 overdue=0 upcoming=25 unspecified=640 inTransit=249
```

First page (excerpt; full text in `tb260r1-queue-api.txt`):

```
group:2c550347…   Group   Group      grp=2c550347… shp=          pkg=          Upcoming    create_shipment
parcel:a44aa20e…  Parcel  Dispatched grp=9fc0c94d… shp=a44aa20e… pkg=          InTransit   deliver_shipment
parcel:1aff12e2…  Parcel  Created    grp=68adf391… shp=1aff12e2… pkg=          Upcoming    assign_tracking
parcel:8c81fbb2…  Parcel  Returned   grp=68adf391… shp=8c81fbb2… pkg=          InTransit   (no action: no resend capacity)
package:aa8e6a4c… Package Created    grp=          shp=          pkg=aa8e6a4c… Upcoming    assign_consolidated_package_tracking
```

Quick tab + paging:

```
filter=dispatch_today total=5 items=5 (each group:<own groupId>, bucket=Today, plan=2026-11-10)
pageSize=1 →  page1 group:2c550347…  page2 parcel:a44aa20e…  page3 group:079023e9…  (total 919 on every page)
```

The two rows of the same seller order (`group:2c550347…` Upcoming with its own locator, `parcel:a44aa20e…` Dispatched
with its own locator) are the exact defect case from the task fixtures: they are no longer one seller summary with the
earliest plan and a single locator.

## Read-only boundary

No click, no POST outside the read-only grid query, no business mutation, no user order/parcel/payment write; every
fixture is task-labelled `TB260R1-*` and was created through the production domain paths by the relational case above.
The canonical `919` rows and the counts include those labelled fixtures (disclosed, not a production baseline).

## Commits

- Base (issued expected): `a5de61258f71ce5cceef30729a35ffac0751a972`
- Code + evidence: `d70fbfe80`
- Ledger + merge of the issued/review docs (`origin/main a50dd95b2`): `d631ba5e3`
- After the ledger commit HEAD == `origin/main` (verified `git status -sb` = `## main...origin/main`), no rebase/reset/force, no unrelated overwrite.
