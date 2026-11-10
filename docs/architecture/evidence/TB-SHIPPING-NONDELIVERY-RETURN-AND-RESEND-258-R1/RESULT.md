# TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R1 — RESULT

Task-ID: `TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R1`
Parent: `TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258`
Mode: `REPAIR_RESEND_INTENT_REPLAY_SOURCE_CAPACITY_AND_MIXED_CONDITION_RECEIPT`
Channel: `tooba-main`
Base-SHA: `00917021d0d63c8e6f5f5c8992eb1a40e15a1601`
Status: **PASS** within the issued four-gap repair scope.
Decision: **OWNING_RESEND_OCCURRENCE_REPLAY_SOURCE_PARCEL_CAPACITY_AND_MIXED_CONDITION_ORIGIN_RECEIPT_REPAIRED**

This is NOT Architect ACCEPT and NOT user VISUAL/FUNCTIONAL ACCEPT. It is not the five-defect completion: only
issue 3 (non-delivery / physical return / origin receipt / quantity-safe resend) is under repair; issues 4 and 5 remain
NOT ISSUED.

## 1. What 258 delivered and what the review rejected

258 added the lawful parcel-level exception path (`Dispatched/InTransit → NonDelivered → Returning → Returned` plus a
durable origin receipt) and a quantity-safe resend on the existing track/dispatch/deliver path. The independent source
review accepted the lifecycle and found four concrete gaps, each confirmed in source before this repair:

| Gap | Source defect |
| --- | --- |
| A | `FulfillmentDirectory.Returns.CreateResendShipmentAsync` returned ANY existing resend carrying the dispatch key **before** comparing the requested source/group/quantities, and a blank key silently took a non-idempotent path. A changed request could receive another intent's parcel; a missing key could allocate twice. |
| B | `post-dispatch-exception-dialog.tsx` reset drafts and minted a fresh occurrence in an effect keyed on `[open, mode, parcelKey, lines]`. A lines rerender, close/reopen or remount after an uncertain response could produce a second operation. |
| C | `FulfillmentUnit.ReturnedUsableQuantity/ResendCommittedQuantity` pooled one order line across every receipt of the unit and `CreateResendShipment` replaced a returned resend with its chain root, so parcel A could borrow parcel B's returned units and the immediate physical lineage was lost. |
| D | `ShipmentReturnReceipt` de-duplicated by `OrderLineId` alone, so one parcel line could not be received as usable+damaged (3 units = 2 usable + 1 damaged). |

## 2. Repair A — exact prebound resend intent, owning replay before the capacity gate

`Tooba.Fulfillment.Domain/Aggregates/FulfillmentUnit.cs`

- A resend occurrence is now a **prebound reviewed intent**. A blank/whitespace key is a typed refusal
  (`fulfillment.resend.occurrence_required`) instead of a non-idempotent fallback; `Shipment.CreateResendShipment`
  refuses a blank key too, and an empty server actor is `fulfillment.resend.actor_required`.
- The recorded occurrence is resolved **inside the owning aggregate and before any capacity gate**:
  `ReplayRecordedResend` accepts it only when the source parcel (`ResendOfShipmentId`), shipping group, effective
  method identity and canonical per-line quantities all match. Anything else is `fulfillment.resend.intent_conflict`
  with zero new rows — never another request's parcel.
- A **cancelled** recorded resend is not invisible: it keeps its history and answers
  `fulfillment.resend.occurrence_cancelled`, so a retry cannot silently recreate it or loop into the unique index. A
  deliberate replacement is a NEW occurrence, lawful only through the caller's normal authority.
- `FulfillmentDirectory.Returns.CreateResendShipmentAsync` no longer short-circuits outside the aggregate: it captures
  the existing shipment-id set, lets the aggregate decide, and treats an already-recorded id as a pure replay
  (rollback, no allocation, same recorded parcel returned). Authorization, method/group enforcement and the
  active-package lock are unchanged and cannot be bypassed by a replay.

## 3. Repair B — retained complete client request through an uncertain outcome

New feature-owned store: `src/frontend/features/order-operational-groups/session/post-dispatch-attempt-store.ts`
(public surface exported from the feature `index.ts`).

- The **complete reviewed envelope** (code, non-delivery reason/note or receipt lines with condition, or resend
  selections, plus the occurrence key and attempt count) is persisted **before the first POST**, scoped to
  `viewerScopeKey:checkoutId:fulfillmentId:sourceShipmentId:mode`.
- `post-dispatch-exception-dialog.tsx` initialises exactly once per scope signature: a lines rerender/refresh can no
  longer reset an open review, overwrite edits or mint another occurrence.
