# TB-SHIPPING-DUE-WORKQUEUE-AND-OPERATIONAL-ORDER-VIEW-260-R1 — RESULT

Status **PASS** within the issued repair scope, decision
**ONE_ROW_PER_PHYSICAL_WORK_UNIT_WITH_ITS_OWN_ACCEPTED_PLAN_EXACT_GROUP_PARCEL_PACKAGE_LOCATOR_AND_TRUTHFUL_TARGET_READINESS**,
NOT Architect ACCEPT and NOT user VISUAL/FUNCTIONAL ACCEPT, and NOT by itself the five-defect completion.

Base `a5de61258f71ce5cceef30729a35ffac0751a972` (== issued expected base, == HEAD before this task's code).
Code SHA is the commit that carries this file; evidence SHA is the following evidence/ledger commit; merge HEAD == origin/main after the ledger commit. Single worker, no parallel task.

## Confirmed defect (measured before the fix)

`AdminFulfillmentWorkQueueQueryEngine.MapPageAsync` produced `rows.Select(unit => …)`: **one row per FulfillmentUnit**
(a seller summary) with ONE whole-unit `pending` flag and `Earliest(plans)` as the target group. Measured consequences:

* a seller order whose group A was dispatched today and whose group B was still pending advertised the *whole* unit as
  pending with group A's plan — so B could never appear as «آینده» and the row pointed at A;
* two independent parcels of one seller order collapsed into one row (one `FulfillmentId` key);
* one cross-seller common package was re-printed once per seller (inflated counts, duplicated action owner);
* readiness (`readinessOf`) only looked at `remainingPackedQuantity`, so a Created/tracked parcel awaiting dispatch was
  labelled «کار فیزیکی باقی نمانده».

## Landed

### 1. Physical work-unit identity (`AdminFulfillmentWorkUnitProjection`, new owning seam)

Read model now contains exactly these row kinds, built from existing Fulfillment facts plus the Contracts-only
`IOrderDispatchPlanFactsReader` (no cross-module DbContext, no plan averaged/borrowed):

| kind | one row means | locator | state |
| --- | --- | --- | --- |
| `Group` | one accepted shipping group that still owns its OWN processing/packing/parcel remainder | `shippingGroupId` (null = «بدون گروه پذیرش») | `Group` / `Ungrouped` |
| `Parcel` | one existing non-cancelled, un-locked physical parcel (including a TB-259 separate-mode parcel and TB-258 resend parcels) | `shippingGroupId` + `shipmentId` | shipment status |
| `Package` | ONE active common package across ALL of its locked members (child parcels expose nothing of their own) | `packageId` + `packageNumber` | package status |
| `Fulfillment` | history row of a seller unit with no open physical unit (delivered/cancelled/failed) | — | fulfillment status |

`AdminFulfillmentWorkUnitKinds.Key(kind, id)` → `group:{id}` / `parcel:{id}` / `package:{id}` / `fulfillment:{id}` is the
ONLY row identity: it is the grid key (`id`), the default and explicit sort tie-break, the page boundary and the row
action test id. Two parcels of one seller order can no longer share an id, swap places between identical pages or be
collapsed by React keying.

### 2. Scoped plan, status and quantity (no lending)

* plans are mapped by `(SellerOrderId, ShippingGroupId)` once per page; a group/parcel row reads **only its own**
  accepted group's plan — a dispatched sibling's old plan can never make a later Created parcel due;
* a Created resend parcel (`ResendOfShipmentId != null`) is funded by returned capacity, so it carries **no**
  first-dispatch plan and stays truthfully `Unspecified`;
* each row reports its own `scopeLineCount` / `scopeQuantity` / `scopeRemainingQuantity`
  (`packed − shipped − open Created`, reusing the TB-257 `FulfillmentPackedQuantityAuthority`), so quantity is never
  double-counted across two rows of one order;
* the package row aggregates its members' quantities for display only; its members contribute no extra action row;
* counts and quick-filter tabs are still computed by the SAME engine over the SAME search/column-filtered set — now
  over the corrected physical rows (server tab counts unchanged in meaning).

### 3. Exact locator + truthful target readiness

* the single «عملیات ارسال» entry now carries the exact target
  `/admin/orders/{checkoutId}/operations?fulfillmentId=…&shippingGroupId=…&shipmentId=…&packageId=…&from=queue&filter=…`;
* the shared route resolves (never guesses) that locator: a stale `shipmentId`/`packageId` yields zero target cards with
  the explicit unresolved notice instead of silently showing another group;
* `readinessOf` now follows the focused physical unit: Created+tracked → «آمادهٔ ارسال به حامل», Created w/o tracking →
  «در انتظار ثبت کد رهگیری», Dispatched/InTransit → «در مسیر تحویل — در انتظار ثبت تحویل», NonDelivered / Returning /
  Returned / Delivered / Cancelled / Failed each truthful, and a package has its own equivalent states. A zero
  fresh-creation remainder no longer means «کار فیزیکی باقی نمانده» while a parcel still needs work;
* because a scoped row can only represent its own unit, the queue's per-fulfillment kebab (a fulfillment-level command
  surface) is no longer offered from a `Group`/`Parcel`/`Package` row — that row's single action owner is the exact
  «عملیات ارسال» entry. History rows keep the menu when the server allows actions. No duplicate child action;
