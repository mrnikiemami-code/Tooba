PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-ADMIN-GRID-RICHTEXT-TYPE-BATCH-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_MULTI_ERROR_REPAIR_WITH_INDEPENDENT_STOP_GATES
Repo: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: 286489b74fba6520299f9dbb66d81bec2935e6ab

GOAL
Clear up to FOUR existing TypeScript diagnostics in two independent Admin areas in ONE bounded worker task and ONE commit. Do not chase other 10 storefront/composition diagnostics. This replaces one-error-per-task workflow for safe clusters. Hard stop at 15 minutes. Prefer 10-12 minutes total. If an area is unsafe, mark that area BLOCKED with evidence and proceed only with the other independently safe area; never fake green.

MANDATORY PREFLIGHT
Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md, docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md and relevant contracts. Confirm clean tracked worktree, HEAD==origin/main==Expected-Starting-HEAD; STOP on mismatch. Do not read/change deferred ARCHITECTURE-FINDINGS-REGISTER.md. Avoid stash/reset/rebase/force-push.

SCOPE A: LANGUAGE GRID, THREE DIAGNOSTICS
File: src/frontend/features/admin-languages/components/language-list.tsx
TS2353 at :41 and :42 for unsupported flex, and at :112 for unsupported pinned on GridColumnDef<AdminLanguageRow>.
BEFORE EDITING trace GridColumnDef, AppDataGrid column mapping, actual pinning behaviour, container sizing, RTL direction and actions column canonical rules. Determine whether flex is used in actual runtime or silently ignored; determine how actions pinning is handled (special actions column ID or canonical option). Choose only contract-truthful implementation that preserves existing column widths, actions access, and RTL pinning. No blind delete of flex/pinned unless demonstrably inert or mapped safely; no loosening shared GridColumnDef just to pass tsc. If pinning intent cannot be retained without a broader architecture change: STOP SCOPE A and report BLOCKED_ARCHITECT_DECISION. No visible layout regression accepted silently.

SCOPE B: TIPTAP EXTENSIONS, ONE DIAGNOSTIC
File: src/frontend/app/admin/product-rich-text-editor.tsx
TS2345 at :162 base.push(Image.configure(...)), because inferred array union excludes Node<ImageOptions,...>.
Inspect actual @tiptap/core Extension / Node / Mark types already in project, the inferred base array, and useEditor extensions contract. Prefer a properly typed common extension-array declaration using library public types, if genuinely correct, while preserving the existing extension ordering, configured image flags and DAM gating. NO casts, any, unknown-as, ts-ignore, suppression, dependency upgrades, HTML serialization changes or weakening image security restrictions. If package version/type ownership mismatch exists and safe local typing is impossible, STOP SCOPE B and report BLOCKED_ARCHITECT_DECISION.

ALLOWED PRODUCTION EDITS ONLY

src/frontend/features/admin-languages/components/language-list.tsx
src/frontend/app/admin/product-rich-text-editor.tsx
Plus task receipt docs/ai/tasks/<Task-ID>.task.md and minimal evidence under docs/architecture/evidence/<Task-ID>/, plus genuinely meaningful focused test changes only in immediately related existing suites when necessary. No storefront files, backend, shared design-system files, tsconfig, CI, lockfiles, guard weakening, locked Home/PDP/Shopeiva, AMSC, broad suites or unrelated refactors.

EXECUTION AND EVIDENCE
Implement safe scopes independently, with concise before/after behavior explanations and explicit decision gate for each. Run frontend node_modules/.bin/tsc --noEmit ONCE after final edits; normalize diagnostics by file/error-code and compare with baseline docs/architecture/evidence/TB-FE-FASHION-PREVIEW-BRAND-LOGO-NULL-001/typecheck-after-tsc.txt. Baseline 14; ideal after 10; only claim actual measured delta, including any added diagnostics. Select small existing directly relevant tests (max ~2 suites per area), run each at most once; disclose pre-existing failures without chasing. Save raw output and summary. Never say typecheck green if errors remain.

DELIVERY
Commit a single bounded commit to main and push only if preflight safe. Include exact SHA, changed files, gate verdict per scope, typecheck before/after by file, focused test pass/fail, runtime/visual impact, outstanding risks, rollback and STOP. If neither scope safely fixable: no production commit, issue BLOCKED report. Do NOT launch the next task automatically.
STOP
END_TOOBA_TASK
