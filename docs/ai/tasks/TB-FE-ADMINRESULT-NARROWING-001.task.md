PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-ADMINRESULT-NARROWING-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: 48a59532c1f5e7e961f047518f5bfcea1313a4c4

Architectural intent:
Act as a senior software architect. Fix consumer misuse of discriminated AdminResult unions, preserving the frontend/backend contracts, runtime semantics, UI behavior, and ownership boundaries. A bug fix MUST NOT sacrifice architecture in either frontend or backend. No global type broadening or casting to hide errors.

Scope:

Inspect current definitions and callers before editing. In src/frontend/app/admin/settings/page.tsx, resolve the five TS2339 reads of result.denied by correct discriminant narrowing; preserve each success, denied, generic-error and message path.
In src/frontend/app/admin/landing-pages/admin-landing-pages-screen.tsx, resolve the TS2339 reading pageResult.message on a successful result by branching according to the real AdminResult contract; preserve the intended user-facing behavior rather than inventing messages.
In the same landing-pages screen, resolve the unsafe onClick={refresh} MouseEvent argument leak using a proper zero-argument click wrapper. Preserve refresh options and existing notifications.
Add only focused regression coverage where existing local tests or suitable testing seams make it cheap and meaningful. Do not create a large new harness. Treat any unexpected product decision, missing contract, or visual lock violation as STOP/ASK.

Permitted production files: src/frontend/app/admin/settings/page.tsx; src/frontend/app/admin/landing-pages/admin-landing-pages-screen.tsx. Relevant existing focused test files may be amended if necessary and explicitly explained. Read relevant skill (.cursor/skills/tooba-frontend-feature/SKILL.md), AGENTS.md and frontend architecture standard before changes. Do not read the deferred findings register unless separately requested.

Forbidden: new competing AdminResult definition, global shared contract refactor, any/unknown-as masking, ts-ignore, tsconfig changes, suppression, guard weakening, baseline resets, modifications to backend/schema, unrelated frontend/screens, Shopeiva Home/PDP surfaces, auto AMSC waves, and auto-next tasks.

Validation: One frontend typecheck after changes, compare remaining diagnostics to recorded 56-error baseline; expect removal of the six TS2339 and one TS2322 diagnostics with ZERO new errors. Execute only focused existing admin/settings/landing page tests if available and reasonably fast; no automatic full suites or builds. If one baseline failure occurs, record it faithfully and stop retrying. Verify git diff and scope.

Timebox: Architect estimates approximately 10-12 minutes of focused work, under the current Bridge hard maximum of 15 minutes. Cursor is not required to measure elapsed time continuously; avoid loops, repeated test runs or exhaustive exploration. If work becomes larger or uncertain, report INCOMPLETE and STOP rather than expanding scope.

Evidence: Record actual before/after typecheck counts, test commands/results, files touched, deviations, and any unresolved risks in docs/architecture/evidence/TB-FE-ADMINRESULT-NARROWING-001/summary.md. Commit the exact received task under docs/ai/tasks/TB-FE-ADMINRESULT-NARROWING-001.task.md. Commit/push bounded completed work to main only if preflight safe and no conflicting concurrent updates. Never force-push or overwrite unrelated work.

Result envelope: PIPELINE-PROTOCOL: BRIDGE-WAKE-V1 and BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT with Task-ID, base/final SHA, PASS/INCOMPLETE, exact test and typecheck evidence, touched files, scope statement, risks, and STOP. automaticNextImplementationTask = NONE. Architect must independently review before next task.
END_TOOBA_TASK
