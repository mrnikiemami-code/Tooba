PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-RESERVATION-POLICY-NUMERIC-FIELD-KEY-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE
Repo: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: fd2ad0e82ed916a515756dacc876151605358353

OBJECTIVE
Resolve exactly the TS2322 at src/frontend/app/admin/reservation-policy-editor.tsx:112, where an HTML text input receives string | boolean from draft[key]. Preserve all reservation-policy values, inherit toggles, UI and save semantics. Target baseline: 21 TypeScript errors; expected after: 20 with no new errors.

PREFLIGHT AND ARCHITECTURE
Verify HEAD == origin/main == Expected-Starting-HEAD and no conflicting tracked changes; otherwise STOP and report. Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md, docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md, this component and relevant reservation-policy-api and focused tests. Do NOT read or update deferred findings register. Do not touch unrelated untracked artifacts.

ROOT-CAUSE HYPOTHESIS TO VERIFY
ReservationPolicyDraft has three string-valued fields initial/retry/max and three boolean inheritInitial/inheritRetry/inheritMax fields. The fields array currently declares key: keyof ReservationPolicyDraft, which allows boolean keys at compile time even though the three actual descriptors always use string keys. Therefore draft[key] becomes string | boolean, incompatible with input.value. Verify the actual field descriptors and their runtime consumers before editing.

ALLOWED REPAIR
Production scope ONLY src/frontend/app/admin/reservation-policy-editor.tsx. Prefer a locally owned precise type for numeric-text field keys, for example type ReservationPolicyValueKey = "initial" | "retry" | "max", and narrow the fields descriptor key to that type; derive from the canonical draft shape where sensible. Keep the descriptors, checkbox inherit-key mapping, draft values, change handlers and save contract unchanged. Do not cast draft[key] as string, do not stringify booleans, and do not change ReservationPolicyDraft's true shape. If the declared mapping does not support this repair without altering runtime behavior, STOP as BLOCKED_ARCHITECT_DECISION and explain.

TESTS AND ACCEPTANCE
Run exactly one cd src/frontend && node_modules/.bin/tsc --noEmit after the final edit; save the full raw diagnostics. Compare with the exact prior evidence docs/architecture/evidence/TB-FE-CONTENT-CATEGORY-MEDIA-PREVIEW-NULL-001/typecheck-after-tsc.txt. Require only the targeted TS2322 gone and no new diagnostics (21 -> 20 expected). Run one directly relevant existing reservation-policy focused test or guard if available, no full suite, build, browser, or test loop. A source-only guard is corroborating, not behavioral proof. Do not add ceremonial tests for erased typing; existing tests are preferred.

FORBIDDEN
No backend, DTO/schema, business-policy, inventory reservation, storefront/Home/PDP/Shopeiva change. No type suppression, any, unsafe cast, ts-ignore, tsconfig/CI/test-baseline or architecture guard change. No cross-module cleanup, automatic follow-up, broad tests, AMSC, stash/reset/force-push.

EVIDENCE AND HANDOFF
Add exact task receipt at docs/ai/tasks/TB-FE-RESERVATION-POLICY-NUMERIC-FIELD-KEY-001.task.md and concise evidence under docs/architecture/evidence/TB-FE-RESERVATION-POLICY-NUMERIC-FIELD-KEY-001/ (summary.md, typecheck-before.txt, typecheck-after-tsc.txt, focused-tests.txt). Include exact changed files, baseline/after counts, proof of no new errors, test passes/failures, existing failure disclosure, runtime invariants, risk and rollback. Commit and push to origin/main only after bounded checks, then verify HEAD == origin/main, STOP for independent Architect review. Do NOT begin another task.
END_TOOBA_TASK
