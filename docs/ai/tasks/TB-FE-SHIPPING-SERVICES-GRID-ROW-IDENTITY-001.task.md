PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-SHIPPING-SERVICES-GRID-ROW-IDENTITY-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: 3786aee814df6d06175e6a418e3746664b3faa96
Architect-Time-Estimate: 10-15 minutes (human estimate, not a worker timer); stop on scope/timebox uncertainty. Current Bridge worker hard cap: 15 minutes.

Architectural-Instruction: Act as a senior software architect. Fix actual ownership and row-identity contracts; never sacrifice frontend/backend architecture merely to satisfy TypeScript. Do not suppress types or weaken constraints.

Goal: Resolve exactly the three ShippingServicesScreen diagnostics in src/frontend/app/admin/shipping-services-screen.tsx caused by ShippingServiceListItem having shippingServiceId but useLegacyAdminGridDirectProps/AppDataGrid requiring a stable id string. Preserve the canonical ShippingService API identity and existing row actions, grid state, sorting/filtering, data semantics, and saved views.

Required-Reads: AGENTS.md; .cursor/skills/tooba-frontend-feature/SKILL.md; docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md; relevant shipping-services-screen.tsx, shipping-services-api.ts, design-system/app-data-grid/legacy-grid-adapters.ts, related grid contracts and focused tests. Do NOT read the opt-in findings register.

Preflight: fetch origin and verify HEAD==origin/main and Expected-Starting-HEAD; inspect tracked worktree; stop and report divergence instead of resetting/stashing or overwriting concurrent changes. Reproduce/locate the precise remaining TypeScript diagnostics using most recent committed typecheck evidence (32 errors).

Implementation-Boundary: First trace AppDataGrid row-id semantics, query adapter, and saved-view/export behaviour. Choose a minimal, correctly owned adapter or typed view-row projection with id === shippingServiceId, only if evidence confirms that is the canonical identity. Do not rename the API/DTO shippingServiceId or introduce fake/random/positional IDs. Ensure column, query rows, and <AppDataGrid> all use the same row type and that edit/deactivate still target shippingServiceId. Prefer change only to src/frontend/app/admin/shipping-services-screen.tsx; focused existing tests may be minimally amended, or one focused discovered test added only if necessary and justified. If the correct fix requires a shared grid contract change, changed API DTO, migration, or product decision, STOP and report; do not silently expand.

Forbidden: backend/schema modifications, global generic weakening, optionalizing id, any, as unknown as, @ts-ignore, non-null assertion masks, tsconfig excludes, baselines, guard weakening, unrelated type-error clusters, broad grid refactoring, Shopeiva/Home/PDP edits, auto AMSC, iterative test loops, automatic next task.

Verification: one TypeScript check after implementation; expect 32 -> 29 with no new diagnostics; run only one directly relevant existing focused grid/shipping test command, adding minimal targeted regression coverage if it detects the identity invariant. Do not run full npm test, broad build, backend suites or visual suites. If a pre-existing unrelated test fails, document evidence and do not chase or rerun in a loop. Prefer a genuine observable identity check over ceremonial regex tests.

Evidence: Record pre/post exact diagnostic counts, changed files, row-identity reasoning, focused test results, remaining risks and any baseline failures in docs/architecture/evidence/TB-FE-SHIPPING-SERVICES-GRID-ROW-IDENTITY-001/summary.md; preserve raw typecheck output if practical. Persist this exact task receipt under docs/ai/tasks/TB-FE-SHIPPING-SERVICES-GRID-ROW-IDENTITY-001.task.md.

Delivery: If safe and complete, make one bounded commit and push main without rewriting history; verify HEAD==origin/main. Otherwise return INCOMPLETE/STOP with blocker and no unsafe change. Return only canonical BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT with SHA, scope, diagnostics, test outcomes and any architectural STOP decision. No automatic next implementation task; wait for independent Architect review.
END_TOOBA_TASK
