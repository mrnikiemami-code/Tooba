PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R3
Parent-Task: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Status: PASS
Decision: PROVEN_ORIGIN_RESTORED_OCCURRENCE_IDENTITY_PREFLIGHT_AND_CANONICAL_DEPLOY
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 2ac19888686b668530b146c7f80702c8edbeb415
Base-SHA: 2ac19888686b668530b146c7f80702c8edbeb415
automaticNextImplementationTask: NONE

## Scope actually closed

R2 was rejected for four exact reasons. This slice corrects only those seams and deploys:

1. the unauthorized `identity="marketplace"` ownership-as-origin fallback is gone;
2. parcel occurrence identity is the prebound workflow occurrence, not a payload hash;
3. the WHOLE per-member plan is validated before the first member write (no silent filtering);
4. the corrected code is installed on the canonical FE3000 → Host5088 runtime.

## A. Proven origin restored (responsibility ≠ physical origin)

`MarketplaceOperationalDeliveryGroupAuthority.TryResolveOriginIdentity` again accepts only a proven physical
origin context or origin postal address. Owner/responsibility, tenant key, carrier city code and recipient address
are NOT origin proof.

- Missing origin → `SeparateMissingOrigin`; two groups stay independent and the group workflow refuses them with
  `order.operational_group.workflow_group_foreign`.
- Different proven origin under the same Marketplace responsibility → not one group (`SeparateIncompatibleCommitment`).
- Same proven origin → groupable.
- Store/Seller responsibilities remain separate; no Store→Marketplace promotion and no backfill of existing orders.

## B. Occurrence identity is payload-free and prebound

- `BuildMemberParcelDispatchKey(checkoutId, occurrenceId, shippingGroupId)` → `opgroup:` + SHA-256 over exactly
  `checkout | occurrence | shipping group`. No line/quantity/reuse payload is part of the key, so a deliberate
  second identical partial package can never be mistaken for a retry.
- `BuildWorkflowOperationLocator(occurrenceId)` is the slice occurrence, not a hash of the member groups.
- `AdminOrderOperationRequest.OperationalGroupOccurrenceId` is required; empty →
  `order.operational_group.workflow_occurrence_required` before any read/write.
- Same occurrence + same reviewed request → replay of the recorded parcels/package (`replayed: true`).
- Same occurrence + changed selection/reuse membership → `order.operational_group.workflow_intent_conflict`
  with zero mutation.
- A new occurrence after confirmed completion allocates a distinct parcel set from remaining packed quantity and
  keeps the first package intact.
- Recovery reads only parcels whose durable `ShippingDispatchKey` equals this occurrence key — never "all open
  parcels of the group" and never aggregate-quantity guessing. `RecordedQuantitiesMatch` is now used only as the
  replay/conflict proof over those recorded ids.
- Unresolved (package not yet persisted) → `order.operational_group.workflow_pending`; the client keeps the same
  occurrence, so the next attempt reconciles instead of minting a new occurrence or blind-resending.
- No new idempotency framework: the durable existing Fulfillment dispatch key + shipment records are the record.

## C. Whole-plan preflight before any member write

`MarketplaceOperationalGroupWorkflow.BuildPlan` now validates the complete immutable plan first:

- every requested line must belong to exactly one intended accepted member (unknown/foreign line → typed refusal);
- duplicate line selections, non-positive quantities and quantities above remaining packed quantity → refusal;
- every requested reuse id must resolve to exactly one intended member, be `Created`, undispatched, method-bound
  and not already in an active package (all supplied ids accounted for; nothing silently dropped);
- a member with neither explicit selection nor explicit reuse → refusal;
- the accepted method is derived from committed evidence and a conflicting client method is refused;
- only after all members pass does execution create parcels, then the one common package.

## D. Canonical deployment (mandatory in this task)

- Host `Release` publish → `artifacts/publish-primary` exit 0 after stopping the old Host; DataProtection keys
  preserved (`KEYS_BEFORE=True`, `KEYS_AFTER=True`).
- FE `npm run build` exit 0, then `next start -p 3000 -H 127.0.0.1`.
- Live identity: `GET /__platform-commerce` → `edition=SingleStore`,
  `deploymentId=store-alpha-canonical-tb253r3`, `tenantId=store-alpha`, `connectionReference=tenant-alpha`,
  DB `tooba_alpha`.
- CapturedPreview stays locally ON (`Enabled/SelectionEnabled/CheckoutEnabled=true`, `AvailableDateCount=7`);
  checked-in defaults untouched.
- No migration was required (no schema change); no SQL/reset/reseed was executed.

## E. Focused proof

Disposable PostgreSQL `tb253r3-pg` → `127.0.0.1:15433`, DB `order_tb253r3`, user `tb253r3` (never 5432, never
`tooba_alpha`). Real committed Order rows (checkout/seller orders/lines/shipping groups/accepted evidence), real
facts + history readers, real mapper/authority, real Fulfillment directory through the production admin adapter.

