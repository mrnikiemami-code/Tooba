# TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258 — RESULT

Parent: `TB-SHIPPING-TRACKING-UNIQUENESS-AND-QUANTITY-AUTHORITY-257`
Channel: `tooba-main` · Worker: `tooba-worker-01` · Agent: `cursor`
Issued base: `4a3a241d2f75e0081a8ed8a375a061a4f987694b`
Status: **PASS** within the issued scope (issue 3 only)
Decision: **POST_DISPATCH_NON_DELIVERY_PHYSICAL_RETURN_RECEIPT_AND_QUANTITY_SAFE_LINKED_RESEND**
NOT Architect ACCEPT and NOT user VISUAL/FUNCTIONAL ACCEPT.

## 0. Scope

Only the third of the five remaining defects: a dispatched parcel that the courier brings back could not be
recorded at all — the operational card offered «ثبت تحویل» and the pre-dispatch «لغو», so a physical return was
unrepresentable and no lawful resend existed. Issues 1 (tracking uniqueness) and 2 (remaining quantity) were
accepted within 257; issues 4 (shared-group common-or-separate parcels) and 5 (due workqueue / shared operational
order view) stay **NOT ISSUED**.

## 1. Transition chart (parcel-level, additive to the kept flow)

```
Created ──track──> Created ──dispatch──> Dispatched/InTransit ──deliver──> Delivered
                                             │
                                             ├── «ثبت عدم تحویل» ──> NonDelivered   (dispatch history + tracking kept)
                                             │        │
                                             │        └── later genuine deliver ──> Delivered (lawful retry-in-transit)
                                             │
                                             ├── «برگشت به مبدأ» (only from NonDelivered) ──> Returning
                                             │        │
                                             │        └── «تأیید دریافت در مبدأ» (only from Returning)
                                             │                 ──> Returned + durable ShipmentReturnReceipt
                                             │
                                             └── «ارسال مجدد» (only from Returned with a receipt and usable capacity)
                                                      ──> NEW parcel, ResendOfShipmentId = original, Created
                                                          -> existing track/dispatch/deliver path
```

Rejected transitions are typed, not silent: Delivered cannot become NonDelivered, a Created parcel cannot skip
dispatch, a return cannot start before a recorded non-delivery, a receipt is only accepted while Returning, and a
resend is refused before the original is physically received. Nothing requires a fake «تحویل» and nothing reuses
the pre-dispatch cancel path. `DispatchedAt`, tracking reference, group, method and accepted money are never erased.

Kept distinct from this flow: the existing customer **RMA after a successful delivery** (`request_return`,
`return.manage`) — unchanged, and explicitly not reachable by these new operations.

## 2. Quantity / inventory semantics (conservation)

One owner per movement, no double counting:

| Reader | Rule |
| --- | --- |
| `FulfillmentPackedQuantityAuthority.OpenAllocatedQuantity` | Created **non-resend** parcels only. A resend is funded by returned capacity, so it never shrinks the fresh packed remainder (257 rule preserved). |
| `FulfillmentPackedQuantityAuthority.RemainingPackedQuantity` | `max(0, packed − shipped − openCreatedNonResend)` unchanged. |
| `ReturnedUsableQuantity(line)` | Σ receipt items with `Condition = Usable`. Damaged receipts contribute **zero** resend capacity. |
| `ResendCommittedQuantity(line)` | Σ items of resend parcels whose status is **not** Cancelled. A cancelled pre-dispatch resend releases its own capacity exactly once; a dispatched/delivered resend keeps it consumed. |
| `ResendableQuantity(line)` | `max(0, usableReturned − resendCommitted)`. |
| `DeliveredQuantity(line)` | Σ items of **parcels in Delivered**. A delivered resend counts once; a Returned original stops counting; a dispatched-but-not-delivered parcel never counts. |
| `FulfillmentUnit.AllItemsDelivered()` | still derived from per-parcel delivery, so one other in-transit parcel keeps the whole unit not-delivered (proven in test E). |

