PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-CONTENT-CATEGORY-MEDIA-PREVIEW-NULL-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE
Repo: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: fcc96e37a02bf9c430d1b409bc00d3d59a34da23

OBJECTIVE
Fix exactly one remaining diagnostic: src/frontend/app/admin/content-category-admin-screen.tsx(507,26), TS2322, because mediaPreviewUrl(...) returns string | null while the <img> src property accepts string | undefined. Maintain the real media-preview behavior and empty/invalid-media handling, not merely compiler silence. Expected typecheck reduction: 22 -> 21, no new errors.

REQUIRED PREFLIGHT
Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md and docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md. Confirm clean tracked worktree and HEAD == origin/main == Expected-Starting-HEAD; otherwise STOP and report. Read the full relevant component, the mediaPreviewUrl owner at src/frontend/app/admin/media-api.ts, its callers/tests, and the exact compiler diagnostic. Do not access the deferred findings register.

ALLOWED PRODUCTION SCOPE
Only src/frontend/app/admin/content-category-admin-screen.tsx. Minimal existing relevant focused test amendment permitted ONLY if it meaningfully asserts behavior; otherwise run an existing focused test without adding ceremonial assertions. Add the exact task receipt to docs/ai/tasks/TB-FE-CONTENT-CATEGORY-MEDIA-PREVIEW-NULL-001.task.md and concise evidence to docs/architecture/evidence/TB-FE-CONTENT-CATEGORY-MEDIA-PREVIEW-NULL-001/.

DECISION GATE
Establish what a null mediaPreviewUrl means in the real helper, whether a mediaAssetId can be present while the URL is null, and how the current media tab represents absent media. Prefer a locally derived, typed preview URL and render the img only for a usable string URL, while preserving the existing no-image fallback and selection controls. The exact implementation is yours to choose based on the actual code; do not use !, as string, any, ts-ignore, unsafe casts, an empty src that triggers document requests, or an unrelated media contract change. If the user-visible empty/failure behavior is ambiguous and requires a product decision, STOP as BLOCKED_ARCHITECT_DECISION without implementing a guessed behavior.

PRESERVE
Content category forms, media selection/saving, workspace ID, alt/text and visual CSS, existing URLs, permissions and tabs. No changes to mediaPreviewUrl's signature or shared media API; no backend/schema changes, no storefront/Home/PDP/Shopeiva locked changes, no tsconfig/baseline/guard edits, no unrelated fixes, no AMSC or broad test suites.

VALIDATION
One frontend node_modules/.bin/tsc --noEmit after editing. Compare diagnostic lines against committed baseline docs/architecture/evidence/TB-FE-ADMIN-RECEIPTS-ACTIONS-COLUMN-WIDTH-001/typecheck-after-tsc.txt. Expected only this TS2322 disappears, 22 -> 21. Run just the nearest relevant existing content-category/media focused tests, report exact pass/fail and preexisting failures honestly; no test retry cascade. If you add a test, it must meaningfully verify the null-URL behavior rather than text-matching an implementation detail.

EVIDENCE AND DELIVERY
Commit source, any justified focused test, receipt and concise evidence including before/after tsc diagnostics and focused test output. Push to origin/main only after verifying scope and HEAD; do not force push, reset, stash or overwrite concurrent changes. Report changed files, actual decision, exact diagnostic count, test outcomes, commit SHA, HEAD==origin/main and any residual risk. STOP after this task; do not generate or run another implementation task.
END_TOOBA_TASK
