PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MVP-DESTINATION-LIVE-067
Parent-Task: TB-SHIPPING-MVP-DESTINATION-066
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_GEOGRAPHY_BACKED_STOREFRONT_DESTINATION_AND_LIVE_CART_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 001fa35f7599b4b8dd7d489b9cdc62836ab18ed4
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD

ARCHITECT RULING / DELIVERY BUDGET
066 ACCEPT in scope; 067 is task 5 of 8 in the agreed Shipping MVP completion budget. Favor real completion over ceremony. One active Bridge task only. PASS != Architect ACCEPT. Follow AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md. No automatic next task. This is not authorization for Shipping pricing cutover, payment, historical order mutation, production deployment, or invented commercial tariffs.

PREFLIGHT

Claim exactly one Pending task on tooba-main after BRIDGE-WAKE. Preserve original task receipt unmodified and unstaged. Fetch origin/main. Verify ancestry of Expected-Base-SHA. Evidence-only advancement permitted only after exact per-path verification; overlapping source drift => RECOVERY_CONFLICT and STOP. Preserve unrelated dirty/untracked paths. No reset/stash/rebase/force push.
Read 065 and 066 planning/results, 057 shadow contract, Shipping recovery checkpoint, current frontend customer shipping code, existing Geography admin read APIs/contracts, Checkout authentication/cart session mechanism, and process launch evidence from 065. Search only relevant code; no broad architecture audit.
Re-identify actual Development Host, backend and frontend processes, effective tenant/database through existing provenance; only Development tooba_alpha is authorized for task-owned disposable test writes. This is a TEST database; preserve unrelated working data where feasible. Do NOT create cloned/scratch/parallel DB, reset schema, or run unrelated migrations. Do not touch staging/production.

MISSION — ONE COHESIVE LIVE DESTINATION CLOSURE
A. In the EXISTING customer shipping screen, replace or augment the static city-choice source using existing Geography read API and stable IDs where safe. Country IR -> area/province -> city must keep parent-child hierarchy and produce validated Geography DestinationIdentifiers in the EXISTING shipping projection request. Do not guess IDs from names, fabricate location data, or introduce a separate customer page. Use existing frontend patterns; avoid new backend endpoints unless genuinely essential to let storefront users read Geography without admin credentials. If the current Geography APIs are admin-only, do NOT leak admin capability to customers: implement the smallest Geography-owned read-only public/customer route guarded by the existing public-read pattern, with necessary bounded query parameters and no write exposure, and document this change. Preserve old free-text province/city compatibility for saved addresses and legacy clients. No mandatory extra HTTP on city changes if existing projections suffice; avoid unnecessary fetches and stale-selection races.
B. Preserve the entire legacy shipping price, method eligibility, preparation/lead/delivery promise, checkout draft, cart and paid snapshot behavior. Keep shadow and cutover flags OFF. Explicit ids are observation-only as established by 066; bad or ambiguous combinations fail closed to DestinationNotComparable without guessed fallback.
C. Rebuild/restart ONLY the positively identified running Development Host if required to load 066/067 code; record actual command/process, restore health, and check the frontend without restarting it unless indispensable. Do not kill other processes. Using a genuine existing or task-owned disposable authenticated cart session (through canonical development/customer flow, never bypass), execute ONE end-to-end customer UI or HTTP session demonstrating selected country/area/city ids reach projection and return an unchanged legacy quote. Compare same session baseline response/amount before/after new destination metadata when safely possible. Do not place/pay a real order. If real authenticated cart/session cannot be provisioned safely in time, record precise blocker and PARTIAL; do not claim live end-to-end PASS from a 401 or only unit tests.
D. Prove old client request without destination ids still behaves as before; invalid hierarchy is rejected or observation NotComparable, never silently trusted. Fix any directly observed defect in this scoped UI/API flow, not unrelated issues. Focused tests for one valid city path, old client, stale-response guard, and malformed hierarchy; frontend typecheck + required relevant build/test only. No Host.Tests build against locked running DLLs, no long polling, no repeated scratch scaffolding. Stop at 45 minutes; document remaining limitations honestly.

ALLOWED SCOPE

Existing storefront shipping selection/API files under src/frontend/app/storefront/ (only flow-relevant files and their focused tests)
Existing Geography read endpoint/application/contracts/registration files ONLY if required for safe customer-readable Geography IDs; not Geography persistence, migrations, or seed.
Existing Order storefront projection request/observation seam files ONLY if a concrete wiring defect is proven, with focused tests.
docs/planning/shipping/SHIPPING-MVP-DESTINATION-LIVE-067.md
docs/architecture/evidence/TB-SHIPPING-MVP-DESTINATION-LIVE-067/**
docs/ai/tasks/TB-SHIPPING-MVP-DESTINATION-LIVE-067.task.md (Bridge receipt ONLY; never stage)
Any wider source modification requires a new Architect task; report it and stop rather than broaden scope.

FORBIDDEN
No Shipping rate/coverage authority switch, appsettings feature gate activation, legacy quote mutation, Commerce/Host security bypass, manual role/capability insert, Checkout/payment/order snapshot edits, production/staging contact, new parallel database, all-module migration, broad test suites, frontend redesign, changes to unrelated dirty files, fabricated nationwide grants/tariffs, automatic follow-on task.

ACCEPTANCE
PASS only if (1) real storefront UI or its existing customer flow obtains stable Geography IDs and passes validated country+area+city hierarchy into the existing projection body, (2) authenticated live Development cart projection is demonstrated against rebuilt current Host, (3) legacy price/method invariance and legacy-client compatibility are proven, (4) invalid/ambiguous IDs cannot be trusted, (5) existing 065/066 picker race stays fixed, and (6) changes are committed, pushed, evidenced with test/session/target provenance and elapsed time. Otherwise PARTIAL/BLOCKED with exact remaining blocker. Never equate successful mock/unit tests or unauthenticated 401 with live acceptance.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MVP-DESTINATION-LIVE-067
Parent-Task: TB-SHIPPING-MVP-DESTINATION-066
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_GEOGRAPHY_BACKED_STOREFRONT_DESTINATION_AND_LIVE_CART_PROOF
Bridge-Task-Row: <actual row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 001fa35f7599b4b8dd7d489b9cdc62836ab18ed4
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<actual frontend Geography-id source; auth/compatibility; live authenticated cart proof; quote invariance; exact changed paths, focused tests, process and DB provenance; safety; elapsed; commit/push; open blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