Consequences: a non-delivery alone grants **zero** resend capacity; only physically received **usable** units are
resendable; the quantity is consumed exactly once; two operators cannot both consume the same returned units; the
original order quantities, reservations and money are untouched; no lifetime-shipped decrement, no ordered/packed
inflation, and no inventory/Returns integration write was invented (see §10 limits).

## 3. Durable facts and schema (additive, applied by the repository MigrationRunner)

Migration `20261110120000_AddShipmentNonDeliveryAndReturnReceipt` (schema `fulfillment`):

- `shipments`: `non_delivery_reason` (varchar 64, null), `non_delivery_note` (varchar 512, null),
  `non_delivered_at`, `return_started_at`, `returned_at` (timestamptz, null), `resend_of_shipment_id` (uuid, null)
  plus partial index `ix_shipments_resend_of_shipment_id`.
- `shipment_return_receipts` (`shipment_return_receipt_id`, `shipment_id`, `fulfillment_id`, `received_at`,
  `received_by_user_id`, `note`, `created_at`) with `ux_shipment_return_receipts_shipment` (unique per parcel) and
  `ix_shipment_return_receipts_fulfillment_id`.
- `shipment_return_receipt_items` (`..._item_id`, receipt id, `order_line_id`, `quantity numeric(18,6)`,
  `condition` varchar 32) with `ix_shipment_return_receipt_items_shipment_return_receipt_id`.

Historical parcels stay readable with null/default values and are **not** marked returned. The receipt collection is
deliberately unmapped on the aggregate (no phantom shadow FK): the owning `FulfillmentDirectory` persists it through
its own `DbSet`s, exactly like shipments and shipping groups.

## 4. Server authority (no raw body trust, no stale-UI authorization)

Commands/queries/validators/endpoints added under the existing owner, no parallel state machine:

- `record_non_delivery` → `AdminOrderOperationsOrchestrator.Fulfillment.cs` → `FulfillmentDirectory.RecordNonDeliveryAsync`
  (reason from a closed list, optional note, actor + time server-side; replay with the identical payload is
  idempotent, a different reason is a typed conflict).
- `start_return` → `StartReturnAsync`.
- `record_return_receipt` → `RecordReturnReceiptAsync` (exact per-line quantity + condition; the unique index is the
  exactly-once authority, a replay of the identical receipt reconciles, a different payload is
  `fulfillment.return.receipt_conflict`).
- `resend_shipment` → `CreateResendShipmentAsync` (new parcel linked to the original, `IsolationLevel.Serializable`,
  bounded attempts, `fulfillment.resend.concurrency_conflict` on a real serialization failure, and
  `shippingDispatchKey` replay returning the same parcel for a lost response).
- Capability projection (`AdminOrderOperationsOrchestrator.Projection.cs`) exposes `canRecordNonDelivery`,
  `canStartReturn`, `canRecordReturnReceipt`, `canResend` **for exactly that parcel** and only when the owning
  command would accept it; `deliver_shipment` keeps its existing authority. Every operation also passes
  `RunProjectedCoreAsync`, so a stale browser cannot authorize what the server does not project.
- Package lock: `EnsureShipmentNotLockedByPackageAsync` refuses a child inside an active consolidated package
  (`fulfillment.shipment.package_locked`) instead of letting it contradict its parent; no silent child unlock.

## 5. UI

The same operational order section (`admin-order-operational-delivery-section.tsx`) and the same parcel card remain
the single action owner; the new flow lives in `features/order-operational-groups/ui/post-dispatch-exception-dialog.tsx`
(public surface through the feature `index.ts`, no `app/admin` import):

- a dispatched parcel renders a concise «ثبت عدم تحویل» beside the lawful «ثبت تحویل» (44px targets);
- one short dialog per intent — reason select + useful note; receipt dialog with exact line quantity and
  `Usable`/`Damaged` condition; resend dialog bounded by the receivable quantity with the accepted method and
  recipient shown read-only;
- retained input, synchronous single-flight submit, **one** inline error (no duplicate toast), and an explicit
  uncertain-outcome state that replays the same dispatch key instead of minting a second parcel;
- no raw enum/code/tenant text, no finance/technical paragraph, no checkbox clutter outside the purposeful dialog.

