# TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R2 — WORKER RESULT

```
Task-ID      TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R2
Parent       TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R1
Channel      tooba-main
Worker       tooba-worker-01 / cursor
Mode         CLOSE_PUBLIC_COMMAND_REPLAY_GATE_REAL_PAGE_RECONCILE_ENTRY_AND_MIXED_RECEIPT_UI
Status       PASS within the issued three-gap scope
Decision     PUBLIC_COMMAND_EXACT_INTENT_RECONCILE_REAL_PAGE_RETAINED_RECOVERY_ENTRY_AND_SINGLE_REQUEST_MIXED_CONDITION_RECEIPT
Base         0a9d42705a3e4a8cd07d5ead1988daf2ff991d25
ExpectedBase 29d3ce3688e58273e92e51fe1b1c782e485ebc1b  (ancestor of Base; issue/review doc commits are legitimate descendants)
Code         (see result-contract.txt — code commit "feat(shipping): answer exact post-dispatch intents … 258-R2")
Stop         STOP_FOR_ARCHITECT ; automaticNextImplementationTask NONE
```

This is a worker PASS inside the issued scope only. It is **not** Architect ACCEPT and **not** user
VISUAL/FUNCTIONAL ACCEPT, and it does not close the five-defect ledger.

## 1. What was actually wrong (source-confirmed before the change)

| # | Confirmed defect | Consequence |
|---|---|---|
| A | `AdminOrderOperationsOrchestrator.Operations.cs` sent `record_non_delivery` / `start_return` / `record_return_receipt` / `resend_shipment` straight into `RunProjectedCoreAsync`, whose `EnsureProjectedActionAllowedAsync` requires the action to exist in the **current** projected state. | Once a resend consumed source capacity, or a receipt committed (`Returning` → `Returned`), the action disappeared from the projection, so an exact retry of a **lost response** was rejected **before** the owning Fulfillment replay could answer it. The durable fact existed; the public command could not reach it. |
| B | `admin-order-operational-delivery-section.tsx` only rendered the exception dialog after a normal action button, and those buttons exist only while `canRecordNonDelivery` / `canStartReturn` / `canRecordReturnReceipt` / `canResend` are true. The section never read the retained attempt store. | After a refresh or after partial/full server progress the operator had **no** visible way back to the reviewed request that was still owed an answer. A remounted manually-opened dialog proved nothing. |
| C | `post-dispatch-exception-dialog.tsx` modelled a receipt line as one quantity + one condition selector and emitted **one** item per line. | The operator could not physically record the ordinary case «۲ سالم + ۱ آسیب‌دیده» for one parcel line in one review; the domain already accepted it, the UI could not deliver it. |

## 2. What landed

### A — the exact recorded intent now reaches the owning state THROUGH the public command

* New narrow read-only contract `Tooba.Fulfillment.Contracts.Operations.PostDispatchIntent` /
  `PostDispatchIntentResult` (`NotRecorded` / `Recorded` / `ResolvedCancelled` / `Conflict`) and
  `IFulfillmentAdminOperations.ReconcilePostDispatchIntentAsync` — implemented by
  `FulfillmentAdminOperationsAdapter` → `FulfillmentDirectory.Returns.cs` → `FulfillmentUnit`
  resolvers `ResolveRecordedNonDelivery`, `ResolveRecordedReturnStart`,
  `ResolveRecordedReturnReceipt`, `ResolveRecordedResendIntent`.
* `AdminOrderOperationsOrchestrator.Fulfillment.cs` gained ONE seam `RunPostDispatchExceptionAsync`
  used by all four public operations (endpoint → MediatR → orchestrator → production adapter). It
  runs, in this order:
  1. load the checkout (`order.operation.invalid` when absent) and the **current** actor's effective
     access — a retained browser envelope is never authorization (`order.operation.denied`);
  2. require the parcel to belong to **this** checkout **and** the requested fulfillment — a forged
     pair is `order.operation.invalid`;
  3. build the exact reviewed intent with the same validation the mutating command applies
     (`BuildReceiptItems` / `BuildResendLines` are shared, so the replay and the mutation cannot
     disagree about the request);
  4. ask the owning facts: `Recorded` / `ResolvedCancelled` → return the authoritative snapshot,
     **zero writes**, no reattributed actor/time; `Conflict` → typed code
     (`fulfillment.nondelivery.conflict` / `fulfillment.return.receipt_conflict` /
     `fulfillment.return.receipt_already_recorded` / `fulfillment.resend.intent_conflict`);
  5. **only** `NotRecorded` falls through to the unchanged `EnsureProjectedActionAllowedAsync`
     (with `allowReturnLifecycleFallback: false`) and then the real mutation.
