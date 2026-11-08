PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R5
Parent-Task: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R4
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Status: PASS
Decision: UNRESOLVED_REMINT_BLOCKED_AT_STATE_BOUNDARY_AND_RECORDED_OCCURRENCE_PARCEL_RECONCILED_BEFORE_REMAINING_GATE
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 1dfdecbaf1365c21fbea86ab13922f63b358b74d
Base-SHA: 1dfdecbaf1365c21fbea86ab13922f63b358b74d
Code-SHA: 4042e9d95129a1a0f0c40ebb6ed103412377c60e
Evidence-SHA: 7bafc2341d005de52be1ff7a4328a5846477e784
automaticNextImplementationTask: NONE

# RESULT — TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R5

Status: **PASS within the issued R5 correction/deploy scope**
Not Architect ACCEPT. Not Visual ACCEPT. Not live-provider acceptance.

## 1. Base / code / evidence

| Item | Value |
| --- | --- |
| Expected base SHA | `1dfdecbaf1365c21fbea86ab13922f63b358b74d` |
| Code commit | stamped in `RECEIPTS.md` |
| Evidence commit | stamped in `RECEIPTS.md` |
| Repo / branch | `SarvNewVer` / `main`, local == `origin/main` |
| Canonical URL | `http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf` (read-only) |
| Edition / DB | SingleStore / `store-alpha` / `tooba_alpha` |

## 2. Defect A — no second operation while an attempt is unresolved

- `resolveOperationalGroupAttempt` no longer mints on a changed review. A retained attempt is treated as
  unresolved (only an authoritative completion clears it), so:
  - identical review → resume THAT occurrence (attempts counter increments, envelope untouched);
  - changed review → `blockedByRetainedAttempt: true`, the recorded occurrence and the recorded request are
    returned UNCHANGED, and nothing is written (no mint, no overwrite);
  - no retained attempt → freshly minted occurrence.
- The section computes `retainedBlocksNewOperation` from the current review fingerprint and DISABLES the normal
  Create path, with one compact localized notice. The same guard runs inside the click handler, and the
  resolver is the state boundary — a caller cannot bypass it by using the normal path.
- Explicit safe transition only for a proven no-claim refusal: `releaseOperationalGroupAttemptAfterNoClaimRefusal`
  accepts only `workflow_occurrence_required`, `workflow_group_foreign`, `consolidate_requires_multi_seller`,
  `consolidate_incompatible`, `order.operation.denied` — codes thrown before the durable claim. A network
  failure carries no server code on purpose (`AdminResult.code` stays null for transport errors), and 5xx/timeout
  are never in the set. `workflow_selection_invalid`, `workflow_selection_required` and
  `workflow_shipment_foreign` are deliberately excluded because the same codes can be produced after the claim.
- Authoritative completion clears the attempt, so the next deliberate run is a genuinely new occurrence.
- A viewer/scope change clears the in-memory ref and reconcile hint before reloading; local hints are never
  authorization (storage remains keyed by checkout + viewer scope + group).
- Single-flight `useRef`, exact quantities, one inline error and the compact layout are preserved; no generic
  reset/delete control was added.

## 3. Defect B — recorded occurrence parcel reconciled before the remaining-quantity gate

- `BuildPlan` now inspects the exact occurrence DispatchKey for a member that has NO durable member link
  **before** the remaining-quantity check and the create binder.
- The exact recorded parcels are accepted only when their own `ShippingGroupId` is the member group, they are
  non-cancelled, their method matches the immutable envelope's accepted method, and their per-line quantities
  equal the durable envelope (`RecordedParcelEvidenceMatches`). They are then planned as
  `MemberPlanKind.ReconcileRecorded` with those exact ids.
- Execution links exactly those ids through the existing durable member-allocation port — no sweep of open
  parcels, no replacement parcel, no new binder call.
- Mismatched evidence is `workflow_intent_conflict` with zero new allocation.
- The whole plan is still validated before any write; true first allocations keep the remaining-quantity gate
  and the existing binding/metadata rules.
- Distinct next occurrence, replay, actor authority, financial history and explicit reuse semantics are
  unchanged.

## 4. Executed proof

Disposable PostgreSQL (`docker` container `tb253r5-pg`, port `15433`, fresh), real Order domain/history/facts +
production Fulfillment adapter.

| Suite | Result |
| --- | --- |
| `Category=TB253R5` relational (G create-before-member-link recovery, H evidence mismatch) | **2/2** |
| Adjacent `Category=TB253R4` relational (no regression of the R4 matrix) | **6/6** |
| Adjacent authority/workflow subset (`MarketplaceOperational*` + `OperationalDeliveryGroup*`) | **34/34** |
| Frontend mounted/workflow jsdom (`admin-order-operational-delivery.mount.test.ts`) | **17/17** |
| Frontend `tsc --noEmit` | exit **0** |
| Frontend production build | exit **0**, `BUILD_ID` `PJOIKkpv60txqyyb_oqfI` |

Case G uses a real `SaveChangesInterceptor` on the Order context that fails EXACTLY on the first
`OrderOperationalGroupOccurrenceMember` insert — i.e. after the Fulfillment parcel create committed and before
the member link persisted. It asserts one occurrence-keyed parcel, zero member links, zero packages at failure;
then the retry recovers the exact id, creates only the missing member, and produces exactly one package and two
links. A changed request under the same occurrence is a typed conflict and adds zero rows.

Case H seeds a parcel under the occurrence key with the wrong quantity and proves the plan fails closed with
`workflow_intent_conflict`, no package, no replacement and no occurrence row.

## 5. Canonical deployment ledger

| Item | Value |
| --- | --- |
| Host publish | `dotnet publish ... -c Release -o artifacts\publish-primary`, exit **0** |
| Host process | PID **17344**, `http://127.0.0.1:5088`, `/health` **200** |
| Frontend rebuild | `npm run typecheck` 0, `npm run build` 0, `BUILD_ID` `PJOIKkpv60txqyyb_oqfI` |
| Frontend process | PID **7960**, `http://127.0.0.1:3000`, `/fa` **200** |
| Migration | `20261108153000_AddOperationalGroupOccurrences` already applied; **no new migration** |
| DataProtection | `artifacts\publish-primary\App_Data\dataprotection-keys` preserved |
| Read-only admin order | `01a1b92d-e97e-7000-999a-143350d0edaf` → 200, 3 shipping groups, 3 Store groups, no Marketplace claim |

No business SQL, no reset/reseed, no accepted-history rewrite, no manual DDL, no user-order/cart/pack/create/
dispatch/cancel/payment/merchant write on the canonical database.

## 6. Honest limitations

- Tipax / live carrier acceptance remains deferred by user instruction; no provider credential was requested.
- The canonical runtime was rebuilt/restarted but no real order workflow was executed against the canonical
  DB; all workflow proof ran on the disposable PostgreSQL with production domain/application code.
- Store/Seller owners still fail closed and were not promoted to Marketplace.
- The safe-transition code list is intentionally narrow; a refusal outside it keeps the retained envelope and
  requires reconcile, which is the conservative direction.
- No screenshots and no visual-acceptance claim; only read-only authorized HTTP checks.
- TB185 remains paused/default-OFF/PendingAllocation; retained known issues (238 unknown 22001, 227 mobile
  empty-note, 225-R1 lost local edits) are unchanged by this slice.

## 7. State

`STOP_FOR_ARCHITECT`
`automaticNextImplementationTask: NONE`
END_TOOBA_WORKER_RESULT