## 6. Real provider vs internal facts

Non-delivery, return and origin receipt are **audited internal operator facts**. No CarrierApi/Tipax call, no fake
provider cancellation, no label, no fabricated carrier status, and no live provider credential was used. Resend
creates a normal internal parcel through the existing owner, so a later provider dispatch would use the already
existing dedicated provider path — untouched here.

## 7. Runtime identity (canonical, one database)

- Source `D:\Users\User\source\repos\SarvNewVer`, FE `http://127.0.0.1:3000` (PID **10304**, served BUILD_ID
  `8ck4YFJYTlUDkk9CZp87c`) → Host `http://127.0.0.1:5088` (PID **5184**, `artifacts/publish-primary`, `/health` 200).
- Edition SingleStore, tenant `store-alpha`, connection `tenant-alpha`, database **`tooba_alpha` @ 127.0.0.1:5432**
  only. No disposable/test/parallel/Docker database, no port 15433.
- Migration state read from the repository runner: `module=Fulfillment database=tooba_alpha
  current=20261110120000_AddShipmentNonDeliveryAndReturnReceipt pendingCount=0`; every other module `pendingCount=0`.
- Migration applied after a private backup `D:\temp\tb258-tooba_alpha.dump`; no manual DDL, no business SQL, no
  reset/reseed/backfill. DataProtection preserved; CapturedPreview local ON / checked-in OFF; 255 parked; Tipax
  deferred.

## 8. Proof (see RECEIPTS for exact commands and outputs)

- Pure domain/application: `PostDispatchException258Tests` **13/13**.
- REAL storage on `tooba_alpha`: `PostDispatchExceptionRelational258Tests` **6/6** (A–F), run twice with distinct
  task-labelled fixtures (`TB258-A…F`), file `tb258-relational.txt`.
- Host admin routing/authority seam: `AdminOrderFulfillmentOperationsTests` **6/6**.
- Mounted frontend: `admin-order-operational-delivery.mount.test.ts` **33/33** (6 are TB-258: static projection-only
  action, returned parcel with usable capacity, non-delivery payload + kept history, per-line receipt
  quantity+condition, bounded resend + stable key replay, refused exception → one inline error).
- `tsc --noEmit` 0; production build 0; `dotnet publish` 0.
- Authorized read-only DOM on the existing review order `01a1b92d-e97e-7000-999a-143350d0edaf` (fresh-profile
  headless Chrome, no click, no screenshot, no mutation): 3 groups / 3 parcels, every shipped parcel exposes
  «ثبت تحویل» **and** the new «ثبت عدم تحویل» at 44px, no return/resend exit before a recorded return, 0 horizontal
  overflow, `dir=rtl`. Capture: `dom/tb258-dom.json`.

## 9. Ledger

- save prerequisite (256-R2): CLOSED within scope.
- issue 1 automatic tracking: FIXED within 257.
- issue 2 truthful remaining quantity: FIXED within 257.
- issue 3 non-delivery / physical return / resend: **IMPLEMENTED — pending Architect review** (this result).
- issue 4 shared-group common-or-separate parcels: PENDING 259, NOT ISSUED.
- issue 5 due workqueue / shared operational order view: PENDING 260, NOT ISSUED.

## 10. Limits (disclosed, not hidden)

- No live Tipax/post waybill and no provider cancellation or return state; the carrier side remains internal truth.
- No Inventory/Returns contract write: damaged-vs-usable treatment is recorded on the receipt and drives resend
  capacity, but no saleable-stock increase and no warehouse/Returns movement is emitted, because the goods stay
  reserved for this order. A future saleable-stock decision belongs to the Inventory/Returns owner.
- Package safety is a **refusal** for a child locked in an active consolidated package; a parent-level return
  workflow for every package member was not built (issue 4 territory) and is disclosed as a limit rather than
  silently unlocking children.
- Concurrency is proven with two real independent contexts and the database unique/serializable guards, not a
  stress test. Fixtures created for this task remain in `tooba_alpha` (task-labelled, never deleted).
- Unrelated suites were not run; pre-existing unrelated reds are not re-baselined.

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
