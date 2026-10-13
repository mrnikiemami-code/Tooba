TB-P0-ORDER-FRESH-PAYABILITY-024

Task-ID: TB-P0-ORDER-FRESH-PAYABILITY-024
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-PAYMENT-FRESH-ENTRY-GUARD-023
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 586a831c03cfdf34a865a377c11467b003077a21
Repository: mrnikiemami-code/Tooba
Branch: main
Timebox: 20 minutes HARD

automaticNextImplementationTask: NONE

PREFLIGHT
Verify HEAD == origin/main == Expected-Base-SHA; if not, STOP without rebase, stash, or touching unrelated dirty files. Read AGENTS.md and the protocol plus evidence 017–023. Host CLOSED: absolutely no edit/build/stop/restart/deploy of Host production, Program.cs, :5088, live database, schemas, migrations, frontend, settings, or providers. Do NOT modify or run Host.Tests in this task.

ARCHITECT DECISIONS — FIXED
Reject permissive NotEvaluated: absence of cycle authority or cycle is NOT authorization for NEW money movement. Historical Host.Tests fixtures without cycle are not a reason to make production fail-open. No extension/TTL change from a read-only payability verdict. Preserve readable/verifyable historical payments after reservation expiry: do not throw inside GetPayableAsync merely because cycle/holds are expired. Reuse existing authoritative Order/Inventory ports; no duplicated inventory mutation logic. Payment retry count is not reservation-renewal count; never hardcode 3. No actual payment initiation in this task.

MISSION — ONLY ORDER-SIDE READ-ONLY TRUTH
Implement an additive read-only, typed fresh-payment payability projection on the Order-owned payment bridge, or a closely focused Order-owned port if that avoids changing existing GetPayableAsync semantics. Cover: (1) authoritative checkout, seller-order lines and reservation IDs from persisted state; (2) active unexpired reservation cycle using canonical ReservationCyclePayabilityPolicy; (3) actual Inventory reservation Held/unexpired using existing owner-provided read projection, not cycle status alone; (4) all lines must be covered, no missing/duplicate/foreign IDs; (5) retry-limit distinction according to existing settings, but no invented numeric ceiling; (6) absence of cycle authority or cycle returns nonpayable / unavailable, never Payable or permissive NotEvaluated. The new projection must NOT cause payment read, verification, callback or refund to fail on an expired cycle. Ownership enforcement for later initiation stays at Payment storefront orchestration; avoid introducing a new public money route. Provide documented typed outcomes rather than new error strings; no Payment catalog edit in this slice.

IMPLEMENTATION BUDGET
Prefer Order.Contracts + Order.Infrastructure bridge/adapter + focused Order tests only. Explicitly authorized: add a ProjectReference from Tooba.Order.Tests to Tooba.Order.Infrastructure and Microsoft.EntityFrameworkCore.InMemory to that TEST PROJECT ONLY if essential, version aligned with repo. Better use a narrower fakeable Order port if possible. No changes to Host.Tests, PaymentDirectory, global DI, Inventory state machine, expired-row reclaimer or frontend. If an important code path cannot be tested without Host or PostgreSQL mutation, STOP with an exact concrete blocker; do not publish an untested new money gate. No general architecture analysis or redesign.

VERIFICATION
Run at most one focused Order module test build/run (and one guard filter if necessary), never Host.Tests or solution build. Cover valid active cycle+all Held, expired cycle, Active cycle but Released/expired/missing Inventory, no cycle with authority, missing authority, multiple lines including duplicate IDs, existing payment projection remains readable after expiry, cancellation. Source guard: no Host production change, no fixed TTL, no payment provider invocation. If in-memory tests cannot prove real EF mapping, clearly disclose limits; avoid pretending race-proof from fake data.

OUTPUT
Commit/push only bounded safe changes to main, with source paths, SHAs, tests, and precise remaining Payment integration work; no automatic next task. If blocked, docs-only and STOP_FOR_ARCHITECT. Preserve baseline/unrelated files. Return BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT, explicitly distinguish VERIFIED / UNVERIFIED, and report whether old public Payment route remains unguarded (expected YES until next task).
