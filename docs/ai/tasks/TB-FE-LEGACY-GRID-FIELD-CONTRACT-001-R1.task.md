PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-LEGACY-GRID-FIELD-CONTRACT-001-R1
Parent-Task: TB-FE-LEGACY-GRID-FIELD-CONTRACT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: BOUNDED_BEHAVIORAL_VERIFICATION_WITH_STOP_GATE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: f9973b04102f9e854460fada8f2fedc5be170287

Architect decision: HOLD the prior change pending targeted evidence that removing ColDef.field does not alter sorting/order in real AG Grid. Review as a senior software architect. Never sacrifice frontend or backend architecture just to clear a TypeScript diagnostic.

Time budget: Architect-estimated 10-12 minutes, maximum 15 minutes under the current Bridge protocol. You do not need to track elapsed time; avoid loops, lengthy test suites or exploratory refactors. If a reliable focused runtime verification is not achievable in bounded scope, report BLOCKED with specific evidence and STOP.

Required reads: AGENTS.md, relevant frontend feature skill, the canonical Grid bridge and AppDataGrid sort handler, existing focused grid tests, and the previous task's evidence summary. Do not read the deferred findings register.

Scope: Verify actual row order and sort directions for at least one sortable legacy column whose id is NOT a property on the row (e.g. orders amount/customer), and one whose id IS a real property. Compare the behavior with field absent versus prior field assignment, preferably through a lightweight existing test/runtime seam. Confirm query sort key, visible ordered rows, and absence of unintended second AG Grid sort. If an actual browser check is used, use already-running local services without restarting them. No full browser suite or broad environment setup.

Decision gate: If behavioral equivalence is established with reproducible targeted evidence, do not change production code; return PASS verification evidence. If a regression is proven, make the smallest architecture-safe correction within legacy-grid-bridge.ts and its existing focused tests only, retaining typed field semantics and app-owned sorting. If a safe correction requires broad redesign or unapproved files, STOP and report BLOCKED_ARCHITECT_DECISION; do not mask it with a cast, any, ts-ignore, disabled sorting or test weakening.

Validation: Run only the directly relevant existing focused grid tests and one frontend tsc --noEmit if production code changes. State any known baseline failures; do not chase unrelated failures. No backend, Home/PDP, schema, global baselines, or AMSC.

Evidence: Report exact runtime/test seam, representative before/after ordered IDs and sort keys or clearly state why verification was not possible. If code changes, include compiler delta and focused tests. Persist exact task receipt under docs/ai/tasks/ and compact evidence under docs/architecture/evidence/TB-FE-LEGACY-GRID-FIELD-CONTRACT-001-R1/; commit and push only if there are justified changes or evidence files, after clean-head checks. No force push, no reset, no process termination.

Result: Return only canonical BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT, with final SHA, changed files, PASS/BLOCKED, limitations and explicit STOP. AutomaticNextImplementationTask: NONE. Do not create/execute another task or retry loop.
END_TOOBA_TASK
