PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-105
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-104
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_CATALOG_PUBLISH_READINESS_DIAGNOSTICS
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 0ac71b2b69d60f5366ee56854531c76c984a121e
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD. At minute 22 stop investigation/coding, reserve >=8 minutes for narrow tests/evidence/commit/push. Do not exceed timebox for cleanup. No automated next task.

ARCHITECT RULING
104 BLOCKED accepted. The existing CatalogDemo seed-only and reset-and-seed are draft-only by construction; neither yields a sellable offer. A 545-product seed MUST NOT be repeated. The first actionable unknown is which ProductPublishReadiness.Missing[] rule rejects CatalogAttributeSchemaDevelopmentSeed's scratch-only product. This is a DIAGNOSTIC task, not a seed-bypass or architecture redesign. Catalog retains sole authority. Preserve all publication invariants; no direct EF/raw DML fixture and no changing publish eligibility simply to make a test pass.

PREFLIGHT — STOP ON CONFLICT

Claim exactly one pending task through BRIDGE-WAKE-V1 on tooba-main; save receipt docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-105.task.md UNSTAGED. Read AGENTS.md, pipeline protocol, 102–104 reports and evidence, CatalogAttributeSchemaDevelopmentSeed.cs, CatalogDirectory.PublishProductAsync / UnwrapLifecycle, ProductPublishReadinessReader, ProductPublishRules and existing Catalog tests.
Fetch origin/main; require Expected-Base-SHA ancestor and source nonoverlap. Only docs/result bookkeeping drift is acceptable. Preserve all pre-existing modified/untracked files; never reset/stash/rebase/force-push.
Establish Development :5088/:3000 provenance and tooba_alpha counts READ-ONLY. Do not restart, enable gates, or write to shared DB.

MISSION A — PRECISE CATALOG-OWNED DIAGNOSTIC (MAX 15 MINUTES)

Map the actual type and semantics of ProductPublishReadiness.Missing[] and find the exact point where the Development seed currently loses its diagnostic codes and emits only «محصول برای انتشار آماده نیست». Distinguish validation failure from infrastructure failure without guessing.
Add the SMALLEST diagnostic improvement ONLY in the Catalog Development seed path (preferred): on a rejected PublishProductAsync, query the existing readiness reader/contract for THAT product and emit the existing missing-rule codes, product reference (non-sensitive), and seed stage to Development-only structured logs or a sanitized, bounded diagnostic exception. Preserve original exception classification and public HTTP error contracts. Do not log access tokens, customer data, connection strings, unbounded entity payloads or new error details in production. If diagnostics are already available through an existing API, use it and make ZERO source edits.
No new publish bypass, no turning off required media/SEO/attributes/variants/category checks, no new raw persistence, no product-force-publish, no new generic seed service. No changes to Order/Fulfillment/Shipping/Checkout/Payment/Settlement/Host composition or appsettings. Do not alter Catalog's global error handling as a shortcut.
Add at most 1–3 focused Catalog tests proving that a rejection surfaces actual Missing[] codes (not invented strings), that the original refusal remains a refusal, and that the diagnostic is disabled/redacted outside Development. Use current project's test conventions, no sprawling fake test infrastructure.

MISSION B — OPTIONAL TINY ISOLATED REPRODUCTION ONLY IF SAFE (MAX 5 MINUTES)

IF there is an already supported short, bounded reproduction of the ONE failing CatalogAttributeSchemaDevelopmentSeed product, use a disposable isolated scratch target and get the actual missing codes. Apply the isolation guard and positive /__platform-commerce provenance BEFORE contact; secrets ephemeral. Hard timeout 5 minutes ONCE. If the only reproduction starts the full Development legacy bootstrap or Catalog demo seed of hundreds of products, DO NOT RUN IT; finish with unit/source-level diagnostic proof and label the exact real Missing[] value NOT YET OBSERVED.
No shared DB DML, no shared Host/Next restart, no catalog reset-and-seed/seed-only call, no permission bypass, no direct raw SQL fixtures. If scratch was used, guarantee teardown and recheck shared parity before final result.

MISSION C — ACCEPTANCE AND HANDOFF

Run focused Catalog tests only. Record the changed code paths, actual failing prerequisite codes ONLY if observed, logging/redaction proof, measured time, real SHA, no-write evidence, and exact next corrective action. If technical constraints make safe diagnostic implementation impossible within the timebox, STOP with BLOCKED and evidence rather than broaden scope.
Commit/push task-owned Catalog diagnostics/tests and docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-105.md + docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-105/**. Do not touch master recovery unless explicitly allowed. Receipt stays UNSTAGED. No manufacturing of a sellable offer or storefront projection evidence.
PASS = a focused Catalog-owned diagnostic path emits verified true readiness-missing codes in a supported test/reproduction while rejecting publish as before, and tests green; PARTIAL = safe diagnostic implemented/tested but actual failing scratch seed rule cannot be reproduced within the timebox; BLOCKED = no safe narrow diagnostic seam or tests, no risky changes. STOP_FOR_ARCHITECT in all cases.

ALLOWED PATHS

Existing Catalog Development seed and readiness diagnostics under src/backend/Modules/Catalog/**, only where strictly required, plus focused Catalog tests.
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-105.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-105/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-105.task.md (UNSTAGED receipt ONLY)
FORBIDDEN: any other src module, Host, migrations, shared database writes, global publishing rule relaxation, broad seeds, configuration commits, automatic next implementation task.

RESULT CONTRACT — BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-105
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-104
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_CATALOG_PUBLISH_READINESS_DIAGNOSTICS
Bridge-Task-Row: <assigned on claim>
Status: PASS|PARTIAL|BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 0ac71b2b69d60f5366ee56854531c76c984a121e
Base-SHA: <actual>
Code-SHA: <commit or NONE>
Evidence-SHA: <commit>
automaticNextImplementationTask: NONE
Report: read paths, actual diagnostic codes vs not-yet-observed, test results, elapsed minutes, all touched files, database/process parity, rollback, exact next decision. Do not claim storefront/Checkout readiness.
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK