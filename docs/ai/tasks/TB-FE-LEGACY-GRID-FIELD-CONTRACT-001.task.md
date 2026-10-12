PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-LEGACY-GRID-FIELD-CONTRACT-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE
Repo: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: e41732c0077af49d33565811199bc8bd92387ca1

Architect framing: Act as a senior software architect. Typecheck repair must not sacrifice frontend or backend contracts, grid behavior, security, or architecture. This is one bounded task, not a repair loop. Architect estimate 15-20 minutes; Bridge's existing hard 15-minute execution limit takes precedence. Aim for 10-12 minutes; stop INCOMPLETE if the budget or evidence boundary is reached. Do not run lengthy or unrelated tests.

Objective: Resolve ONLY the remaining TS2322 at src/frontend/design-system/app-data-grid/legacy-grid-bridge.ts:71, where a legacy GridColumnDef<T>.id string is assigned to AG Grid ColDef<T>.field of type ColDefField<T>. Preserve existing user-visible sorting, filtering, export and grid row rendering semantics.

Required reads: AGENTS.md; .cursor/skills/tooba-frontend-feature/SKILL.md; docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md; exact source of legacy-grid-bridge.ts, legacy-grid-bridge.test.ts, AG Grid ColDef field/valueGetter contract, and nearby real callers. Read only relevant sections; do NOT read the opt-in findings register.

Preflight: fetch origin; require HEAD == origin/main == Expected-Starting-HEAD; do not disturb pre-existing untracked artifacts. If diverged, STOP with evidence.

Architectural decision gate: Before editing, determine whether field is needed by AG Grid at runtime for sorting, filter, export, data access, or state persistence given this bridge uses accessor-based cellRenderer and explicit colId. Choose the smallest type-sound design backed by that evidence. Do NOT use any/unknown-as, broad cast, ts-ignore, optionalize shared generic contracts, or silently remove field if it would change behavior. If runtime semantics cannot be established safely, STOP with BLOCKED_ARCHITECT_DECISION and report the precise alternatives. No speculative implementation.

Allowed production scope: src/frontend/design-system/app-data-grid/legacy-grid-bridge.ts only. Minimal focused amendment to existing src/frontend/design-system/app-data-grid/legacy-grid-bridge.test.ts if needed. Task receipt and short evidence under docs/architecture/evidence/TB-FE-LEGACY-GRID-FIELD-CONTRACT-001/. Any other production file requires STOP and Architect decision.

Forbidden: backend/schema/API changes; storefront Home/PDP/Shopeiva edits; broad grid refactor; unsafe casts or compiler suppression; tsconfig/CI/baseline/guard changes; other TypeScript clusters; AMSC; self-created follow-up tasks; retries/test loops; forced push or resetting others' changes.

Verification: one direct frontend tsc --noEmit run; record baseline 29 and actual residual count plus introduced diagnostic count. Run only the relevant existing bridge test (node --experimental-strip-types --test design-system/app-data-grid/legacy-grid-bridge.test.ts) and, only if justified, one other directly relevant focused test. Do not run all frontend tests, build, or backend tests. Clearly distinguish tests not run and any pre-existing failure. Do not change an unrelated failing assertion.

Deliverable: If safe, commit/push bounded diff on main, document actual contract rationale, before/after errors, focused tests, exact changed files, HEAD == origin/main and rollback. Cursor saves this exact task as docs/ai/tasks/TB-FE-LEGACY-GRID-FIELD-CONTRACT-001.task.md. If blocked, return evidence without a speculative commit. Return ONLY the canonical BRIDGE-WAKE-V1 BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT envelope; automaticNextImplementationTask = NONE; STOP. No auto-next work.
END_TOOBA_TASK
