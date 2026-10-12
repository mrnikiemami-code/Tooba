PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-PREVIEW-FILL-REAL-REVIEW-FIXTURE-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE
Repo: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: 40c1526afb1cd52704d36cb5ef6d9811399a50c8

OBJECTIVE
Fix exactly one pre-existing TS2322 at src/frontend/app/admin/landing-pages/admin-builder-ux-r8.guard.test.ts:69: assigning false to previewFake typed as the literal true by createFakeReview. Preserve meaningful characterization of a REAL review followed by fake fill. Expected compiler count 18 -> 17, no new diagnostics.

PREFLIGHT
Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md and docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md. Confirm HEAD == origin/main == expected SHA and no conflicting tracked changes. Do NOT access the deferred architecture findings register.

REQUIRED INVESTIGATION AND STOP GATE
Read full target test, createFakeReview and PreviewFakeMarked declarations in lib/storefront-composition/preview-fake-data.ts, applyPreviewFill and StorefrontFeaturedReviewItem contracts. Confirm why a factory-created fake is inappropriate as a mutable real fixture. Find a truthful type-safe way to construct a real review fixture while retaining necessary real review properties (e.g. a copy with previewFake: false and distinct publicId, typed against the legitimate consumer input contract). Do not falsify production type semantics by casting or weakening PreviewFakeMarked.
If the real item is not admissible by the production policy or its legitimate type cannot be expressed cleanly without a product or architectural decision, STOP as BLOCKED_ARCHITECT_DECISION, report the concrete evidence, and make no speculative fix.

ALLOWED CHANGES
Only src/frontend/app/admin/landing-pages/admin-builder-ux-r8.guard.test.ts (plus task receipt and minimal evidence). Preserve ALL assertions, especially realCount=1, first item real and fake remainder. No production source edit. No weakening/removing tests, no any, broad casts, unknown-as, ts-ignore, non-null assertion added to conceal issues, or type suppressions. Do not edit locked Shopeiva Home/PDP, storefront composition, backend, schemas, tsconfig, CI or baseline; no broad test suite, AMSC, or unrelated repairs.

VERIFICATION
Run exactly one frontend tsc --noEmit after change and compare normalized diagnostic identities against prior committed docs/architecture/evidence/TB-FE-CONTENT-ACTIONS-SLOTS-CONTRACT-001/typecheck-after-tsc.txt. Expect 18 -> 17, target diagnostic removed, zero new diagnostics. Run only the target focused test file with existing node --experimental-strip-types --test; if existing unrelated failures occur, disclose and do not chase. Do not claim unrun tests pass. Record precise test pass/fail counts.

DELIVERY
Commit exact task receipt as docs/ai/tasks/TB-FE-PREVIEW-FILL-REAL-REVIEW-FIXTURE-001.task.md and minimal evidence under docs/architecture/evidence/TB-FE-PREVIEW-FILL-REAL-REVIEW-FIXTURE-001/ (summary, typecheck-before, typecheck-after-tsc, focused-tests). Commit and push main normally, verify HEAD == origin/main. Report base SHA, final SHA, changed files, typecheck before/after, focused tests, contract rationale, pre-existing failures, STOP. No automatic next task.
END_TOOBA_TASK
