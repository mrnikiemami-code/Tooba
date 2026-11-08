# RESULT — TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R4

Task: `TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R4`
Parent: `TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R3`
Mode: `BIND_COMPLETE_OCCURRENCE_INTENT_AND_RESTORE_EXPLICIT_REUSE_ONLY_EXECUTION`
Status: **PASS within the issued R4 correction/deploy scope**
Decision: **IMMUTABLE_OCCURRENCE_ENVELOPE_EXPLICIT_REUSE_ONLY_AND_PREBOUND_REQUEST_PRESERVATION_DEPLOYED**
Not Architect ACCEPT. Not Visual ACCEPT. Not live-provider acceptance.

## 1. Base / code / evidence

| Item | Value |
| --- | --- |
| Expected base SHA | `8617161188232acfc4f70d6e363b906e85e14953` |
| Code commit | stamped in `RECEIPTS.md` |
| Evidence commit | stamped in `RECEIPTS.md` |
| Repo / branch | `SarvNewVer` / `main`, local == `origin/main` |
| Canonical URL | `http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf` (read-only) |
| Edition / DB | SingleStore / `store-alpha` / `tooba_alpha` |

## 2. Defect A — one complete immutable intent record, claimed before any allocation

- Additive durable store: `order.order_operational_group_occurrences` +
  `order.order_operational_group_occurrence_members`
  (migration `20261108153000_AddOperationalGroupOccurrences`, applied to `tooba_alpha`).
- The envelope carries authority scope: occurrence id, checkout, actor user id, operational group key,
  accepted method code, and per-member `{ shippingGroupId, ordered selections(lineId, quantity), reuseShipmentIds }`.
- `OrderOperationalGroupOccurrenceStore.ClaimAsync` inserts the canonical envelope **before the first
  Fulfillment write**. A concurrent twin that loses the unique-PK race detaches and reloads the winner's row,
  so "allocate once" is a storage invariant rather than a timing coincidence.
- Same occurrence + different checkout/actor/group-key/envelope fingerprint → typed
  `order.operational_group.workflow_intent_conflict` **before** any new write, including a change limited to a
  member that was never allocated and a change limited to the reuse set.
- Occurrence identity stays distinct from payload fingerprint: an identical **next** occurrence is a distinct
  occurrence and allocates a distinct parcel set.
- Member allocations are appended as durable links immediately after each allocation; the common package and
  method are recorded on completion. A completed occurrence is answered from its own durable links
  (`ReplayCompletedAsync`) with no remaining-quantity gate and no new allocation.
- A brand-new occurrence is proved to fit (`BuildPlan` on a synthetic probe record) before claiming, so a typed
  refusal leaves no claimed row behind. An existing occurrence is never re-validated against remaining quantity.

## 3. Defect B — explicit reuse-only execution

- `MemberPlanKind` is now explicit: `Create`, `ReuseOnly`, `Replay`.
- Reuse-only members never call `AcceptedGroupShipmentCreateBinder` (the R2/R3 defect where empty selections were
  converted to an invented/default selection is removed) and create zero parcels.
- Reuse ownership is resolved by the shipment's own `ShippingGroupId`, not `sellerOrderId` alone; a parcel that
  belongs to a sibling accepted group of the same seller is `workflow_shipment_foreign`.
- An already-packaged reuse parcel is refused unless it is the recorded reuse of this exact occurrence
  (reconciliation path).
- The whole plan, including create/reuse conflicts, is validated before the first write
  (unknown/duplicate/foreign/over-quantity members are typed refusals with zero partial writes).
- A parcel already written under this occurrence's dispatch key but not yet linked (crash between the durable
  create and the durable member link) is reconciled by exact key + exact quantities, never re-created.

## 4. Defect C — preserve the reviewed client request through uncertainty

- `operational-group-occurrence-store.ts` now persists the **complete** attempt
  `{ occurrenceId, checkoutId, operationalGroupKey, scopeKey, fingerprint, members, selections,
  reuseShipmentIds, attempts, lastFailedAtUtc, lastErrorCode }` under
  `tb.operational-group-attempt.v2:{checkoutId}:{scopeKey}:{operationalGroupKey}`.
