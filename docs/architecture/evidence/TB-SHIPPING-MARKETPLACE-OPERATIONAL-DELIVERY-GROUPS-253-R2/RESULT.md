PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R2
Parent-Task: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Status: PASS
Decision: ONE_GROUP_WORKFLOW_ACTOR_AUTHORITY_AND_REAL_RELATIONAL_MATRIX_DEPLOYED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 9532d6c87045313231f0a9da06d8627679dcfe58
Base-SHA: 9532d6c87045313231f0a9da06d8627679dcfe58
Code-SHA: 8580097d696f5b1fd0f9c8ed9382dea2c912eccc
Evidence-SHA: PENDING_DOCS_COMMIT
automaticNextImplementationTask: NONE

## Architect context
253-R1 closed compatibility/readiness/consolidate/UI but left four seams: no single reviewed group step, unproven
client concurrency/lost-response, `actorMayManage: true` projection, and a controlled-fact (not real committed
Order evidence) relational matrix. R2 closes exactly those seams and nothing else.

## LANDED

### A. One server-owned group workflow
- New `prepare_operational_group` operation (endpoint → MediatR `PrepareOperationalGroupCommand` →
  `AdminOrderOperationsOrchestrator.PrepareOperationalGroupAsync` → `MarketplaceOperationalGroupWorkflow`).
- Accepts exact member `operationalGroupShippingGroupIds` + reviewed per-line quantities + optional reusable
  `reuseShipmentIds`; the requested set must be EXACTLY one projected operational group, otherwise
  `order.operational_group.workflow_foreign`.
- Per member: refuses cancelled/payment-locked, refuses a foreign/dispatched/unbound/already-packaged reuse parcel
  (`workflow_shipment_foreign`), refuses a member with neither remaining packed quantity nor usable parcel
  (`workflow_selection_required`), derives the accepted method from committed evidence and refuses a conflicting
  client method (CarrierApi still unsupported).
- Creates each constituent parcel through the real `IFulfillmentAdminOperations.CreateShipmentAsync` with a stable
  `opgroup:` allocation key over checkout/group/line-quantities, then creates THE common consolidated package; an
  already-member/duplicate refusal resolves the exact existing package and returns `replayed`.
- Returns `operationalGroupKey`, stable `operationLocator`, package id/number/method, per-member parcel ids with
  `created`/`replayed`.
- Retry/lost-response/twin: a recorded open parcel whose aggregated line quantities equal the reviewed ones is
  reused instead of re-allocated (this closed the `workflow_selection_required` false refusal after a lost response).
- Projection offers the action only through `WithGroupWorkflowReadiness` (Marketplace + ≥2 sellers + no CarrierApi +
  no payment lock + real actor authority + every member creatable or reuse-eligible).

### B. Corrected Marketplace origin identity
- `TryResolveOriginIdentity`: a Marketplace-owned group's persisted owner identity IS the committed dispatch origin.
  A real Marketplace Manual group carries no pickup location and must never copy the buyer/recipient into one; the
  R1 location-only rule made every such group ungroupable. Store/Seller keep the strict location/address requirement
  and still return `missing_origin` / `seller_responsibility` fail-closed.
- Two authority assertions that encoded the superseded location-only rule were re-expressed for the corrected rule
  (`Marketplace_owner_identity_is_a_proven_origin_without_a_location`, `Store_missing_origin_still_fails_closed`).
  No fail-closed assertion removed, no skip.

### C. Hardened client state
- Admin order operational delivery section reviews remaining packed items per constituent seller
  (`title`, `remainingPackedQuantity`, `packedQuantity` from the server projection) instead of guessing.
- Synchronous single-flight `useRef` lock (set before the async call, released in `finally`) → one same-tick
  double click = exactly one mutation.
- Known refusal renders inline and localized; reviewed quantities are retained; no duplicate toast.
- Lost response: inline error; a remount reconciles the recorded parcels/package from the server projection.

### D. Real actor authority
- `AdminOrderDetailComposer` now resolves `IOrderAdminEffectiveAccessReader` for the real viewer
  (`order.handle` or `fulfillment.manage`) and emits `actorMayManageOperations`; the UI hides the actionable create
  for an unauthorized actor.
- The workflow re-checks the same authority server-side and throws `order.operation.denied`; a projected ready
  action is never authorization.

### E. Real relational proof
- New `MarketplaceOperationalGroupWorkflowRelationalTests` on the task-owned disposable
  `127.0.0.1:15433/order_tb253r2` (`tb253r2`): seeds committed Order shipping-group evidence through
  `CreateFromPaidSellerOrdersAsync` + real `PackSelectionsAsync`, reads facts/history through the production
  readers, maps via the shared `AdminOrderOperationalFacts`, projects via the authority, and executes the public
  workflow through the production Fulfillment admin adapter. Asserts real parcel ids, per-line quantities and
  package membership per member seller, retry/twin reuse, refusals, and both directions of the actor check.

## PROOF
- Disposable PostgreSQL TB253R2: **4/4** (`group-workflow`, `retry-reuse`, `refusals`, `incompatible-slot`).
- Authority + workflow facts: **17/17** (includes the 4 relational cases when the task env var is set).
- Admin section mount/static: **8/8**; `tsc --noEmit` exit 0; `npm run build` success.
- Backend compile of Order Application/Endpoints/Infrastructure + Fulfillment Infrastructure + tests: 0 errors.

## HONEST LIMITS
- No Live Tipax/provider call (deferred by the user); Manual/Free remain.
- No schema/migration/seed/reset/business SQL; no change to financial seller orders or committed history; no
  Store→Marketplace promotion.
- No canonical FE3000/Host5088 rebuild/redeploy in this slice (no runtime asset was required by the issued scope);
  the changed backend/FE ship with the next deployment.
- No visual/viewport acceptance and no full-suite claim; pre-existing unrelated failures are not re-baselined.
- The Marketplace-origin correction is a deliberate product-rule repair required for real Marketplace Manual groups
  to be groupable; it is disclosed here rather than hidden behind a green matrix.

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT
