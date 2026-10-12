PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-ADMIN-SCREENS-REACTNODE-IMPORT-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: 1a18bab5ca5f98258c8f34ca734186322bea91ca
Architect Intent: Resolve exactly the missing ReactNode TS2304 diagnostic in src/frontend/app/admin/admin-screens.tsx without runtime or architecture changes. Think as a senior software architect; never trade frontend/backend boundaries or real behavior for a green compiler.
Preflight: Verify HEAD == origin/main == Expected-Starting-HEAD and inspect tracked worktree. Preserve pre-existing untracked artifacts. If unexpected divergence or conflicts exist, STOP and report; do not reset, stash, force-push or silently rebase.
Required Reads: AGENTS.md; .cursor/skills/tooba-frontend-feature/SKILL.md; docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md; target file imports and truncatedCell signature at approximately line 234; latest TypeScript diagnostic evidence under docs/architecture/evidence/TB-FE-LANDING-COMPOSER-DRAG-EVENT-TYPES-001/typecheck-after-tsc.txt. Do not read the deferred findings register unless the user explicitly asks.
Allowed Production Change: Only src/frontend/app/admin/admin-screens.tsx, preferably import type ReactNode from react in its existing import. Confirm the canonical type owner before editing. No change to truncatedCell rendering, markup, UI behavior, other types, or any other production file.
Forbidden: No any, casts, ts-ignore, tsconfig/exclusions, test baseline edits, guard weakening, dependency additions, refactors, backend/Host/schema changes, Shopeiva/Home/PDP changes, unrelated diagnostic fixes, AMSC wave or automatic next task.
Verification: Run ONE frontend typecheck (node_modules/.bin/tsc --noEmit) and compare with the committed 26-error baseline, expecting precisely 26 to 25, no new diagnostics. Since this is an erased type import, avoid broad or unrelated test suites. Run at most one directly relevant existing small test if it genuinely exercises this file; otherwise document why typecheck is sufficient. Do not generate ceremonial tests. If anything unexpected appears, stop and report instead of retrying in a loop.
Evidence: Commit a concise summary, full raw post-change typecheck result and a before/after diagnostic count under docs/architecture/evidence/TB-FE-ADMIN-SCREENS-REACTNODE-IMPORT-001/. Save this exact task under docs/ai/tasks/TB-FE-ADMIN-SCREENS-REACTNODE-IMPORT-001.task.md. Avoid lengthy unrelated logs.
Commit And Push: Only if focused verification confirms the intended fix and no new errors, commit and push main without force. State exact SHA, touched files, test outcomes and remaining TypeScript count. If blocked, do not manufacture PASS.
Timebox: Architect estimates up to 15 minutes of bounded work (target 10-12). You do not need to track a timer; do not expand work to fill time. If scope grows, return INCOMPLETE and STOP.
Final Worker Response: PIPELINE-PROTOCOL: BRIDGE-WAKE-V1 followed by BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT with Task-ID, Status, Verdict, cause, delta, verification, SHA and remaining risk. automaticNextImplementationTask = NONE. STOP for independent Architect review. No self-initiated next task.
END_TOOBA_TASK