- The complete envelope is written **before the first POST**; on network/5xx/cancellation the same envelope and
  occurrence are retained and no new occurrence is minted.
- The mounted card exposes an explicit reconcile block that replays the retained envelope verbatim, plus a
  distinct "new operation" path only after authoritative resolution; reconcile stays available even when create
  readiness is false (partial server progress + lost response).
- Viewer scope is derived from `ADMIN_ACTOR_STORAGE_KEY`; one viewer's hint is never inherited by another.
- Storage unavailability falls back to in-memory state plus authoritative server reconciliation (no blind remint).
- Existing synchronous single-flight `useRef` guard, `catch/finally`, one inline localized error and compact
  layout are preserved.

## 5. Executed proof

Disposable PostgreSQL (`docker` container `tb253r4-pg`, port `15433`), real Order domain/history/facts +
production Fulfillment adapter:

| Suite | Result |
| --- | --- |
| `Category=TB253R4` relational (A reuse-only, B sibling-group foreign, C crash+conflict+retry, D preflight zero writes, E barrier concurrency on the claim path, F mixed create+reuse replay) | **6/6** |
| `Category=TB253R3` + `Category=TB253R4` (no regression of the R3 matrix, separate) | **15/15** |
| Operational delivery authority/workflow subset (`MarketplaceOperational*` + `OperationalDeliveryGroup*`) | **32/32** |
| Frontend mounted/workflow jsdom (`admin-order-operational-delivery.mount.test.ts`, incl. 5 R4 cases) | **15/15** |
| Frontend `tsc --noEmit` | exit **0** |
| Frontend production build | exit **0**, `BUILD_ID` `Pa2w5BmMJCuC-79qEVcv_` |

Case E is a real two-`DbContext` barrier: one allocation set, one common package, and the durable occurrence row
completes with exactly two member links.

Case F reuses one real pre-existing parcel and creates exactly one new parcel; the replay returns the same package
and identical ids without a third parcel.

## 6. Canonical deployment ledger

| Item | Value |
| --- | --- |
| Host publish | `dotnet publish ... -c Release -o artifacts\publish-primary`, exit **0** (16:32 local) |
| Host process | PID **29240**, `http://127.0.0.1:5088`, `/health` **200** |
| Frontend rebuild | `npm run typecheck` 0, `npm run build` 0, `BUILD_ID` `Pa2w5BmMJCuC-79qEVcv_` |
| Frontend process | PID **41724**, `http://127.0.0.1:3000`, `/fa` **200** |
| Migration | `20261108153000_AddOperationalGroupOccurrences` applied to `tooba_alpha`; both tables present; **0** rows |
| Private backup | `D:\temp\tb253r4-backup\tooba_alpha-pre-tb253r4.dump` (2.77 MB) before migration |
| DataProtection | `artifacts\publish-primary\App_Data\dataprotection-keys` preserved (keys before/after = True) |
| Local CapturedPreview | Enabled / SelectionEnabled / CheckoutEnabled / AvailableDateCount=7 (checked-in OFF) |
| Read-only admin order | `01a1b92d-e97e-7000-999a-143350d0edaf` → 200, **3 Store groups**, no Marketplace claim, no mutation |

No business SQL, no reset/reseed, no accepted-history rewrite, no manual DDL.

## 7. Honest limitations

- Tipax / live carrier acceptance remains deferred by user instruction; no provider credential was requested.
- No real user order/cart/pack/create/dispatch/cancel/payment/merchant edit was executed on the canonical DB;
  the workflow proof ran on the disposable PostgreSQL with production domain/application code.
- Store/Seller owners still fail closed and were not promoted to Marketplace; origin vs responsibility remains
  distinct.
- The R3 occurrence-next-package proof was executed once in the combined 15/15 run, not repeatedly.
- No screenshots and no visual-acceptance claim; only read-only authorized DOM/HTTP checks.
- TB185 remains paused/default-OFF/PendingAllocation; retained known issues (238 unknown 22001, 227 mobile
  empty-note, 225-R1 lost local edits) are unchanged by this slice.

## 8. State

`STOP_FOR_ARCHITECT`
`automaticNextImplementationTask: NONE`
