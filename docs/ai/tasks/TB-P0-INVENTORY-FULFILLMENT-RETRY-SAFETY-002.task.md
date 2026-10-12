PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-P0-INVENTORY-FULFILLMENT-RETRY-SAFETY-002
Parent-Task: TB-P0-FULFILLMENT-PACKAGE-DISPATCH-ATOMICITY-001
Program: TOOBA-FULFILLMENT-DISPATCH-INTEGRITY-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: BOUNDED_ARCHITECTURE_GATE_AND_SAFE_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: 04e3aa79f1fcf26c2c4c17f10ea1d3fce99dac91

GOAL
Close the P0 cross-module inventory-consumption replay gap left by the parent commit, without falsely claiming distributed atomicity. The preceding repair guarantees one FulfillmentDbContext SaveChangesAsync, but the Inventory module commits consumption separately and currently rejects a repeat when its reservation is Consumed. Deliver a provable safe, bounded implementation or BLOCKED_ARCHITECT_DECISION with exact evidence and zero unsafe changes.

PREFLIGHT
Verify HEAD == origin/main == Expected-Starting-HEAD; clean tracked worktree; read AGENTS.md, .cursor/skills/tooba-backend-feature/SKILL.md, applicable architecture standards/locks, parent Gate-B evidence and both module contracts. No reset, stash, force push, broad refactor or deferred findings register.

MANDATORY TRACE AND DECISION GATES
A. Trace actual ownership and persistence boundaries: FulfillmentDirectory.Packages.cs dispatch phase; ConsumeInventoryForShipmentAsync; IFulfillmentInventoryGateway; InventoryDirectory.ConsumeAsync; StockReservation.MoveTo; reservation identity and ownership/tenant checks; database constraints; outbox/transaction facilities. Verify how reservation-to-shipment identity is enforced.
B. Idempotency must NEVER mean blindly accepting any Consumed reservation. Prove that a retry is for the SAME logical consumption/fulfillment/shipment and same owner/tenant, that quantity cannot be deducted twice, and that conflicting consumers/reused IDs are rejected. Audit concurrent attempts, lost-update/unique constraints, and prior partial states. If this cannot be proven with current data/contract, STOP this scope rather than add a broad Consumed->success shortcut.
C. Model failure windows explicitly: Inventory commits then Fulfillment fails; Fulfillment succeeds but response fails; Inventory fails before Fulfillment commit; two concurrent dispatches; preexisting Dispatched member with consumed/held reservation. Verify current partial-package recovery does not silently double consume or skip needed consumption.
D. Determine the SMALLEST architecturally valid approach: idempotency key/consumption journal enforced within Inventory, safe operation-scoped retry, or proven existing outbox/saga mechanism. Never cross module domain/infrastructure boundaries; no ad-hoc shared DbContext/transaction or deceptive atomicity claim. Any schema migration, cross-module public contract change, or new business semantics need explicit decision gate. If required, return BLOCKED_ARCHITECT_DECISION with a bounded, implementable design and tests, instead of coding a half-safe fix.
E. Check whether parent change can be safely deployed before closing this gate. Give explicit HOLD/GO guidance; no live invocation.

IF ALL GATES PASS FOR A BOUNDED NON-SCHEMA CHANGE
Implement only the necessary module-owned code and focused tests. Use no any, type suppressions, unsafe casts, exception swallowing or status rewrites. Preserve tracking and package identity, existing dispatched member references, existing ownership policies, and other module isolation. Inject failures at EACH commit boundary and demonstrate retry safety, no double inventory consumption, no lost reservation, no partial Fulfillment commit. Tests must distinguish true replay from conflicting consume. Build targeted module(s), run relevant focused tests and architecture guards; avoid full suites/AMSC unless a mandatory repo policy requires them. Compare before/after diagnostics.

LIVE DATA HARD STOP
NO writes to live Host; NO dispatch/cancel/retry of order 01a12e97-ee9e-7000-826c-a9eca536b223 or package 01a12f79-6ef1-7000-8578-dd7cc841195d; NO SQL UPDATE/DELETE, migrations against a live DB, API mutations or live service restarts. No claim that the existing package is repaired without a separately approved recovery action.

EVIDENCE / OUTPUT
Add exact task receipt at docs/ai/tasks/<Task-ID>.task.md and evidence under docs/architecture/evidence/<Task-ID>/: boundary-map.md, failure-matrix.md, contract-decision.md, targeted-test-results.txt (if executed), summary.md. Report Gate A-E pass/block, exact changed file list, starting/final SHA, build/tests with explicit pass/fail, remaining P0 risk, deployment HOLD/GO, and safe runbook constraints. If implemented, commit and push main, then STOP for independent Architect review. If blocked, do NOT push speculative production changes and STOP with precise required decision. Never self-start the next task.
END_TOOBA_TASK
