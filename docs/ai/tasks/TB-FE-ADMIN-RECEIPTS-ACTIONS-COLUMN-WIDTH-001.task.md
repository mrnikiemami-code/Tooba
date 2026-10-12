PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-ADMIN-RECEIPTS-ACTIONS-COLUMN-WIDTH-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE
Expected-Starting-HEAD: 82dcb78aba67632ceeb16420607798b757d2fcbc
Repo: mrnikiemami-code/Tooba
Branch: main

OBJECTIVE
Remove exactly the TS2739 diagnostic at src/frontend/features/admin-receipts/components/receipts-screen.tsx:222: the actions GridColumnDef<AdminReceiptRow> lacks required width and minWidth. Preserve existing receipts grid behavior and its canonical column contract. Target typecheck 23 -> 22, with zero new diagnostics.

PREFLIGHT AND REQUIRED READS

Verify HEAD == origin/main == Expected-Starting-HEAD, tracked worktree clean; STOP on conflict. Do not modify existing untracked files.
Read AGENTS.md; .cursor/skills/tooba-frontend-feature/SKILL.md; docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md. Do not access the deferred findings register.
Inspect receiptColumns, the canonical GridColumnDef in design-system/data-grid/types.ts, the existing design-system/app-data-grid/legacy-grid-bridge.ts actions-column behavior and nearby Admin receipts/grid actions column precedents. Confirm whether the missing dimensions affect the actual rendered grid and choose consistent positive finite dimensions. If the screen's actions column is excluded or overridden by a separate actions implementation, state this explicitly and ensure no undesired extra blank column or changed actions affordance.
Review existing focused receipts/grid tests and relevant typecheck baseline at docs/architecture/evidence/TB-FE-ADMIN-ORDER-DETAIL-TEST-FIXTURE-001/typecheck-after-tsc.txt.

ALLOWED PRODUCTION CHANGE
Only src/frontend/features/admin-receipts/components/receipts-screen.tsx. Prefer adding the two required numeric width/minWidth properties to the existing actions column, if this matches canonical grid behavior. Keep width >= minWidth and appropriate for compact AppGridRowActionsCell. No changing column id, accessor, cell, exportable, sortable, row-action logic, other column sizes, data loading, or filters. If evidence shows that adding dimensions is incorrect or a broader grid architecture change is needed, STOP and report BLOCKED_ARCHITECT_DECISION; do not guess.

FORBIDDEN
No any, unknown-as, ts-ignore, suppressions, optionalizing shared GridColumnDef dimensions, tsconfig/CI/baseline changes, backend, schema, storefront, Shopeiva locked surfaces, generic grid redesign, unrelated fixes, AMSC, broad test suites, retry cascade or next-task automation.

VERIFICATION
Run frontend node_modules/.bin/tsc --noEmit once after edit; compare diagnostics line-by-line with baseline: expected 23 -> 22, the single target removed, none added. Run only the nearest existing focused receipts or grid bridge test(s), if applicable. Do not add ceremonial tests; add or amend one focused test only if genuinely necessary to guard material behavior. Record any pre-existing failures accurately without chasing them. Inspect exact diff and ensure only in-scope changes.

DELIVERY
Commit the exact task receipt to docs/ai/tasks/TB-FE-ADMIN-RECEIPTS-ACTIONS-COLUMN-WIDTH-001.task.md and brief evidence under docs/architecture/evidence/TB-FE-ADMIN-RECEIPTS-ACTIONS-COLUMN-WIDTH-001/ (summary, before/after typecheck, focused test result). Commit and push to main only if preflight, scope and tests permit; no force push, reset or stash. Return a BRIDGE-WAKE-V1 worker result with root cause, actual diff, dimension rationale, test counts, diagnostics before/after, changed files, commit SHA and HEAD/origin confirmation. STOP for Architect ACCEPT; issue no follow-on task.
STOP
END_TOOBA_TASK