* the status column badge now shows THAT unit's state (`parcel: Created` under a dispatched fulfillment no longer looks
  dispatched) with a second line naming what the row owns («گروه ارسال» / «مرسوله» / «بستهٔ تجمیعی MP-…»).

Work-unit kind tokens are PascalCase on the wire and normalised once on the client (`workUnitKind.toLowerCase()`), so a
kind comparison can never silently fail on casing.

No migration, no schema change, no new lifecycle, no new page, no second operational UI; 249/253 occurrence replay,
254-R1 one-action-owner, 256 scheduling/save, 257 quantity/tracking, 258 exception/resend capacity, 259 immutable
packing mode, reservations, money and accepted evidence untouched.

## Proven

### Case A / A2 — one seller, two groups; no lending (REAL `tooba_alpha`)

`AdminFulfillmentWorkUnitProjection260R1RelationalTests` **5/5** (real Order+Fulfillment DbContexts, real Contracts
bridge, task-labelled `TB260R1-*` fixtures):

```
TB260R1-A  caseA rows=2 parcel=InTransit/9fc0c94d… group=Upcoming/2c550347…
           counts=active1/today0/overdue0/upcoming1/unspecified0/transit1/due0 targetB=groupB
TB260R1-A2 caseA2 rows=2 noPlanGroup=Unspecified/null plannedGroup=Upcoming/handoff+2 noLending=true
```

The defect case (dispatched-today A + pending B) now yields B `Upcoming` with locator B and A as independent
`InTransit` work; the dispatched plan is not re-used for the pending group, and a null-plan group never borrows a real
plan.

### Case B — two Created parcels of one seller (REAL storage)

```
TB260R1-B rows=2 distinctKeys=2 locators=e9a720f8…,f9033fb4… bucket=Upcoming plan=shared-own-group
          pageSize1=2x1 groupRow=0
```

Two rows, two distinct stable keys/locators, own statuses/plans; `pageSize=1` returns both exactly once; totals
consistent.

### Case C — common package vs TB-259 separate packing (REAL storage)

```
TB260R1-C1 caseC packageRows=1 kind=Package locator=aa8e6a4c… number=MP-AA8E6A4C680F
           childParcelRows=0 scopeQty=2 counts=active1/upcoming1 separateRows=2
```

Cross-seller common package = ONE parent row/action target with zero locked-child duplicates and no count inflation;
the separate-mode shape stays two independent physical parcels.

### Case D — mixed remainder + existing parcel + Created resend (REAL storage)

```
TB260R1-D caseD rows=4 groupRemaining=2 openParcelScope=0 resendBucket=Unspecified resendPlan=null
          returnedResendable=0 returnedBlocker=true total=4
```

No phantom first-allocation quantity, no shipped-plan contamination, a Created resend stays `Unspecified` with no
inherited plan, and a receipt-exhausted returned parcel truthfully offers no resend.

### Case E — mounted real route/section

* `admin-order-operations-view.mount.test.ts` **5/5** (new: package work row names the exact package number and a
  Created package awaiting tracking is real work).
* `app/admin/orders/[checkoutId]/operations/admin-order-operations-route.mount.test.ts` **3/3** (new): mounts the REAL
  route component against a mapped server detail — a Created parcel awaiting tracking is «در انتظار ثبت کد رهگیری» and
  never «کار فیزیکی باقی نمانده», the parcel locator resolves to ITS OWN group only, a tracked parcel reads «آمادهٔ
  ارسال به حامل», a package locator shows both member groups + `MP-260R1PKG`, a stale parcel/package locator reports the
  unresolved notice, the SAME items/operational panels render, and no financial marker is present.

