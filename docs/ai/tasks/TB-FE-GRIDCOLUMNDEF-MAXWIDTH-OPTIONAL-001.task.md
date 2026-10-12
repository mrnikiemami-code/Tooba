PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-GRIDCOLUMNDEF-MAXWIDTH-OPTIONAL-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Mode: IMPLEMENT_BOUNDED_REPAIR
Branch: main
Starting-Reference-SHA: 3fda1a8c01b644a180c881abaaa4287fadefe5d2
Expected-Work-Time: 10-12 minutes; hard stop by 15 minutes under current Bridge protocol. Do not loop or automatically create another task.

Senior-Architect-Mandate:
Treat this as a shared type/API contract repair, not as silencing TypeScript errors. Preserve frontend architecture, design-system ownership, backend boundaries, Shopeiva locks and real runtime behavior. No weakening of compiler or test gates. Worker PASS is not Architect ACCEPT.

Preflight:

Recover main safely; fetch origin/main; verify HEAD == origin/main, current task is active, and worktree is safe. If diverged, STOP with RECOVERY_CONFLICT; do not force-push, stash/pop, overwrite others or automatically rebase unsafe changes.
Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md, docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md and audit summary docs/architecture/evidence/TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001/summary.md. Do not read the opt-in architecture findings register.
Inspect exact GridColumnDef type, AG Grid bridge, internal DataGrid resize clamp function and focused tests before editing. Confirm missing maxWidth truly means no imposed maximum, not a mandatory design constraint. Check usage of clampWidth with undefined.

Objective:
Remove the false required-maxWidth type contract while preserving explicitly capped columns and uncapped action-column behavior. The audited baseline is 155 errors; 96 are attributed to this single root. Actual outcome must be measured, not assumed.

Permitted Files (only if necessary):

src/frontend/design-system/data-grid/types.ts
src/frontend/design-system/app-data-grid/legacy-grid-bridge.ts
src/frontend/design-system/data-grid/DataGrid.tsx
src/frontend/design-system/app-data-grid/legacy-grid-bridge.test.ts
Closely-owned focused DataGrid width/clamp test file ONLY if an existing test seam requires it; enumerate and justify any extra file before modifying.
This task's receipt and bounded evidence under docs/architecture/evidence/TB-FE-GRIDCOLUMNDEF-MAXWIDTH-OPTIONAL-001/ and its exact task file under docs/ai/tasks/.

Implementation:

Change maxWidth from required to optional only if confirmed to match existing design-system behavior. Preserve required width/minWidth and all existing semantics.
Ensure all width-clamping/resizing/rendering code behaves correctly without maxWidth: no NaN, no undefined cap coercion, no accidental forced maximum, no visual layout shift for existing capped columns.
Preserve explicitly provided maxWidth; legacy bridge must pass through defined caps only. Avoid broad modifications to 95 individual screen columns.
Add/update focused tests for both uncapped and capped columns and resize behavior as needed, using established test patterns. No parallel design-system implementation.
If actual code demonstrates maxWidth must be required for product behavior or fixing it needs a wider redesign, STOP and report the decision rather than forcing optionality.

Do Not:

Do not touch backend, schema, localization, Admin screens, Home/PDP, SectionDisplayConfig, other typecheck clusters, or architecture findings register.
Do not add any, ts-ignore, casts hiding errors, tsconfig exclusions, global baselines or guard relaxations.
Do not auto-run AMSC, full backend suites, unrelated browser tests or repeated exhaustive tests.

Proof (bounded):

Run cd src/frontend && npm run typecheck once after the change. Report EXACT diagnostic total, count reduced from 155, and any NEW errors (expected near 59, but never assert without measured output).
Run relevant focused legacy bridge/width tests via existing script and npm run test:grid only if its scope/runtime is reasonable; report actual pass/fail and compare known pre-existing failures.
Run npm run test:architecture once if feasible; don't reset baselines. If change touches shared Home/PDP visual primitives, obey critical-storefront contract; otherwise document why critical visual tests are out of scope.
Inspect the exact final diff and verify no visual/runtime change and no unrelated files.
On safe PASS, commit and push to origin/main; fetch and verify HEAD == origin/main. If blocked or timebox exceeded, return INCOMPLETE with evidence and STOP, no continuation loop.

Worker Result:
Return one canonical BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT on tooba-main through approved Bridge transport, including task ID, baseline/final SHA, changed files, actual commands/results, measured typecheck delta, architecture contract justification, risks, stop state. automaticNextImplementationTask = NONE.
END_TOOBA_TASK
