PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-MIDDLEWARE-LOCALE-TYPE-IMPORT-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: 60e2623602bdeeb452f3aa3cc65703595bc6521d

Architect intent: Act as a senior software architect. Fix the real type ownership mismatch; never trade away frontend or backend architecture for an easy green check. This is ONE bounded repair, not a loop or an invitation to clear other TypeScript clusters. Architect estimates the work at less than 15 minutes; do not run a timer. Protocol hard limit remains 15 minutes. If unsafe, blocked or likely to overrun, report INCOMPLETE and STOP.

Issue: src/frontend/lib/i18n/middleware-locale.ts imports type Locale from ./routing.ts, but routing.ts only imports (does not export) that type. The canonical owner is src/frontend/lib/i18n/locale.ts. Baseline contains TS2459 from this import; 42 frontend type errors remain after prior task.

Required reads: AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md, docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md, and the focused i18n files needed for this edit. Do not open the deferred architecture findings register absent explicit instruction.

Preflight: fetch origin and verify clean tracked worktree, expected HEAD == origin/main; preserve existing untracked artifacts. If divergent or another task changed the relevant files, STOP and report; no reset, stash or force push.

Allowed production scope: src/frontend/lib/i18n/middleware-locale.ts only. Import Locale directly as a type from ./locale.ts, retaining the existing runtime imports from ./routing.ts. Keep all runtime behavior identical. Do not re-export Locale from routing.ts solely to conceal incorrect ownership. A focused existing i18n test may be amended only if genuinely needed, with explanation; no new harness.

Forbidden: changes to locale inventory, routing behavior, middleware decisions, URL prefixes, cookies, Accept-Language, fallback, backend, schemas, Shopeiva locked surfaces, tsconfig, test baselines or architecture guards; no any, ts-ignore, broad casts, suppression or unrelated type fixes. No AMSC, no auto-next task, no retry cascade.

Validation (minimal): run frontend tsc --noEmit once after edit and compare diagnostic count and TS2459 against the saved 42-error prior evidence at docs/architecture/evidence/TB-FE-CATEGORY-METAKEYWORDS-CONTRACT-001/typecheck-after-tsc.txt. Run one focused existing middleware/i18n test if present and directly relevant, otherwise skip and explain. Do not run broad test suites or builds. Expected type errors: 42 -> 41; report actual and unexpected diagnostics honestly. If a pre-existing failure arises, record it, do not chase it.

Evidence: brief report under docs/architecture/evidence/TB-FE-MIDDLEWARE-LOCALE-TYPE-IMPORT-001/summary.md including exact change, typecheck before/after, focused tests actually executed, scope and risks. Save this exact task under docs/ai/tasks/TB-FE-MIDDLEWARE-LOCALE-TYPE-IMPORT-001.task.md. Commit and push only the bounded change, task receipt and evidence if preflight and checks permit; no history rewrite.

Worker result: respond only with PIPELINE-PROTOCOL: BRIDGE-WAKE-V1, BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT, including Task-ID, Base/Final SHA, changed files, real diagnostics and test outcomes, deviations and remaining risk, HEAD/origin status. Set Automatic-Next-Implementation-Task: NONE. STOP for Architect review; do not start another task.
END_TOOBA_TASK
