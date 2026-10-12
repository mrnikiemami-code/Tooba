PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-CONTENT-ACTIONS-SLOTS-CONTRACT-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE
Repo: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: 345a87b5db6b8dbaab123543af2628007957e040

OBJECTIVE
Investigate and, only if demonstrably safe, eliminate the single pre-existing frontend typecheck TS2322 in src/frontend/app/admin/content-list.tsx around line 262: actionSlots: 5 is not assignable to 1 | 2 | 3 | undefined on buildPinnedActionsColumnDef. Baseline is 19 total TypeScript errors. Preserve ALL five underlying article actions, permissions, visibility conditions, click/navigation targets, and observable action affordances.

MANDATORY PREFLIGHT
Verify expected HEAD == HEAD == origin/main; clean tracked worktree. Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md, docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md, the relevant actual code and the canonical task-handoff protocol. Do NOT open the deferred findings register. Review src/frontend/app/admin/content-list.tsx; the actual owner/signature/implementation of buildPinnedActionsColumnDef; AppGridRowActionsCell and its overflow/compact handling; in-repo comparable five-action menus; relevant focused tests. Respect existing frontend architecture and locked Shopeiva Home/PDP surfaces.

STOP GATE — REQUIRED BEFORE ANY EDIT
Establish exactly what actionSlots means at runtime: width/reservation, count, rendering, overflow, or an actual cap. Prove whether the five action definitions (view/edit/delete/archive/publish/unpublish, including conditional visibility) are all preserved and accessible if the slot count is changed or removed. Do NOT mechanically replace 5 with 3, widen shared actionSlots union to 5, hide actions, remove actions, silence the type error, or apply casts. If changing/removing actionSlots would lose a visible control, change geometry unexpectedly, violate shared contracts, or requires an architectural/product choice, STOP and return BLOCKED_ARCHITECT_DECISION with precise source paths, alternatives, and impact. Do not commit speculative production edits when blocked.

ALLOWED IMPLEMENTATION IF GATE PASSES
Only src/frontend/app/admin/content-list.tsx as production scope. Choose the smallest contract-correct local repair backed by actual implementation and reference callers; preserve all existing action definitions and effective rendering, labels, localization, permissions and navigation. If the existing shared component's supported semantics cannot express the requirement without user-visible change, STOP instead. No shared design-system edits without separate architect approval. A minimal focused test amendment is allowed only if it tests a meaningful observed contract and does not weaken existing tests.

VERIFICATION
Run exactly one frontend tsc --noEmit after final edit, compare normalized before/after diagnostics by filename/line/TS code: target must disappear (19 -> 18 expected) with zero new diagnostics. Run the one or two closest existing focused content/action guard suites; do not run full tests/build/AMSC. Document source-backed behavioural evidence for every action. If blocked, provide audit evidence without misleading typecheck reduction claims and do not force a patch. Record unrelated pre-existing failures without chasing them.

DELIVERABLES
If repaired: commit exact task receipt to docs/ai/tasks/TB-FE-CONTENT-ACTIONS-SLOTS-CONTRACT-001.task.md; bounded summary and raw evidence to docs/architecture/evidence/TB-FE-CONTENT-ACTIONS-SLOTS-CONTRACT-001/; commit and push to origin/main only after scope verification. Report commit SHA, real diagnostic count, test results, exact behavior delta if any, any outstanding uncertainty and STOP. If blocked: commit only task receipt and short evidence if safe; report BLOCKED_ARCHITECT_DECISION and STOP. NEVER auto-start the next task. No stash, reset, force-push, type suppression, tsconfig/CI baseline adjustments, or unrelated refactors.
END_TOOBA_TASK
