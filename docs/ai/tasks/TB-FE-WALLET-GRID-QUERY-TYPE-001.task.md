PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-WALLET-GRID-QUERY-TYPE-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: ed0c0ba3b4243d58542b20b6bf95d044e2b0251b
Architect Decision: Previous ReactNode import repair ACCEPTED after independent GitHub diff and 25-error evidence review.

Objective: Fix exactly the remaining TS7006 implicit-any diagnostic on the giftCardQueryAdapter callback in src/frontend/app/wallet/wallet-ui.tsx (approximately line 577), by annotating the parameter with the canonical GridServerQuery type. Target TypeScript errors: 25 -> 24, zero new errors.

Senior architect constraint: Diagnose ownership and use the existing design-system/data-grid GridServerQuery export. This is a type-only, runtime-neutral correction; never degrade frontend/backend architecture just to satisfy tsc. Preserve wallet and gift-card behaviour, grid query/sort/filter/page/export, callbacks, dependencies, visual design and existing data contracts.

Required preflight: Fetch main, verify HEAD == origin/main == Expected-Starting-HEAD and inspect tracked changes. If divergence or unsafe unrelated edits exist, STOP and report without reset/stash/force push. Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md and docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md in targeted sections only. Do NOT read the deferred findings register.

Allowed production change: src/frontend/app/wallet/wallet-ui.tsx only. Prefer extending its existing type-only import to include GridServerQuery, then annotate exactly the query callback argument. Add no new interface or duplicate grid type. No unrelated cleanup.

Forbidden: any, unsafe type assertions, ts-ignore, tsconfig/exclusion/baseline/guard changes, shared grid or API contract rewrites, backend/schema changes, home/PDP/Shopeiva changes, new dependencies, automatic repair chains, AMSC waves or unapproved tasks.

Validation: Run ONE local TypeScript noEmit check and compare diagnostics against the committed 25-error baseline in docs/architecture/evidence/TB-FE-ADMIN-SCREENS-REACTNODE-IMPORT-001/typecheck-after-tsc.txt. Verify exactly targeted TS7006 disappears with no added errors. Run only the nearest pre-existing focused wallet/grid test if readily identifiable; do not create ceremonial tests, run full test suites, or chase unrelated baseline failures. If test is unavailable, say so. Check diff and scope before commit.

Timebox: Architect estimates a small unit comfortably under 15 minutes; protocol target 10-12 minutes, HARD STOP at 15 minutes. Cursor does not need to track time continuously; if investigation unexpectedly grows or requires runtime/contract redesign, report INCOMPLETE or BLOCKED and STOP, no retry loop.

Evidence: docs/architecture/evidence/TB-FE-WALLET-GRID-QUERY-TYPE-001/summary.md and minimal before/after compiler output and focused-test result, as applicable. Commit the exact task receipt to docs/ai/tasks/TB-FE-WALLET-GRID-QUERY-TYPE-001.task.md, commit/push only authorized changes to main if safe. No automatic next implementation task.

Worker result: Return only canonical PIPELINE-PROTOCOL: BRIDGE-WAKE-V1; BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT, including SHA, exact files, TypeScript counts, focused tests, baseline failures, deviation/risk, head state, and STOP. Await Architect independent acceptance.
END_TOOBA_TASK