- A synchronous `useRef` single-flight makes two same-tick submits send one mutation.
- Timeout / network / 5xx / undefined / typed refusal keep the exact envelope (`markPostDispatchAttemptFailed`); there
  is no bare-error-code release, mirroring the accepted 253 retained-attempt rule.
- Close/reopen/remount restores the recorded request, renders it read-only (one amber notice, no editable conflict),
  and offers ONE submit that reconciles it verbatim — even when fresh capacity/readiness is now false because the
  server already consumed it. Only an authoritative completion clears the envelope; only then may a new review mint a
  new occurrence. A viewer switch never inherits another viewer's attempt; unavailable storage degrades to "no
  retained review", never to a blind remint.
- Presentation stays inside the existing purposeful dialog: one concise inline localized error, no raw diagnostics, no
  extra page, no input outside the review dialog.

## 4. Repair C — source-parcel capacity and precise physical lineage

- New per-source readers: `ReturnedUsableQuantity(sourceShipmentId, orderLineId)`,
  `ResendCommittedQuantity(sourceShipmentId, orderLineId)`, `ResendableQuantity(sourceShipmentId, orderLineId)` and
  `HasResendableQuantity(sourceShipmentId)`. Capacity belongs to the parcel that physically came back, so a sibling
  returned parcel contributes nothing (`A usable1 + B usable1` can never fund `resend from A qty2`).
- `CreateResendShipment` no longer substitutes a chain root: a resend of a resend links its **immediate** returned
  source, so `A → R1 → R2` keeps the full ancestor chain readable while the first receipt cannot fund a second live
  branch.
- A cancelled pre-dispatch resend releases only its own source's capacity, once; concurrent independent requests are
  serialized by the existing `Serializable` transaction and the exact-once keys (proven with two real contexts).
- The 257 `packed − shipped − open` authority is untouched: a Created resend is funded by returned capacity and is
  skipped by `OpenAllocatedQuantity`, so no ordered/packed inflation, no lifetime-shipped decrement, no second
  reservation consume, no settlement/refund/saleable-stock movement.
- Secondary readers now consume the same authority: the admin detail composer, the operational projection
  (`ResendableQuantity` per parcel line; `canResend` only when THAT parcel still has capacity) and the dialog lines.

## 5. Repair D — mixed usable/damaged receipt for one line

`Tooba.Fulfillment.Domain/Aggregates/ShipmentReturnReceipt.cs`

- Duplicate authority is now `(OrderLineId, Condition)`, and validation sums ALL conditions of a line against what
  **this** parcel line actually carried. `3 units = usable2 + damaged1` is representable and grants exactly 2 usable
  units; damaged units grant zero.
- Positive quantity, owning line, legal condition, duplicate `(line, condition)` and aggregate excess are typed
  refusals; a missing quantity never becomes usable by default.
- Replay compares the complete canonical receipt: an identical receipt is reconciled, any changed quantity/condition is
  `fulfillment.return.receipt_conflict` and adds no capacity.
- The receipt remains a **one-time final** receipt; the operator wording says so and incremental receipt is not
  claimed. A new database index `ux_shipment_return_receipt_items_line_condition` enforces the pair at storage level.

## 6. Durable operator audit

Previously several actor arguments were discarded (`_ = actorUserId`). Now:

- `fulfillment.shipments.created_by_user_id`, `non_delivered_by_user_id`, `return_started_by_user_id` are persisted
  through the owning aggregate; the receipt already records `received_by_user_id`.
- An exact replay of a non-delivery does **not** reattribute the operator fact to the second caller.
- Audit is verified by reading the raw columns back from `tooba_alpha`, not by trusting the passed argument.

## 7. Schema / migration

Additive only, applied by the repository `Tooba.MigrationRunner` after a private backup:

- `fulfillment 20261110160000_AddShipmentReturnOperatorAudit`
  - `shipments.created_by_user_id`, `shipments.non_delivered_by_user_id`, `shipments.return_started_by_user_id`
    (nullable uuid; historical rows keep NULL and stay readable)
  - unique index `ux_shipment_return_receipt_items_line_condition`
    (`shipment_return_receipt_id, order_line_id, condition`)

No backfill, no fabricated historical receipt/intent, no rewrite of accepted money/history/tracking.

## 8. Proof

