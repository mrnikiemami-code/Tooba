PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R2
Parent-Task: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — close operational-group execution and relational delivery proof
Mode: ONE_GROUP_WORKFLOW_HARDENED_CLIENT_ACTOR_AUTHORITY_AND_REAL_RELATIONAL_PROOF
Expected-Base-SHA: 9532d6c87045313231f0a9da06d8627679dcfe58
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

1. Decision and bounded continuation

253-R1 is PASS within its issued scope but NOT Architect ACCEPT. Its remaining gaps are:

- the operator still had to perform the accepted-group workflow as separate per-parcel and then
  package actions, with no single reviewed group step;
- client state for those actions was not proven against same-tick double submit or a lost response;
- group readiness was projected with `actorMayManage: true`, i.e. the UI could offer an operation the
  actual actor is not authorized to run;
- the disposable PostgreSQL matrix proved the authority/Fulfillment path with hand-built facts, not the
  real committed Order shipping-group evidence.

R2 closes exactly those four seams on the existing canonical path. No UI redesign, no provider
integration, no general audit, no roadmap expansion.

2. Locked product rule (unchanged)

Cross-seller operational grouping ONLY under one Marketplace sending authority, proven common origin
and compatible accepted service + chosen delivery date/hour. Seller/Store responsibilities remain
independent. Store is not a Marketplace alias. Financial seller orders and accepted shipping-group
records remain separate and immutable. One operational group may contain multiple physical parcels.

3. Scope A — one coherent group-level workflow

Add one server-owned operation `prepare_operational_group` that, for one reviewed compatible
Marketplace group:

- accepts the exact member `shippingGroupIds` plus per-line remaining packed quantities and/or the
  existing eligible parcel ids to reuse;
- verifies the requested member set is exactly one projected operational group (no foreign, partial or
  merged sets), that every member still has remaining packed quantity or an eligible unattached parcel,
  and that no member is cancelled or payment/state locked;
- derives the accepted method from committed evidence and refuses a conflicting client method, exactly
  like the accepted consolidate path (CarrierApi stays unsupported);
- creates a constituent parcel per member through the real fulfillment `CreateShipmentAsync` boundary
  with a stable per-group allocation key (`opgroup:` over checkout/group/line quantities) so retries,
  lost responses and concurrent twins never double allocate; changed quantities produce a new key;
- reuses a member's already recorded open parcel when the recorded line quantities equal the reviewed
  ones, and refuses reuse of a foreign, dispatched, unbound or already-packaged parcel;
- then creates THE common consolidated package over the constituent parcels, and on an
  already-member/duplicate refusal resolves and returns that exact existing package as `replayed`;
- returns `operationalGroupKey`, a stable `operationLocator`, the package identity/method and per-member
  parcel ids with `created`/`replayed` flags.

The projection must offer the action only when that same readiness holds, and must never surface it for
Store/Seller-owned, single-seller, CarrierApi, payment-locked or unauthorized groups.

4. Scope B — hardened client state

The admin order operational delivery section must:

- render the remaining packed items per constituent seller (title + remaining + packed) inside the group
  review so the operator reviews exact quantities instead of guessing;
- use a synchronous single-flight lock (`useRef`) so one same-tick double click produces exactly one
  mutation request, and reset it in `finally`;
- keep the reviewed quantity selection after a refusal and show the refusal inline (localized), not as a
  silent failure or a duplicate toast;
- recover from a lost response: a remount/reload reconciles the recorded parcels/package through the
  server projection instead of re-creating them.

5. Scope C — actor authority seam

Readiness must reflect the real actor. `AdminOrderDetailComposer` and the operations orchestrator must
resolve effective permissions through the existing `IOrderAdminEffectiveAccessReader`
(`order.handle` or `fulfillment.manage`) and propagate `actorMayManageOperations` to the UI. The workflow
must re-check that authority server-side; a projected ready action is not authorization.

6. Scope D — real relational proof

Replace the earlier controlled-fact matrix with a disposable PostgreSQL proof that:

- seeds committed Order shipping-group evidence through the real domain/handoff boundaries
  (`CreateFromPaidSellerOrdersAsync`) and real packed quantities (`PackSelectionsAsync`);
- reads facts/history through the real `OrderCommittedCarrierShipmentFactsReader` and
  `OrderShippingGroupHistoryReader`, maps through the real fact mapper and projects through the real
  authority;
- executes the public `MarketplaceOperationalGroupWorkflow` through the production fulfillment admin
  adapter and asserts REAL created parcel `ShippingGroupId`s, line quantities and consolidated package
  membership per member seller;
- proves retry/twin reuse without duplicate allocation, refusals for foreign selections and
  incompatible inputs, and both directions of the actor authority check.

Target identity stays the task-only disposable instance (`127.0.0.1:15433`, `order_tb253r2`,
`tb253r2`), never the shared 5432 instance and never `tooba_alpha`.

7. Out of scope

- No Live Tipax/provider call (deferred by the user).
- No schema, migration, seed, reset or direct business SQL on the canonical database.
- No change to financial seller orders or committed shipping-group history.
- No Store→Marketplace promotion and no edit of historical completed tasks.
- No visual/viewport acceptance claim; user appearance review remains the user's.

8. Validation and evidence

Focused only and honestly reported: the disposable relational cases, the admin authority/unit facts, the
mounted operational delivery tests, frontend typecheck and production build, plus the affected backend
builds. Record exact commands, counts and any pre-existing red guard unchanged.

9. Exit

Create `TASK.md`/`RESULT.md`/`RECEIPTS.md` under `docs/ai/tasks/` and
`docs/architecture/evidence/TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R2/`, update only the
authorized TOP documents, commit and push, require `HEAD == origin/main`, then post the complete Result to
Bridge and stop. Worker PASS is not Architect ACCEPT.

END_TOOBA_TASK
