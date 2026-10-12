# TB-P0-FULFILLMENT-KEYED-CONSUME-WIRING-004 — persisted claim artifact

Persisted claim artifact — full Architect body received via Bridge
`GET /api/tasks/next?channelId=tooba-main&workerId=tooba-worker-01&agentType=cursor`
at Bridge task UUID `6d55ed2f-dec1-4b23-9cc3-840e5ddcd4a5` (createdAtUtc 2026-10-12T13:37:55.4517883Z).

Receipt: executed in `IMPLEMENT_ONE_BOUNDED_CROSS_MODULE_WIRING_WITH_STOP_GATES` mode; only the
consolidated-package dispatch consumption path was routed to Inventory's keyed `ConsumeAsync`;
Inventory journal/migration/schema untouched. Evidence in
`docs/architecture/evidence/TB-P0-FULFILLMENT-KEYED-CONSUME-WIRING-004/`.

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-P0-FULFILLMENT-KEYED-CONSUME-WIRING-004
Parent-Task: TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003
Program: TOOBA-FULFILLMENT-DISPATCH-INTEGRITY-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_ONE_BOUNDED_CROSS_MODULE_WIRING_WITH_STOP_GATES
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: e7d0bf978fcf8b3f90bd0300474ab23e486d3289

OBJECTIVE
Connect consolidated-package shipment reservation consumption to Inventory's NEW keyed ConsumeAsync(reservationId, consumerKey, cancellationToken), without changing Inventory's journal or migration. One narrow production behavior: stable logical identity for each package/member/order-line reservation, so safe replay across a Fulfillment commit failure. DO NOT claim global cross-module transaction or resolve W6 in this task.

PREFLIGHT (STOP if not met)

HEAD==origin/main==Expected-Starting-HEAD; clean tracked task paths. Read AGENTS.md, .cursor/skills/tooba-backend-feature/SKILL.md, existing Gate-B and Journal-003 architecture evidence, and EXACT caller/port/adapter/source types.
Inventory migration from Journal-003 remains UNDEPLOYED in live Host; absolutely NO live Host write, deploy, migration execution, restart, direct SQL, dispatch or cancel. Changes only code/tests/receipt/evidence in git.

BOUNDED IMPLEMENTATION
A. Trace FulfillmentDirectory.ConsumeInventoryForShipmentAsync, FulfillmentInventoryGateway and IFulfillmentInventoryLifecyclePort. Establish which stable IDs are actually available: packageId, shipmentId, reservationId, orderLineId. A consumerKey must be deterministic on retry, <=128 characters, tied to an unambiguous single reservation and same logical consumer. Prefer compact stable GUID identities; if an order-line ID is not available, only proceed if reservationId+shipmentId+packageId are proven sufficient for unique consumption ownership. DO NOT invent identifiers, depend on time/random, or hash without documented collision rationale.
B. Route ONLY consolidated-package dispatch consumption to the keyed Inventory API. Preserve existing unkeyed semantics for other flows; do not silently change individual shipment dispatch, returns, cancellation, reservation release or order state. Avoid changed contract beyond Journal-003's existing additive overload.
C. Preserve two-phase package validation and one Fulfillment SaveChangesAsync from parent 04e3aa79. Do not mutate pre-dispatched members, do not bypass held/consumed reconciliation W6, and do not alter tracking generation. Ensure existing gateway and other callers compile.
D. If stable consumer identity cannot be proven from actual model or requires substantial refactor/migration/API change: STOP as BLOCKED_ARCHITECT_DECISION; commit only evidence, no half-implemented feature.

PROOF / TEST BUDGET (STRICT; NO LOOP)

One focused compile of impacted backend project(s), one focused test command covering key forwarding/stability and error propagation; add or amend at most two targeted tests, preferably using existing test fixture. NO repository-wide tests, AMSC, full Host suite, Testcontainers spin-up if ordinary narrow unit proof is adequate, frontend build, browsers or exploratory test retries.
Run each verification command AT MOST ONCE. If environment/baseline failure occurs, record it; do NOT debug/re-run unrelated failures.
Hard scope cap: at most 3 production source files plus up to 2 test files. No other module's production code except necessary narrow Fulfillment-owned adapter/caller; never edit Inventory journal, migration or shared schema.
Work budget: one linear investigation and implementation pass, target <=20 minutes; if not provable/complete in that budget, STOP, report partial evidence, no speculative widening or indefinite loops. Do not consume time trying to make unrelated tests green.

EVIDENCE / REPORT / STOP

Explain exact consumerKey format, provenance and stable retry behavior; show diff, test output and any unresolved gaps.
Add docs/ai/tasks/TB-P0-FULFILLMENT-KEYED-CONSUME-WIRING-004.task.md receipt and minimal evidence in docs/architecture/evidence/TB-P0-FULFILLMENT-KEYED-CONSUME-WIRING-004/ (summary.md and tests.txt only unless needed).
Commit and push origin/main only if within permitted scope; report starting/ending SHA, files, tests, remaining P0 risks. STOP for independent Architect ACCEPT. NO automatic next task.
END_TOOBA_TASK
```
