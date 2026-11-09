# TB-SHIPPING-TRACKING-UNIQUENESS-AND-QUANTITY-AUTHORITY-257 — RESULT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-TRACKING-UNIQUENESS-AND-QUANTITY-AUTHORITY-257
Parent-Task: TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: REPAIR_AUTOMATIC_TRACKING_COLLISIONS_AND_UNIFY_EXECUTABLE_QUANTITY_AUTHORITY
Expected-Base-SHA: 662254a1efe2a1eb42534b7f9ccb9f2a28e6c55f
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

Status: **PASS** within the issued two-defect scope (defect 1 automatic tracking uniqueness, defect 2 truthful remaining
parcel quantity), decision
**WHOLE_PARCEL_DETERMINISTIC_INTERNAL_TRACKING_WITH_DB_BOUNDED_RETRY_AND_ONE_PACKED_MINUS_SHIPPED_MINUS_OPEN_QUANTITY_AUTHORITY**.
This is NOT Architect ACCEPT and NOT user VISUAL/FUNCTIONAL ACCEPT. Defects 3–5 (non-delivery/return/resend, shared-group
common-or-separate parcels, due workqueue/shared operational view) are **NOT ISSUED** and were not touched.

---

## 1. Measured root causes (source first, then real storage)

### 1.1 Defect 1 — the automatic tracking button produced an already-used code

`src/frontend/app/admin/admin-assign-shipment-tracking-modal.tsx` derived the "تولید خودکار" suggestion as

```ts
const suffix = shipmentId.replace(/[^A-Za-z0-9]/g, "").slice(0, 8).toUpperCase();
return `TRK-${suffix || Date.now().toString(36).toUpperCase()}`;
```

That is the **first eight hex characters of the UUIDv7 shipment id**, i.e. a 32-bit bucket that changes roughly every
65 seconds. Two parcels created in the same minute therefore received the **same** candidate. The modal is a local
suggestion: pressing «ثبت کد رهگیری» posts the value through the existing assignment command, and the real
PostgreSQL unique index `ix_shipments_tracking_reference` rejected the second parcel with a duplicate. Pressing
«تولید خودکار» again re-derived the *same deterministic* value, so the operator could not escape the failure — that is
exactly the reported "generates a previously used code and fails duplicate".

The server had no automatic-allocation seam at all: candidate generation lived only in the browser, so no bounded
retry, no reconciliation and no uniqueness guarantee could exist behind it.

The real business/DB uniqueness scope is the column-level unique index on
`fulfillment.shipments.tracking_reference` (case-insensitive in the existing duplicate probe) — not an array of rows
loaded by the screen.

### 1.2 Defect 2 — shipped quantity was still offered as an executable remainder

`src/backend/Modules/Order/Tooba.Order.Infrastructure/Integrations/Shipping/OrderCommittedCarrierShipmentFactsReader.cs`
computed

```csharp
var remaining = item.QuantityPacked - openAllocated.GetValueOrDefault(lineId);
```

subtracting only **open Created** allocations and ignoring `QuantityShipped`. A packed1/shipped1 group therefore still
produced one executable line with `createParcelEligible=true`, while the same request was refused by the owning create
command and the create modal had nothing to select. The same omission was duplicated in the order detail composer, the
capability projector, the operations orchestrator rules/projection, the admin fulfillment operations adapter and the
fulfillment work-queue filters — five divergent copies of one business rule.

---

## 2. What changed (smallest owning seams, no new framework)

### 2.1 A — one owning internal tracking reference (`Tooba.Fulfillment.Contracts`)

New `FulfillmentInternalTrackingReference`:

* `ForShipment(shipmentId, attempt = 0)` → `TRK-` + the **whole** 32-character shipment id (`-N` for bounded retries),
  ceiling `MaxLength = 128` (the real column ceiling), `MaxAllocationAttempts = 5`.
* Deterministic **per parcel**: a deliberate retry, double click or lost response resolves the same candidate; a
  different parcel always gets a different candidate.
* `IsCanonicalSuggestion` marks only attempt 0 as "automatic intent".

Owning command seam: `IFulfillmentAdminOperations.AssignAutomaticTrackingAsync` →
`FulfillmentAdminOperationsAdapter` → `FulfillmentDirectory.AssignAutomaticTrackingAsync`:

