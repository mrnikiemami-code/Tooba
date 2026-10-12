PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003
Parent-Task: TB-P0-INVENTORY-FULFILLMENT-RETRY-SAFETY-002
Program: TOOBA-FULFILLMENT-DISPATCH-INTEGRITY-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_INVENTORY_ATOMIC_CONSUME_WITH_STOP_GATES
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: fa2c9209ec2d03216e823cf4e2c069178df868c4

ARCHITECT DECISION
Approve design E IN PRINCIPLE: an Inventory-owned reservation-consumption journal, state-guarded exactly-once stock deduction and reservation transition in one local Inventory DB transaction, and replay detection tied to a stable consumer identity. This is permission to implement reviewed code and repository migration, NOT permission to execute migrations against live servers, mutate live data, or deploy. Reject blanket Consumed=>success and unguarded ExecuteUpdateAsync. Full cross-module atomicity is NOT thereby guaranteed.

PREFLIGHT
Verify HEAD == origin/main == expected SHA and tracked tree clean. Read AGENTS.md, backend feature skill, architecture standard, parent task and evidence (boundary-map.md, failure-matrix.md, contract-decision.md), InventoryDirectory.cs, InventoryDbContext.cs, StockReservation and StockPosition, FulfillmentInventoryGateway and IFulfillmentInventoryLifecyclePort, migration conventions, existing inventory module tests and architecture guards. Do not open or modify deferred findings register. STOP on divergence or active conflicting edits.

BOUNDED IMPLEMENTATION — INVENTORY OWNERSHIP ONLY

Create minimal Inventory-owned durable consumption journal with ReservationId unique/PK, StockItemId, exact Quantity representation, stable ConsumerKey (non-null for the protected path), ConsumedAt; define explicit proper migration in Inventory's migration convention. Examine existing historical Consumed reservations: do NOT backfill provenance, invent a consumer key, or treat a missing journal row as proven replay. Existing legacy callers cannot be silently changed to fake ownership.
Implement a single real local Inventory transaction covering state-guarded reservation consume, stock position deduction, consumption journal insert and related Inventory outbox changes. Respect existing domain transitions and events; avoid bulk updates that bypass optimistic concurrency or domain invariants. Validate quantities, identity and status before write. Ensure racing consumers are serialized by durable constraints/CAS and handle transaction race/retry without double deduction. If invariant cannot be demonstrated with actual ORM/database behavior, STOP and report the precise decision gap.
Add consumer-key-aware API/port ONLY with a sound compatibility story for all existing callers. Same (reservationId, consumerKey) with a matching durable journal may succeed without new stock deduction; different key, missing journal, quantity or stock-item mismatch must fail with a typed, mapped domain error. Legacy unkeyed behavior must remain strict, NOT blanket-success. Follow existing module ownership and Contracts-only boundaries. If extending the public port safely requires broad unrelated refactors, STOP and provide the call-site impact instead of an unsafe compatibility overload.
Do NOT change Fulfillment dispatch orchestration, W6 skip logic, existing live packages, global tracking index, UI, backend authentication, tenant isolation, order state, or any unrelated architecture. This task is Inventory atomic-consumption foundation only. A separate follow-up will wire a canonical stable Fulfillment consumer key and address W6.

STOP GATES

If current Inventory schema or EF transaction semantics cannot atomically encompass ExecuteUpdate/row updates, journal insertion, reservation status, and outbox under ONE physical transaction, BLOCKED_ARCHITECT_DECISION with evidence; do not fake atomicity.
If the migration may destroy or rewrite historical Consumed rows, or requires unapproved backfill, STOP.
If concurrency/exact-once guarantees cannot be proven with focused integration tests on the supported DB, STOP and distinguish implemented from verified; never claim success on only an in-memory fake.
If existing callers cannot be preserved without allowing ambiguous replay, STOP for a compatible contract decision.

REQUIRED TESTS (focused; no broad sweep)

Same reservation+same consumer replay => one stock deduction, one journal row.
Conflicting consumer => typed conflict, no second deduction.
Consumed legacy row without journal => typed conflict (no synthetic success).
Failure between deduction and reservation/journal commit => rollback ALL Inventory writes, including outbox.
Concurrent consumers => one effective deduction with DB-enforced conflict/replay behavior.
Not-Held, Released, reservation mismatch and insufficient stock retain correct domain errors.
Run existing focused Inventory and cross-module port/guard suites; report actual pass/fail, no weakening tests or guards.

EVIDENCE AND HANDOFF
One bounded commit/push to origin/main ONLY if gates pass; code, new migration, tests, exact task receipt in docs/ai/tasks/<Task-ID>.task.md, evidence in docs/architecture/evidence/<Task-ID>/ (design, files changed, pre/post invariants, focused raw logs, proof of SQL transaction/unique constraints, migration safety, risks). If blocked, docs-only decision report is acceptable. No git reset/stash/force-push. Never invoke live dispatch, cancellation, migrations, SQL UPDATE/DELETE, service restart or deploy. Explicit STOP_FOR_ARCHITECT_INDEPENDENT_ACCEPTANCE at end; do not self-start next task.
END_TOOBA_TASK