| Case | Fact |
|------|------|
| A-origin (missing) | two independent groups, `SeparateMissingOrigin`, workflow `workflow_group_foreign`, 0 shipments/packages |
| A-origin (different) | two groups, `workflow_group_foreign`, 0 shipments |
| B-replay | same occurrence twice → one parcel set, one package, second result `replayed=true`, 2 shipments/1 package |
| B-concurrency | real `Barrier` + two independent DbContexts/workflows → one package id and identical shipment set, 2 shipments/1 package/2 members |
| C-next-occurrence | first 1+1 then identical next 1+1 → two distinct packages, disjoint parcels, exactly 2 units per line; original occurrence replay returns its original package |
| D-preflight | same occurrence changed selection → `workflow_intent_conflict`; foreign line → `workflow_selection_invalid`; over-remaining later member → same, with ZERO added parcels; foreign reuse id → `workflow_shipment_foreign`; missing occurrence → `workflow_occurrence_required`; shipment count unchanged throughout |
| E-recovery | real SaveChanges interceptor fails before the common package persists → 2 occurrence-keyed parcels, 0 packages; same-occurrence retry recovers the exact recorded ids, no extra parcels, 1 package, all members `replayed` |
| refusals | unauthorized actor `order.operation.denied`; foreign group `workflow_group_foreign`; conflicting method `consolidate_incompatible`; empty selection `workflow_selection_required` |
| incompatible-slot | different committed receiving hour is not one operational group |

Immutable accepted money/evidence and financial seller orders are asserted unchanged (Paid, 100000/120000,
125000 charge, accepted snapshot/owner).

The first relational run exposed three real defects in the R3 rewrite, all fixed before the recorded green run:
the per-member binder was called without `ShippingGroupId`; replay was blocked by already-consumed remaining packed
quantity; and a facts-then-parcels read order let a concurrent twin look like a stale over-selection. After the fixes
the whole class ran green six consecutive times.

## F. Frontend facts

- The group button mints the occurrence BEFORE the mutation, persists it per checkout + operational group, and
  passes it in the request body.
- Failure (including transport throw) keeps the same occurrence and reviewed quantities; success clears it so a
  deliberate next run is a new occurrence.
- Synchronous single-flight `useRef` and `try/finally` are retained; one inline localized error.
- Mounted jsdom facts **10/10**: one request across two sellers; same-tick double click = one mutation; occurrence is
  a real GUID in the body; retry keeps the occurrence; remount after a lost response keeps occurrence + selection;
  success then next click mints a different occurrence; inline refusal retains quantities; lost-response remount
  reconciles; no authority → no actionable create; seller-owned groups offer no group workflow.

## G. Canonical authorized review (read-only, no mutation)

- Host `GET /v1/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf` (actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db`,
  dev-actor seam) → 200: 3 shipping groups, ownerKinds `Store,Store,Store`, 3 operational groups all
  `isCrossSellerMarketplace=false`, `consolidateEligible=false`.
- FE `/admin/orders/01a1b92d-…` → 200 `admin-shell`.
- Rendered authorized DOM on a fresh, sessionless task-owned Chrome profile (`D:\temp\tb253r3-chrome`, CDP 9339):
  `دسترسی مجاز نیست` absent, section present, 3 group cards, no document/section horizontal overflow at
  1440/1024/390, hit-test at the section center lands inside the section (no covering overlay), cards stacked,
  and no group action button (Store groups are correctly non-groupable).
- No pack/create/dispatch/cancel/payment/merchant edit was performed on the real order or any user data.

## HONEST LIMITS

- The FE occurrence store is scoped to `checkout + operational group` key; the panel carries no actor identity. The
  server's allocation key is `checkout + occurrence + shipping group`, and actor authority is enforced per request —
  the stored value is never authorization.
- The shared agent browser profile holds a non-admin HttpOnly session which the Host prefers over the dev-actor
  header, so the rendered review used the fresh profile above (the same dev-actor seam the Host documents). No user
  browser/profile/tab was touched or closed.
- No Live Tipax/provider call (deferred); Manual/Free only; CarrierApi still uses its dedicated path.
- No schema/migration/seed/reset/business SQL; no Store→Marketplace promotion; no change to accepted money,
  committed history or financial seller orders.
- No full solution suite and no viewport pixel-critique; user visual acceptance remains the user's.
- Pre-existing unrelated issues (249 provider concurrency, 238 unknown 22001, 227 mobile empty note, 225-R1 lost
  LOCAL edits) and TB185 paused/default-OFF are retained.

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT

## ENVELOPE
- Base: `2ac19888686b668530b146c7f80702c8edbeb415`
- Code commit: `b9596f1ae`
- Evidence envelope commit: `29afccae6781260b1b733c205407bd4c439716d9`
- `origin/main` parity verified after push (fail-closed).

