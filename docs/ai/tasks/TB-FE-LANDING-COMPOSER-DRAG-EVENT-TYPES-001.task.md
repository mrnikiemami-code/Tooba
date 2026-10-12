PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-LANDING-COMPOSER-DRAG-EVENT-TYPES-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: 20c013e5e69cca80bcb240679b241b8c68b966cf

Architect intent: Resolve ONLY the two TypeScript drag-event element mismatches in the Admin Landing Page composer without altering drag/drop semantics, architecture, user-facing visuals, or Shopeiva-locked surfaces. Think as a senior software architect: compiler green alone is insufficient, never sacrifice Frontend/Backend architecture to remove a diagnostic.

Estimated human task size: approximately 15–20 minutes maximum by Architect's estimate; Cursor does not need to track a clock. Keep actual execution bounded and focused under the current Bridge worker timebox (target 10–12 minutes, hard stop at 15 minutes). At risk of overrun, report INCOMPLETE and STOP; no retries/loops.

Required preflight: verify HEAD == origin/main == Expected-Starting-HEAD and note existing untracked files without deleting/stashing them. Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md and docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md as applicable. Read relevant composer function and its immediate source/event target only. Do NOT read architecture findings register unless Architect/user explicitly asks.

Observed baseline: 28 TypeScript errors remain; the two targeted diagnostics are app/admin/landing-pages/admin-landing-page-composer.tsx at approximately 1024 and 1025. The dragover/drop handlers are typed DragEvent<HTMLLIElement> but the JSX binds both to a nested div inside an li. The drag-start handler binds to a button and MUST retain its button typing. Source currently shows onSectionDragOver and onSectionDrop reading preventDefault, dataTransfer and id/order only.

Allowed production edits: ONLY src/frontend/app/admin/landing-pages/admin-landing-page-composer.tsx. Correct the two receiving handlers to the actual event-currentTarget element type without casts. Confirm whether HTMLDivElement is the correct bound owner and retain HTMLButtonElement for drag start. Do not move handlers, modify markup, change reorder algorithms, order persistence, aria semantics, class names, styles or event propagation. If runtime contract or an alternative binding prevents a safe local correction, STOP as BLOCKED_ARCHITECT_DECISION instead of guessing.

Forbidden: no changes to Home/PDP/storefront, backend, schema, shared DragEvent types, third-party typings, tsconfig, guard baselines or errors; no any, @ts-ignore, broad assertions, fake casts, type suppression, unrelated lint/type fixes or visual refactors. No AMSC wave, no full suite, no browser automation unless truly required by discovered risk.

Validation: one tsc --noEmit run after edit and compare against committed 28-error baseline from docs/architecture/evidence/TB-FE-LEGACY-GRID-FIELD-CONTRACT-001/typecheck-after-tsc.txt. Expect 28 -> 26 with zero new diagnostics, but report actual result honestly. Run at most one directly relevant existing focused landing-composer test/guard if discoverable in seconds; if existing baseline test fails for an untouched reason, record proof and do not chase it. Do not invent or amend ceremonial source guards for a two-signature edit. Review diff for unchanged runtime logic.

Evidence: concise docs/architecture/evidence/TB-FE-LANDING-COMPOSER-DRAG-EVENT-TYPES-001/summary.md plus minimal typecheck output/test result; persist EXACT task receipt to docs/ai/tasks/TB-FE-LANDING-COMPOSER-DRAG-EVENT-TYPES-001.task.md. Commit and push only if safe and scope-clean; do not force-push or reset. On unexpected concurrent change STOP and report.

Worker result: return ONLY PIPELINE-PROTOCOL: BRIDGE-WAKE-V1 and canonical BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT, with base/final SHA, changed files, tests actually run, compiler delta, remaining issues and honest known limitations. End STOP. Automatic-Next-Implementation-Task: NONE. Never select or implement a successor, never run a repair loop. Architect reviews acceptance before issuing the next task.
END_TOOBA_TASK