* reconciles an existing reference unchanged (never replaces a real code);
* skips a candidate already owned by another parcel;
* on a real collision (`DbUpdateException` whose `PostgresException` is `23505` on
  `ix_shipments_tracking_reference`) reverts **only** that tracking assignment and moves to the next bounded candidate in
  the same scoped context;
* every other persistence failure propagates unchanged (proven by a synthetic `23503` on a different index);
* exhausted attempts fail closed with the existing `fulfillment.tracking.duplicate` contract code.

Both admin paths route automatic intent to that seam (`AdminOrderOperationsOrchestrator.Fulfillment.cs` for the
operations request, `AdminOrderFulfillmentOperations.cs` for the accepted-group/seller-side path). Manual operator input
keeps the existing strict duplicate validation (frontend inline + server `fulfillment.tracking.duplicate`); nothing was
weakened to make automatic mode green. Provider (CarrierApi/Tipax) identifiers are untouched and never overwritten or
faked; no live provider call was made; historical short codes on existing parcels were left exactly as they are.

Frontend: `suggestShipmentTracking` mirrors the backend rule (whole id), the «تولید خودکار» button restores this parcel's
canonical candidate, and the modal now has a **synchronous same-tick single-flight** guard (`useRef`) so a click burst in
one tick sends exactly one request; the operator's value is retained on a refusal with one inline message.

### 2.2 B — one owning packed quantity authority (`Tooba.Fulfillment.Contracts`)

New `FulfillmentPackedQuantityAuthority`:

```
RemainingPackedQuantity(packed, shipped, openAllocated) = max(0, packed − shipped − openCreatedAllocations)
```

`OpenAllocatedQuantity(snapshot, line, shippingGroupId?)` reads the same authoritative Fulfillment shipments
(Created only, group-scoped) that the existing code already used, so `quantityAllocated`-style values are never
double-subtracted and inconsistent facts can never manufacture stock.

Consumers switched to that one rule (no behaviour change beyond the defect):

| Surface | Change |
| --- | --- |
| `OrderCommittedCarrierShipmentFactsReader.ResolveFulfillmentEligibility` | remaining now excludes shipped (the defect) |
| `AdminOrderDetailComposer` | one allocation reading (fourth local formula removed) + suggested reference for untracked parcels |
| `AdminFulfillmentCapabilityProjector` | `unpackable`/`shippable` share the authority |
| `AdminOrderOperationsOrchestrator.Rules/Projection/Fulfillment` | command-side checks use the authority |
| `AdminOrderFulfillmentOperations` | generic accepted-group create quantity gate |
| `AdminFulfillmentQueueFilters` | work-queue "has shippable remainder" filters |

Consequences preserved by design: wholly shipped → no executable line/no create entry; partial shipped + open
allocation → only the real remainder; a cancelled pre-dispatch allocation releases its reservation; a delivery never
makes already-shipped quantity shippable again; sibling shipping groups stay isolated (group filter). The
occurrence/replay reconcilitation of 249/253 runs **before** these remaining-quantity gates and was not modified, so a
recorded attempt still resolves its original parcel while a genuinely new occurrence obeys the remaining quantity.

No migration, no schema change, no manual SQL, no data rewrite/backfill, no new module dependency, no baseline or
allow-list edit.

---

## 3. Proof (source / pure / real storage / mounted / canonical)

| Layer | Command | Result |
| --- | --- | --- |
| Pure A+B authority | `dotnet test Tooba.Fulfillment.Tests --filter FullyQualifiedName~TrackingAndRemainingQuantityAuthority257` | **10/10** |
| Real PostgreSQL (only `tooba_alpha`) | same project, `--filter FullyQualifiedName~AutomaticTrackingRelational257` with `TB257_CONNECTION` | **4/4** |
| Order facts/eligibility | `dotnet test Tooba.Order.Tests --filter FullyQualifiedName~OrderCommittedCarrierShipmentFactsEligibility` | **5/5** |
| Host admin routing seam | `dotnet test Tooba.Host.Tests --filter FullyQualifiedName~AdminOrderFulfillmentOperationsTests` | **6/6** |
| Mounted operator tracking modal | `tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-create-shipment-modal.mount.test.ts` | **7/7** (4 are TB-257) |
| Mounted operational section (253/254/256 preserved) | `… --test app/admin/admin-order-operational-delivery.mount.test.ts` | **27/27** |
| Frontend typecheck | `npx tsc --noEmit -p tsconfig.json` | **0** |
| Frontend production build | `npm run build` | **0**, BUILD_ID `KRvftNHH3cYhdoBybBqej` |
| Backend publish | `dotnet publish Tooba.Host -c Release -o artifacts/publish-primary` | **0** |
| Architecture guards | `frontend-source-size`, `frontend-flat-folder` | **unchanged pre-existing RED** (18 findings / 11 flat-folder files, identical family; this task added none) |

