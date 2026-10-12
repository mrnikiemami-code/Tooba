PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-FASHION-PREVIEW-ORIGIN-TYPE-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE
Repo: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: d62b1ea2ed8e29124f2d4364e1605cd9f1046fc7

GOAL
Resolve exactly the TS2345 in src/frontend/app/template-preview/fashion/fashion-template-preview-view.tsx:60, where setOrigin(result.origin) accepts only the two literal initial-origin values but loadFashionTemplatePreview declares origin: string. Respect the locked Fashion preview / Shopeiva storefront provenance, sample-versus-store isolation, and rendering contract. This is a type-contract repair, NOT permission to weaken provenance semantics.

MANDATORY PRE-FLIGHT

Confirm clean tracked worktree, HEAD == origin/main == Expected-Starting-HEAD. STOP on divergence or conflicting changes.
Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md, docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md and applicable locked-surface guidance in docs/architecture/TOOBA-LOCKS.md. Do not read deferred ARCHITECTURE-FINDINGS-REGISTER.md.
Examine full fashion-template-preview-view.tsx, the sourceMode contract, FASHION_DEMO_ORIGIN, FASHION_STORE_ORIGIN, the declared return types and implementations of loadFashionTemplatePreview and loadFashionStorePreview in lib/storefront-composition/fashion-demo-preview.ts, and all meaningful origin consumers/tests. Identify whether arbitrary origin string is a legitimate API provenance value or should be constrained by a canonical producer mapping. Cite exact source locations in evidence.

DECISION GATE (MANDATORY)
Choose the smallest TRUTHFUL remedy, using verified contract rather than a cast. If result.origin is legitimately a string (sample endpoint can return varying values), it may be correct for this display-only state to be typed string, but prove that origin is used only as provenance display/test metadata, not a source authorization or sample/store trust discriminator. If origin is contractually constrained to exactly two values, consider producer-side validation only if this is explicitly established, authorized, and demonstrably no change to store/sample sourcing. Do not invent a provenance fallback or coerce untrusted values into a trusted origin marker. If unresolved or requiring a product/security decision, STOP as BLOCKED_ARCHITECT_DECISION with no production edit.

BOUNDED SCOPE
Preferred only production file: src/frontend/app/template-preview/fashion/fashion-template-preview-view.tsx. Do not edit loaders, source selection, store/sample fixture logic, rendering, context, data attributes, backend DTO, SSR, locale behavior, locked Home/PDP/Shopeiva surfaces, or shared types. Preserve data-demo-origin behavior for all previously accepted runtime values. Permit the smallest existing directly relevant focused test amendment ONLY if it tests a genuine provenance boundary rather than source-text ceremony.

FORBIDDEN
No any, as unknown as, broad assertion/cast, ! mask, ts-ignore, suppression, tsconfig/CI/baseline/guard relaxation, ungrounded fallback, broad refactor, AMSC, full test/build suite, automatic next task, history rewrite, force-push, reset, stash, or touching unrelated errors.

VERIFICATION
Run frontend node_modules/.bin/tsc --noEmit ONCE after final edit. Compare against 16 diagnostics captured in docs/architecture/evidence/TB-FE-CATEGORY-EDIT-SURFACE-TAB-MAPPING-001/typecheck-after-tsc.txt. Expected 16 -> 15, no added diagnostics; normalize comparison by file/line/code if TS union print order changes. Run only the closest existing fashion preview/provenance guard tests, without repeated cascades. Disclose pre-existing failures and unrun tests honestly. Provide proof that sample/store source selection and provenance marker output semantics are retained.

DELIVERABLE
Commit and push only the scoped repair, task receipt at docs/ai/tasks/TB-FE-FASHION-PREVIEW-ORIGIN-TYPE-001.task.md, and bounded evidence at docs/architecture/evidence/TB-FE-FASHION-PREVIEW-ORIGIN-TYPE-001/ (summary.md, typecheck-before.txt, typecheck-after-tsc.txt, focused-tests.txt). Include exact commit SHA, changed paths, count, tests, any real behavior delta, risks and rollback; HEAD == origin/main confirmation. If blocked, provide precise STOP gate reason; do not manufacture a green outcome.

STOP. Do not start any follow-on task; await independent Architect ACCEPT.
END_TOOBA_TASK