| Proof | Result |
| --- | --- |
| `PostDispatchException258Tests` (retained 258 lifecycle) | 13/13 |
| `PostDispatchException258R1Tests` (new A/B/C/D/DTO/audit facts) | 11/11 |
| `--filter FullyQualifiedName~PostDispatchException` | 33/33 |
| `PostDispatchExceptionRelational258Tests` on REAL `tooba_alpha` (A–I) | 9/9, three recorded runs |
| Mounted operational delivery suite (`admin-order-operational-delivery.mount.test.ts`) | 37/37 (7 are TB-258-R1) |
| `tsc --noEmit` | 0 errors |
| `next build` | 0 (served BUILD_ID `UvDFu9r8dErn3rqFmyyHX`) |
| `dotnet publish` | 0 (`artifacts/publish-primary` newer than every touched backend source) |
| Full Fulfillment module | HEAD 393/412 vs base worktree 379/398 — **identical set of 19 pre-existing failures**, no re-baseline |
| Authorized read-only DOM `01a1b92d-e97e-7000-999a-143350d0edaf` | 3 groups / 3 parcels, «ثبت تحویل» + «ثبت عدم تحویل» 44px each, one action owner, 0 duplicate test ids, overflow-x 0, rtl |

Case coverage against the issued matrix (§6 A–E):

- **A** — same occurrence + same intent replays the recorded parcel after the source units were consumed (zero new
  allocation); a changed source and a changed quantity under the same key both conflict with zero new rows; a missing
  key is a typed refusal; a cancelled recorded occurrence is explicit (`G`, `C`).
- **B** — two real independent contexts and one key produce one parcel (`D`); different keys over one source cannot
  overdraw (`D`, `H`); enforcement is the aggregate + `Serializable` + DB unique index, never an in-memory double.
- **C** — two sources of one line cannot borrow (`H`); successive return/resend cycles keep the immediate link (`H`);
  a pre-dispatch cancellation releases only its own allocation once (`C`); delivered/in-transit conservation (`E`).
- **D** — `usable2 + damaged1` on one 3-unit line persists and reads back granting 2 (`I`); aggregate excess, duplicate
  condition and foreign line refused; exact replay accepted vs changed receipt `fulfillment.return.receipt_conflict`.
- **E** — mounted dialog: lines rerender preserves edits and key; one same-tick submit; uncertain outcome + remount +
  consumed capacity reconciles the EXACT saved payload; changed review blocked; viewer isolation; completion then a
  deliberately distinct next occurrence.

Fixture identities (task-labelled `TB258-*`, never deleted) are recorded verbatim in
`docs/architecture/evidence/TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R1/tb258-relational.txt`.

## 9. Runtime / environment

- FE `http://127.0.0.1:3000` PID **41812**, BUILD_ID `UvDFu9r8dErn3rqFmyyHX`.
- Host `http://127.0.0.1:5088` PID **6336** from `artifacts/publish-primary`, `/health` → `{"status":"ok"}`.
- SingleStore / `store-alpha` / `tenant-alpha` → **`tooba_alpha`** @ `127.0.0.1:5432` only.
- MigrationRunner plan: `module=Fulfillment current=20261110160000_AddShipmentReturnOperatorAudit pendingCount=0`;
  every module `pendingCount=0`.
- No disposable/test/parallel/Docker database, no `15433`, no edition/connection/port/appsettings change, no
  DataProtection or session-store write, no manual DDL/business SQL/reset/reseed/backfill.
- CapturedPreview local ON / checked-in OFF; 255 parked; live Tipax deferred (no provider credential, call or waybill).

## 10. Limits / disclosed

- No live Tipax/post waybill; non-delivery, return and origin receipt are audited internal operator facts.
- No Inventory/Returns contract write: damage stays a receipt fact without resend capacity, and no saleable-stock
  increase is emitted while the goods remain reserved for this order.
- The origin receipt is a one-time final receipt; incremental receipt is not claimed.
- Package safety is still a refusal for a child locked in an active consolidated package, not a parent-level return
  workflow (issue-4 territory); the lock was never silently released.
- Concurrency is proven with two real independent contexts plus serializable/unique guards — not a stress test.
- Unrelated suites were not run; the pre-existing 19 Fulfillment failures (historical/type-catalog migration parity
  family) are unchanged and were not re-baselined.

## 11. Ledger

- save prerequisite (256-R2): CLOSED within scope.
- issue 1 tracking uniqueness: FIXED within 257.
- issue 2 truthful remaining quantity: FIXED within 257.
- issue 3 non-delivery / return / resend: **REPAIRED by 258-R1 — pending Architect review of this result**.
- issue 4 shared-group common-or-separate parcels: PENDING 259, NOT ISSUED.
- issue 5 due workqueue / shared operational order view: PENDING 260, NOT ISSUED.

STOP_FOR_ARCHITECT — automaticNextImplementationTask: NONE.