* An absent record therefore **cannot** turn a readiness failure into a new allocation; no global
  guard was removed, no client flag authorizes anything, and no error text is used as proof.
* Receipt identity now has ONE authority: `ShipmentReturnReceipt.MatchesItems(...)` is used by both
  the mutating replay (`FulfillmentDirectory.Returns.cs`) and the read-only reconcile, so they
  cannot disagree about "the same receipt". `RecordedResendAnswersIntent` is likewise one authority
  (same source parcel, same group, same normalized method, canonical per-line quantities, not
  cancelled).

### B — one real-page recovery entry survives a state refresh

* `features/order-operational-groups/session/post-dispatch-attempt-store.ts` gained
  `POST_DISPATCH_MODES` + `listPostDispatchAttemptsForParcel(...)` (newest obligation first), exported
  through the feature `index.ts` (no `app/admin` import).
* `admin-order-operational-delivery-section.tsx` computes `permittedExceptionModes(projected caps)`
  and renders exactly ONE contextual recovery row per parcel when a retained envelope exists whose
  mode the fresh projection **no longer** offers: «پیگیری «…»» at 44px + one concise sentence
  («پاسخ قطعی این درخواست ثبت نشده است؛ همان درخواست قبلی حفظ شده و فقط همان ارسال می‌شود.»).
* `admin-order-items-shipping-panel.tsx` re-reads the SAME viewer/checkout/fulfillment/parcel/mode
  envelope when the action vanished; it posts the **exact recorded request body** only when the
  retained request is byte-identical, otherwise it keeps the existing refusal. The panel owns the
  viewer scope once (`viewerScopeKey`) and shares it with the group-workflow occurrence scope.
* This is a RECOVERY entry, not authorization: the server re-checks actor and ownership on every
  request (see A). One parcel action owner is kept; no seller-aside duplicate control, no new page,
  no raw diagnostics.

### C — usable + damaged for one parcel line in ONE request

* `post-dispatch-exception-dialog.tsx` `ReceiptDraft` is now `{ usable, damaged }` with an explicit
  «سالم» and «آسیب‌دیده» numeric input per line, a live «مجموع ثبت‌شده», and per-line inline refusals:
  non-parsable quantity («مقدار واردشده معتبر نیست.»), excess («مجموع «سالم» و «آسیب‌دیده» از مقدار
  این قلم در مرسوله بیشتر است.»), and a zero-total guard. No `Math.min` clamp and no silent rewrite:
  the reviewed numbers are either sent exactly or refused inline.
* Zero categories are omitted; a line with `usable 2 + damaged 1` becomes **TWO** items of the same
  line in ONE request (`condition 0` qty 2, `condition 1` qty 1); durable capacity grants only 2.
* `POST_DISPATCH_MODE_LABEL_FA` is the single display copy shared by the card, the recovery row and
  the dialog title.

## 3. Proof (integration layers executed, not helper substitution)

| Layer | Evidence | Result |
|---|---|---|
| Fulfillment pure + relational (`PostDispatchExceptionRelational258Tests`) | `dotnet test … --filter FullyQualifiedName~PostDispatchException` with `TB258_CONNECTION` = canonical `tooba_alpha` | **35/35** (retained 258 lifecycle 13 + R1 11 + relational A–K 11) |
| Order public command → production adapter → real storage (`AdminOrderPostDispatchReplayTests`) | `dotnet test … --filter FullyQualifiedName~AdminOrderPostDispatchReplay` | **8/8** (7 pure + 1 relational through `AdminOrderOperationsOrchestrator` → `FulfillmentAdminOperationsAdapter` → `FulfillmentDirectory` on `tooba_alpha`) |
| Mounted real section + real panel (`admin-order-operational-delivery.mount.test.ts`) | `npx tsx --tsconfig tsconfig.mount-tests.json --test …` | **39/39** (3 new TB-258-R2: mixed receipt splits one line, unresolved request keeps ONE recovery entry after the action vanished, no recorded request ⇒ no entry/invented action) |
| Frontend typecheck | `npx tsc --noEmit` | 0 |
| Frontend production build | `npm run build` | 0 |
| Backend solution | `dotnet build src/backend/Tooba.slnx -c Release` | 0 errors (359 pre-existing warnings) |
| Backend publish | `dotnet publish …Tooba.Host -c Release -o artifacts/publish-primary` | 0 |