### Case F — live canonical API (read-only, GET/POST query only, zero writes)

On the deployed Host through `/v1/admin/dev-context` + `POST /v1/admin/fulfillments/work-queue/query`
(`tb260r1-queue-api.txt`):

```
total=919 page1 items=20 distinctKeys=20 kinds=Group:8,Parcel:11,Package:1
counts totalActive=670 due=5 today=5 overdue=0 upcoming=25 unspecified=640 inTransit=249   (670+249=919)
filter=dispatch_today → total=5 items=5, every row group:<its own groupId>, bucket=Today, plan=2026-11-10
paging pageSize=1 → page1 group:2c550347…, page2 parcel:a44aa20e…, page3 group:079023e9… (total 919 each)
```

Page 1 shows the exact repaired defect case from the task fixtures: `group:2c550347…` (Upcoming, own locator) and
`parcel:a44aa20e…` (Dispatched, own locator) are two rows of ONE seller order, plus a single `package:aa8e6a4c…` row and
no locked-child duplicate.

### Frontend contract

* `app/fulfillment/fulfillment-work-queue-260.test.ts` **6/6** (4 retained + 2 R1: distinct ids/kind normalisation/exact
  locators for two units of one order; the quantity cell reports the row's own scope and falls back to the historical
  calculation only when the payload has no scope).
* `app/fulfillment/fulfillment-api.test.ts` **6/6** (unchanged).

### Build / typecheck / publish

```
npm run typecheck   → exit 0
npm run build       → exit 0, BUILD_ID MoBhYSN-pLcFMKWSta7ls
dotnet build src\backend\Tooba.slnx -c Release → 0 errors
dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Release -o artifacts\publish-primary → exit 0
```

## Deploy

NO migration added and none applied (no `Migrations` change). Host republished and restarted: PID **20052**,
`/health` 200, `/health/ready` 200 → `{"edition":"SingleStore","postgresql":"configured",…}`. FE rebuilt and restarted:
PID **28184**, BUILD_ID `MoBhYSN-pLcFMKWSta7ls`, `/fa` 200, previous BUILD_ID chunk `7825-19f75933ca8b9455.js` → 404,
served chunk `992-24accfc967f39328.js` → 200 carrying «بدون گروه پذیرش».

ONE database `tooba_alpha` @ 127.0.0.1:5432; Edition `SingleStore`, tenant `store-alpha`; no disposable/test/parallel/
Docker DB, no 15433, no edition/connection/port/appsettings change; DataProtection key file and CapturedPreview state
unchanged (local ON / checked-in OFF); no manual DDL/business SQL/reset/reseed/backfill; user order/cart/parcel/payment
rows read-only; only task-labelled `TB260R1-*` fixtures created through production domain paths; 255 parked; live Tipax
deferred.

## Known limits (disclosed, not defects claimed fixed)

* No screenshots or DOM matrix in this repair (task: read-only DOM optional). Real UI proof is the mounted route/view
  against a mapped server payload plus the live API; the visual appearance of the accepted TB-260 UI is unchanged.
* Canonical counts (`919` rows / `670` active) now include the task-labelled TB-260/TB-260-R1 fixtures; they are not a
  pure production baseline.
* Plan-bearing classification is still fixture-derived — no live plan-bearing real checkout exists and TB-256/259 forbid
  inventing dates.
* A returned parcel with no remaining usable capacity truthfully offers no queue action (blocker text), and package
  status `Delivered` intentionally owns no queue row; its members fall back to their own rows only when no active package
  row covers them.
* Concurrency/recovery inherited from 249/253/257/258/259; no new stress test.
* Known legacy leak unchanged: pre-existing fixture checkouts whose row carries `SalesChannel='Web'` still answer 500 on
  admin order detail; the operational view degrades honestly and TB-260-R1 does not touch that mapping.

## Ledger

Save prerequisite CLOSED within scope; issue1 tracking FIXED 257; issue2 remaining quantity FIXED 257; issue3
non-delivery/return/resend ACCEPTED 258-R3 within internal-operator scope; issue4 common-or-separate packing ACCEPTED 259
within scope (4/5); **issue5 due workqueue/shared operational order view REPAIRED by 260-R1 — pending Architect review of
this result**. No five-defect completion is claimed here.

STOP_FOR_ARCHITECT; automaticNext NONE.
