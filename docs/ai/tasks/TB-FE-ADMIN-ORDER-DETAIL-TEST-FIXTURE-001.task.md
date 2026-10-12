PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-ADMIN-ORDER-DETAIL-TEST-FIXTURE-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: c204c65b467c4548de44ca8223a6340854ee58b8
Architect estimate: a bounded approximately 10-12 minute task; target completion within current 15-minute Bridge protocol. Do not start a timer, loop, or chain tasks. If unsafe or unexpectedly broad, report INCOMPLETE/BLOCKED and STOP.

Senior architect principle: fix the true test-fixture/contract discrepancy without sacrificing Frontend or Backend architecture. Typecheck green is not permission to weaken a domain contract, use casts, or invent fake data.

Objective: Resolve the single TS2345 diagnostic in src/frontend/app/admin/admin-api.test.ts around line 114 where the argument to enrichAdminOrderDetail lacks required AdminOrderDetail.consolidatedPackages. Current recorded baseline: 24 TypeScript errors. Expected result: 23 with no new errors, contingent on contract analysis.

Required preflight:

Fetch origin and check HEAD == origin/main == Expected-Starting-HEAD. Preserve all unrelated tracked and untracked work. STOP on conflicting active changes.
Read AGENTS.md and the relevant frontend skill, plus only the directly applicable contract owner, test fixture, and nearby existing tests. Do not read the deferred architecture findings register.
Inspect src/frontend/app/admin/admin-api.ts: AdminOrderDetail declaration, mapAdminOrderDetail, enrichAdminOrderDetail, and consolidatedPackages model semantics. Inspect the precise fixture in admin-api.test.ts and confirm the intended shape and whether the test is modeling a genuinely empty package projection. Do not assume an empty array without checking.

Allowed production changes: NONE. Preferred edit: src/frontend/app/admin/admin-api.test.ts only, if and only if a contract-faithful fixture addition is justified. Add consolidatedPackages: [] only when the test's scenario legitimately contains no consolidated packages; otherwise build a representative valid package fixture and preserve current assertions. If this indicates a legitimate optionality/wire contract mismatch instead of stale test setup, STOP and report evidence for Architect decision without changing types.

Forbidden: changes to AdminOrderDetail or other API DTO definitions, mapper behavior, backend/schema, tests outside this focused fixture, any new mock-only production seam, any, casts hiding errors, ts-ignore, suppression, tsconfig/exclusions, guard/baseline weakening, Home/PDP/Shopeiva, AMSC, broad suites, and autonomous next-task issuance.

Verification: Run exactly one frontend typecheck at the final edit state (tsc --noEmit) and compare diagnostics against the committed 24-error baseline at docs/architecture/evidence/TB-FE-WALLET-GRID-QUERY-TYPE-001/typecheck-after-tsc.txt; record total, removed/introduced diagnostics. Run the directly related existing admin-api.test.ts focused suite once. If an unrelated old failure occurs, document it without repeated retries. No full npm test, build or backend suite.

Evidence: concise docs/architecture/evidence/TB-FE-ADMIN-ORDER-DETAIL-TEST-FIXTURE-001/summary.md including source-grounded contract decision, exact diff, before/after compiler counts, focused test result, limitations and rollback. Persist this exact task receipt under docs/ai/tasks/TB-FE-ADMIN-ORDER-DETAIL-TEST-FIXTURE-001.task.md. No unnecessary raw logs or lengthy generated reports.

Delivery: if safe, commit and push only the approved bounded changes to main after confirming no unrelated tracked changes are staged. Return canonical PIPELINE-PROTOCOL: BRIDGE-WAKE-V1 / BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT with Task-ID, PASS or BLOCKED/INCOMPLETE, final SHA, touched files, validations and exact failures, HEAD/origin status and explicit Automatic-Next-Implementation-Task: NONE. STOP for Architect review. No automatic repairs or next tasks.
END_TOOBA_TASK
