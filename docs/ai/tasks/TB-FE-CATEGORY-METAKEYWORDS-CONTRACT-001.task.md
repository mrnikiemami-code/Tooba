PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-CATEGORY-METAKEYWORDS-CONTRACT-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: a4703078913759304f4fc2289c8480783d975317

Architect intent: Make the existing Admin category meta-keywords UI and its imported API helper contract agree without changing persistent wire semantics. Approach this as a senior software architect: repair the true owner, preserve frontend/backend boundaries, do not trade architecture or UX for green TypeScript.

Preflight: fetch origin; require HEAD == origin/main == Expected-Starting-HEAD or STOP and report divergence. Preserve any pre-existing untracked artifacts and active processes. Read AGENTS.md, the frontend skill and frontend architecture standard as required for frontend work; do not read deferred findings register. Inspect category-admin-screen.tsx, catalog-category-api.ts and category-admin-form.test.ts, specifically the existing MetaKeywordsTagInput/MetaKeywordsViewChips usages, API DTO metaKeywords as string|null, and tests defining Persian/ASCII comma, newline and duplicate behavior.

Allowed production scope: src/frontend/app/admin/catalog-category-api.ts and, only if truly necessary, src/frontend/app/admin/category-admin-screen.tsx. Allowed focused test scope: existing src/frontend/app/admin/category-admin-form.test.ts. Restore the missing parseMetaKeywords and serializeMetaKeywords named helper contract at its correct owner if justified by actual UI semantics; alternatively use an already-existing canonical helper if one is truly available and compatible. Preserve stable order, trimming, empty removal, Persian/ASCII comma/newline splitting, duplicate handling and the serialized string payload contract; do not invent a new backend structure. No unrelated category CRUD or translations redesign.

Acceptance: four known TS diagnostics from the stale meta-keyword API (two missing exports and two test import/member errors), plus any directly causal implicit-any diagnostic, are resolved without new diagnostics. The removable chip UI still uses the same string|null backend DTO. Validate with ONE frontend TypeScript check and ONLY the focused category-admin-form test plus the essential frontend architecture check if required by the skill and reasonably fast. Avoid full suites and unrelated failing guards. Record exact baseline vs post-change diagnostic counts and honest test results, including any pre-existing failures; no repeated tests to chase existing failures.

Forbidden: any, type assertions used to mask drift, ts-ignore, TS config or baseline changes, test weakening/deletion, changed backend contracts, schema/migrations, Home/PDP or locked Shopeiva surfaces, unrelated refactors, new dependencies, register reads/writes, AMSC waves, automatic subsequent tasks, force pushes, process termination. Stop if product semantics conflict or if fix requires contract changes outside scope; describe the decision instead of guessing.

Timebox: Architect estimates this as a small approximately 15-minute work unit. Worker does not need to keep elapsed-time accounting; keep work sharply bounded and stop with INCOMPLETE rather than start an unbounded investigation. Current canonical Bridge execution ceiling remains 15 minutes, aiming around 10-12 minutes. No retry cascade or self-expanded repair.

Evidence: commit the exact task receipt at docs/ai/tasks/TB-FE-CATEGORY-METAKEYWORDS-CONTRACT-001.task.md and concise evidence at docs/architecture/evidence/TB-FE-CATEGORY-METAKEYWORDS-CONTRACT-001/ describing source proof, patch, diagnostics, tests and remaining risks. If safe, commit and push one bounded commit to main. Return only canonical BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT with BASE/FINAL SHA, scope, checks and remaining diagnostic count. automaticNextImplementationTask = NONE. STOP for Architect review; do not begin another task.
END_TOOBA_TASK