### Measured integration facts (task-owned `TB258-*` fixtures on the ONE canonical database)

`tb258-relational.txt` (Fulfillment owning facts, read-only after the write):

```
A original=25e5b971-9864-45e1-a4bf-c570269af5ab resend=9f2fe1bc-ea13-4d8e-96f1-c958d421eefc lineA=ceb881af-… lineB=513c4c82-…
B delivered=cc41ad17-0ee6-449b-b158-52c7e6da20af preDispatch=9f30917e-e107-410a-bc0b-03c13d11db43
C original=5c25fb3b-1483-4341-b23a-c5c175cccac4 cancelledResend=13d5af21-d807-4e96-b50b-d87a956ffb01
D winner=470e7cb5-62d3-4dbb-b7c7-64b13cac0623 loserKey=tb258-D-left resendRows=1 replay=same
E first=aee8d16d-… secondInTransit=3aea1446-… delivered=1
F package=fa8b5568-… members=66609150-…,4eb672d2-… locked=refused
G resend=fb6f9557-… replay=same intentConflicts=2 rows=1
H first=b5adeb82-…->ef9ed448-… second=bc063a12-…->5a7ed3ef-…
I receiptLines=3 lineAusable=1 resend=f69a0f72-… changedReceiptRefused=fulfillment.return.receipt_conflict
J nondelivery=recorded/conflict return=recorded receipt=recorded(3 lines)/conflict resend=…:recorded/conflict/blank:NotRecorded/cancelled:ResolvedCancelled rowsUnchanged=true
K mixed=usable2+damaged1:grants2 overTotal=4:refused(fulfillment.return.receipt_qty_exceeds) clamped=false persisted=false
```

`tb258r2-orchestrator-relational.txt` (public command through the production adapter):

```
G orchestration fulfillment=185b0ab7-560e-4e5a-8720-dcbbd3324930 checkout=ce4f6824-574b-4ada-85c1-d2cb35dce18c
  source=4ea48680-67fd-4850-8385-c7003931a470 resend=973e2c2e-ef46-4a64-ad4e-8a9d42bbab7b
  receipt=5fb240a1-6056-4f17-b664-9b69c7a39997 lifecycle=public-command replay=exact(sameParcel)
  changed=conflict foreign=refused rows=shipments2/receipts1
```

Coverage of the required cases A–E:

* **A** — a committed resend loses its response; the fresh projection reports source capacity 0 and no
  resend action; the **same public command** returns the **same parcel** with zero new rows (relational
  `G` + Order `Relational_public_command_replays_a_lost_resend_and_receipt_from_real_owning_storage`);
  changed quantity/source is `fulfillment.resend.intent_conflict`; another fulfillment's parcel and an
  actor without `order.handle`/`fulfillment.manage` are refused.
* **B** — the same for a committed receipt after `Returning → Returned`; the exact replay returns the
  recorded receipt, a changed receipt is `fulfillment.return.receipt_conflict`, and no actor/time is
  reattributed (`J`, `I`, Order `Exact_recorded_receipt_replays_the_same_mixed_condition_lines`).
* **C** — mounted real section + real panel: no dialog open, a retained attempt and refreshed capabilities
  with the action gone ⇒ exactly ONE visible reconcile entry ⇒ open ⇒ the SAME envelope/key is posted ⇒
  authoritative completion clears it; a changed review cannot remint (mount `TB-258-R2 mounted: an
  unresolved recorded request keeps ONE recovery entry after the action vanishes` and the static
  no-record case).
* **D** — thrown/undefined/network/5xx outcomes and close/remount/viewer switch keep the full envelope and
  never launch a second new operation (retained R1 store cases + section cases).
* **E** — mounted inputs `2 usable / 1 damaged` ⇒ exact payload (two items, same line) ⇒ owning API
  persistence/readback grants **2**; total 4 is refused **without** clamping and **without** a POST
  (mount mixed case + relational `K`).

