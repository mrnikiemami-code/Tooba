PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-ADMIN-MENUS-HELPER-SIGNATURES-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: eebb0a09ebbbea362a06f0f6d1d624724478364c

Architect intent: Act as a senior software architect, preserving Frontend and Backend ownership, contracts and behavior. Repair the root cause; never trade architectural integrity for a passing compiler.
Timebox: Architect estimates ~10-12 minutes of focused worker effort, hard STOP by the authoritative 15-minute Bridge ceiling. No worker timer requirement; do not run loops, retry cascades, broad suites, or exploratory refactors. If unsafe or cannot finish coherently, return INCOMPLETE and STOP.

Goal: Resolve only the three currently recorded TypeScript diagnostics in src/frontend/app/admin/menus/admin-menus-api.ts (near lines 84, 90, 91) caused by outdated calls to shared adminHeaders, parseAdminProblemErrorCode and mapAdminErrorMessage. Preserve menu request authentication, CSRF, response errors and localized customer/admin-facing messages. Correct current signatures by inspecting their implementations and real neighboring callers; do not blindly change arguments to satisfy TS.

Required reads: AGENTS.md; relevant .cursor/skills/tooba-frontend-feature/SKILL.md; docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md (targeted applicable sections); src/frontend/app/admin/menus/admin-menus-api.ts; src/frontend/app/admin/admin-api.ts adminHeaders; src/frontend/app/admin/admin-error-map.ts parseAdminProblemErrorCode/mapAdminErrorMessage; directly relevant focused tests. Do NOT read architecture findings register (opt-in only).

Allowed production change: src/frontend/app/admin/menus/admin-menus-api.ts only. Focused existing Admin Menus test(s) may be adjusted or supplemented only if necessary for a behavior-based regression; explicitly justify any such file. Do not refactor shared admin helpers or create competing error/locale contracts. Check whether mutation requests require CSRF initialization; use existing canonical helpers rather than inventing an authentication protocol. Check the expected locale and error-code fallback contract; avoid hardcoded Persian fallback when a canonical locale is available, but STOP and escalate if a change would require new cross-module i18n behavior or product decision.

Forbidden: any Backend, schema, migration, global type, tsconfig, baselines, gates, Home/PDP/Shopeiva, unrelated Admin modules, broad localization rewrite, any/ts-ignore/unsafe cast, suppressions, AMSC, automatic next task, process disruption.

Verification: run ONE TypeScript check after change; compare with the committed 41-error baseline at docs/architecture/evidence/TB-FE-MIDDLEWARE-LOCALE-TYPE-IMPORT-001/typecheck-after-tsc.txt; expected 41 to 38 if exactly 3 errors removed and no new errors. Run only directly relevant existing focused menu/API tests and, if required, one directly relevant architecture guard; do NOT run full npm tests/build or repeat failing baseline tests. Report failures faithfully, no masking. Inspect exact diff and HEAD/origin/main before commit/push. If another worker advanced HEAD, STOP and report; do not rebase blindly.

Delivery: commit a narrowly scoped change and exact receipt docs/ai/tasks/TB-FE-ADMIN-MENUS-HELPER-SIGNATURES-001.task.md; add concise evidence under docs/architecture/evidence/TB-FE-ADMIN-MENUS-HELPER-SIGNATURES-001/. Push only if safe under normal main governance. State SHA, source-grounded rationale, changed paths, tests executed (and not executed), before/after TS totals, pre-existing failures and any uncertainty. Return only canonical PIPELINE-PROTOCOL: BRIDGE-WAKE-V1 / BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT with Status and verdict, then STOP. automaticNextImplementationTask=NONE.
END_TOOBA_TASK