### 3.1 Real-storage facts (`docs/.../tb257-relational.txt`, task-labelled fixtures in `tooba_alpha`)

* **A** two parcels of one task fixture → `TRK-48F8B04993AA45C395CA57DA83430882` and
  `TRK-BC653D3D37C641E296953638332C1779`; assigning the first twice returned the same code; the raw rows carry exactly
  those codes and exactly one row per code.
* **B** another parcel already held this parcel's canonical candidate → the target got the bounded retry
  `TRK-D2ECBC9B84FA4B0B94C36D1BC36B3B7F-1` while the squatter kept the canonical value.
* **C** an injected `23505` on `ix_shipments_tracking_reference` was retried to attempt 1 and the parcel ended tracked.
* **D** an injected `23503` on a different index propagated unchanged and the parcel stayed untracked.
* **E** two independent DbContexts allocating in parallel produced two distinct valid codes, each persisted.

Fixtures were created only through production domain/application paths (`CreateFromPaidOrder` → processing → packing →
shipment), are named `TB257-A/B/C/D/E*`, and their shipment ids are recorded in the evidence file. No user order, parcel,
tracking code or published method was written, and nothing was deleted or re-seeded.

### 3.2 Canonical read-only console proof on the known review order

Order `01a1b92d-e97e-7000-999a-143350d0edaf` (read-only; task-owned fresh-profile headless Chrome, DOM only, no
screenshot, no click, no mutation):

| | before fix (base runtime) | after fix (deployed) |
| --- | --- | --- |
| operational groups | 3 | 3 |
| group items | 2×«ارسال‌شده», 1×«ارسال‌شده», 1×«ارسال‌شده» | identical (unchanged) |
| parcels | 1 per group, codes `TRK-01A1B93F`, `TRK-01A1B942`, `TRK-01A1B9423` | identical (never touched) |
| «ایجاد مرسوله» entries | **3** (one dead-end entry per wholly shipped group) | **0** |
| horizontal overflow / dir | none / rtl | none / rtl |

Raw captures: `dom-order-before.json`, `dom-order-after.json` (script `scripts/tb257-order-quantity-dom.mjs`).

---

## 4. Deploy (canonical, single database)

* Backend republished to `artifacts/publish-primary`, Host5088 restarted (PID 21536 → **38976**), `/health` 200,
  `SingleStore` / deployment `store-alpha-canonical-tb256` / tenant `store-alpha` → `tooba_alpha`.
* Frontend rebuilt and restarted on FE3000 (PID 20404 → **44384**), served BUILD_ID `KRvftNHH3cYhdoBybBqej`; the served
  chunk `/_next/static/chunks/6411-fdb48bcccfc432f7.js` returns 200 and carries the new `TRK-` derivation.
* Same `tooba_alpha` on `127.0.0.1:5432`; no disposable/parallel/Docker database and no port 15433; no migration; no
  edition/connection/port switch; DataProtection keys preserved (`key-d82e31a6-…xml`, SHA-256 prefix
  `B6985A10085AC272` before and after); CapturedPreview local ON / checked-in OFF; 255 parked; live Tipax deferred.

---

## 5. Honest limits

* The automatic path is a **server-owned deterministic candidate + DB-authoritative allocation**, not a new global
  numbering service; uniqueness is exactly the existing unique index, which is what the retry reacts to.
* Existing short/historical tracking codes were not rewritten (no backfill was requested or performed); only new
  automatic allocations use the whole-parcel form.
* No live provider (Tipax) call, no real post waybill and no provider identifier was produced or overwritten.
* The concurrency proof is two genuinely independent contexts performing real PostgreSQL writes; it is not a
  thousands-of-writers stress test.
* `frontend-source-size` (18) and `frontend-flat-folder` (11 files, 2 rules) remain the same pre-existing RED families as
  at base — no baseline file was edited, and this task's files were deliberately placed so the counts do not grow.
* Not verified: full unrelated module suites, user VISUAL/FUNCTIONAL acceptance, defects 258–260.

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