## 4. Runtime / deployment (canonical, observed)

```
FE    PID 3024     node node_modules/next/dist/bin/next start -p 3000 -H 127.0.0.1
      BUILD_ID     mdzGQu5f7TP39KSWV_eKU     GET /fa 200
      old build    otEqpbYA0ejFbfq-8pTRE     -> 404 (no stale process)
Host  PID 34180    artifacts/publish-primary/Tooba.Host.exe   GET /health 200
      Edition SingleStore / DeploymentId store-alpha-canonical / Tenant store-alpha -> tooba_alpha
DB    ONE database tooba_alpha @ 127.0.0.1:5432 (no disposable/test/parallel/Docker DB, no 15433)
DP    App_Data/dataprotection-keys/key-d82e31a6-0269-4818-8766-c8267198bde9.xml unchanged (2026-11-02)
Migr  none added by this task (no Migrations file in the code commit)
Cfg   CapturedPreview local ON (Enabled/SelectionEnabled/CheckoutEnabled=true, AvailableDateCount=7); checked-in OFF
```

`01a1b92d-e97e-7000-999a-143350d0edaf` was **not** mutated, and no user order/cart/payment/parcel/service
row was written. All writes are task-labelled `TB258-*` fixtures created through the owning production
domain/application paths.

## 5. Changed paths

```
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Fulfillments/Models/PostDispatchIntent.cs        (new)
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Fulfillments/Ports/IFulfillmentDirectory.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Contracts/Operations/FulfillmentAdminOperationsContracts.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/FulfillmentUnit.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/ShipmentReturnReceipt.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Adapters/FulfillmentAdminOperationsAdapter.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Directories/FulfillmentDirectory.Returns.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/PostDispatchExceptionRelational258Tests.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/AdminFulfillmentBulkTests.cs   (fake member)
src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Services/AdminOrderOperationsOrchestrator.Fulfillment.cs
src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Services/AdminOrderOperationsOrchestrator.Operations.cs
src/backend/Modules/Order/Tooba.Order.Tests/Admin/AdminOrderPostDispatchReplayTests.cs           (new)
src/backend/Modules/Shipping/Tooba.Shipping.Tests/Carriers/ShippingCarrierPackageExecutionRelationalTests.cs  (fake member)
src/backend/Modules/Shipping/Tooba.Shipping.Tests/Carriers/ShippingCarrierShipmentDirectoryTests.cs          (fake member)
src/backend/Host/Tooba.Host.Tests/AdminOrderFulfillmentOperationsTests.cs                        (fake member)
src/frontend/app/admin/admin-order-operational-delivery-section.tsx
src/frontend/app/admin/admin-order-items-shipping-panel.tsx
src/frontend/app/admin/admin-order-operational-delivery.mount.test.ts
src/frontend/features/order-operational-groups/index.ts
src/frontend/features/order-operational-groups/session/post-dispatch-attempt-store.ts
src/frontend/features/order-operational-groups/ui/post-dispatch-exception-dialog.tsx
```

## 6. Disclosed limits

* No live Tipax provider call, no credentials, no waybill; the lifecycle stays internal and audited.
* The origin receipt remains a **one-time final receipt**; incremental receipts were not designed.
* Damage grants no resend capacity and no saleable-stock increase (no Inventory/Returns contract write).
* A child locked in an active consolidated package is still refused (`fulfillment.shipment.package_locked`);
  the parent-level shared/independent parcel choice belongs to 259 and was **not** implemented here.
* Concurrency is proven with two real independent contexts plus DB guards, not a stress test.
* No automatic DOM/screenshot was produced: the task made it optional, and the required proof is the
  fixture integration matrix above (the real section/panel is mounted against the real API callback).
* `frontend-source-size` remains the same pre-existing red family at base (`admin-api.ts`,
  `admin-error-map.ts`, `admin-order-items-shipping-panel.tsx`, `admin-order-operational-delivery-section.tsx`,
  its mount test, …). The three touched files were **already** over the guard at `0a9d42705`; no baseline
  was edited and no allowlist entry was added.
* Issue-3 is **not** closed by this worker result: it is pending Architect review. 259/260 remain NOT ISSUED.

STOP_FOR_ARCHITECT ; automaticNextImplementationTask NONE
