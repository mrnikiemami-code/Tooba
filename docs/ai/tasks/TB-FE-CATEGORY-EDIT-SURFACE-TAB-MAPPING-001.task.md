PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-CATEGORY-EDIT-SURFACE-TAB-MAPPING-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Repo: mrnikiemami-code/Tooba
Branch: main
Mode: IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE
Expected-Starting-HEAD: 1a98fa6a33dd18bb27e5f8c84f26f1822c5e77b6

OBJECTIVE
Resolve exactly one residual TS2322 in src/frontend/app/admin/category-admin-screen.tsx around line 1801: tabToEditSurface(tab: TabId): EditSurface | null currently returns tab after filtering only "products", but TabId additionally contains "reservation" while EditSurface excludes it. Preserve the existing separate reservation-policy editing flow and navigation semantics. Starting frontend typecheck baseline: 17 diagnostics. Target: 16 with no new diagnostics.

MANDATORY PREFLIGHT

Verify clean tracked worktree, HEAD == origin/main == Expected-Starting-HEAD. STOP if mismatch or conflicting edits; do not reset, stash, rebase or force push.
Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md, docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md, relevant portions of src/frontend/app/admin/category-admin-screen.tsx and existing focused category/reservation guards.
Do NOT open the deferred Architecture Findings Register absent explicit user instruction.

ROOT-CAUSE AND STOP GATE
Inspect TabId definition, EditSurface union, tabToEditSurface implementation, callers during tab switching, handleHeaderEdit (including reservation branch), and ReservationPolicyEditor on the reservation tab. Determine whether "reservation" must map to null (separate editing surface) or the owner contract genuinely includes reservation in EditSurface. Existing source indicates reservation has a separate editor and handleHeaderEdit explicitly returns for reservation; DO NOT mechanically add "reservation" to EditSurface. Ensure switching tabs while in form edit mode preserves existing dirty/discard and draft behaviour. If a product decision or broader state-machine change is required, STOP as BLOCKED_ARCHITECT_DECISION with evidence, no speculative code.

ALLOWED CHANGES
Only src/frontend/app/admin/category-admin-screen.tsx as production source; focused test amendment only if it guards a genuine behavior without ceremonial source assertions; task receipt and bounded evidence. Prefer an explicit, exhaustive and type-safe conversion or appropriate narrowing for the two non-edit tabs (products and reservation). No casts, any, ts-ignore, non-null assertion or compiler suppression. No unrelated category, reservation, backend, schema, ShopEva Home/PDP or shared-grid changes.

VERIFICATION

Exactly one cd src/frontend && node_modules/.bin/tsc --noEmit after the final edit; compare normalized diagnostics to the committed 17-error baseline in docs/architecture/evidence/TB-FE-PREVIEW-FILL-REAL-REVIEW-FIXTURE-001/typecheck-after-tsc.txt; target 17 -> 16, zero added errors.
Run one or two directly relevant existing focused tests/guards; record honest pass/fail, do not chase pre-existing failures. No broad tests, builds, AMSC, test loops or browser unless a specific stop-gate issue requires escalation.
Prove reservation continues using its independent editor and neither tab switching nor editing state receives a fictitious new EditSurface. Keep source change narrowly scoped.

DELIVERY
Commit exact task receipt as docs/ai/tasks/TB-FE-CATEGORY-EDIT-SURFACE-TAB-MAPPING-001.task.md, brief summary and raw typecheck/test outputs under docs/architecture/evidence/TB-FE-CATEGORY-EDIT-SURFACE-TAB-MAPPING-001/. Commit/push to main only after passing preflight; report Base-SHA, Final-SHA, changed files, diagnostic delta, tests, behavior, pre-existing failures, risks and rollback. STOP for independent Architect acceptance; DO NOT self-start the next task.
END_TOOBA_TASK
