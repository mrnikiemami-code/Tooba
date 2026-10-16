TB-SHIPPING-ONE-LEVEL-TABS-117 — Isolated Runtime Acceptance After Commerce Context Repair
Plain text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-117
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-116
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_DEVELOPMENT_LEGACY_CATALOG_RUNTIME_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 709e474951c0aa6f5481e2ff4658d24c02533dea
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD. Stop investigative work by minute 22; reserve remaining time for cleanup, evidence, commit, and reporting. Exactly ONE isolated Host launch, external watchdog <= 180 seconds. If deadline is reached, return PARTIAL/BLOCKED; do not retry.

ARCHITECT RULING
TB-116 source repair is accepted: the existing DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync gateway establishes a trusted, scope-local CommerceContext before the six legacy Catalog bootstraps. Focused tests 12/12 passed and Host build succeeded. This task is OBSERVATION ONLY, to prove whether the prior platform.edition.unconfigured startup barrier is eliminated in real isolated runtime and discover the FIRST subsequent blocker, if any. No implementation fix or production acceptance claim in this task. The unrelated four pre-existing Host guard failures do not justify broad changes here.

PREFLIGHT — FAIL CLOSED
1. Claim exactly ONE Pending Bridge task on tooba-main and preserve docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-117.task.md UNSTAGED. Read AGENTS.md, Bridge protocol, relevant skills, and TB-113/115/116 evidence.
2. Fetch origin/main and inspect actual HEAD, index, worktree, and untracked files. Expected-Base-SHA must be ancestor; accept only non-overlapping bookkeeping drift. Any ambiguous or overlapping source drift => RECOVERY_CONFLICT / STOP. No reset/stash/rebase/force push; never adopt other task artifacts.
3. Read only shared baseline of Host :5088, Next :3000, commerce provenance, and tooba_alpha method/translation/cart/line/draft/order/product/offer counts. Shared resources are strictly READ ONLY; never start, stop, restart, kill, mutate, or migrate them.

MISSION A — EXACTLY ONE ISOLATED DEVELOPMENT STARTUP
1. Prepare a NEW disposable PostgreSQL instance (no persistent volume) on a unique loopback port, with task-owned application and messaging databases and ephemeral credentials. Use isolated output, run, and content-root paths and an unused loopback Host port. Do not reuse prior task scratch resources.
2. Build/publish the current Host without changing src/ or configuration in the repository. Construct a task-owned effective Development configuration. Verify OFFLINE, before launch, that exactly one ACTIVE Development tenant selected by the canonical gateway exists; database connection references for application and messaging resolve ONLY to the task-owned scratch targets; no shared tooba_alpha, tenant-alpha, shared port or credentials can be reached; RunLegacyBootstraps=true; AllowResetAndSeed=false; ShippingOneLevelCatalog, ShippingRateAuthority, and ShippingOperationalShadow absent/OFF. Any mismatch => STOP, no launch.
3. Launch the isolated Host ONCE with a recorded absolute executable path and exact PID, bounded by an external watchdog <=180 seconds. No retries, no seed-only shortcuts, no extra launch, no HTTP mutation endpoints, no manual SQL writes, no reset-and-seed or 545-product expansion.
4. Record stage-by-stage evidence for: DevelopmentSchemaMigrator complete; entry into the legacy Catalog bootstrap gateway; trusted scoped commerce-context availability before FashionTemplateCatalogSeed resolves CatalogDbContext; whether platform.edition.unconfigured disappears; completion/failure of each of the six legacy seeds; Host listening, GET /health and GET /__platform-commerce if the Host actually stays alive. For a failure, capture the FIRST new exception, stack, source location, and last completed stage. DO NOT repair it.
5. On SCRATCH ONLY and read-only, inspect migration state; schema-mobile-demo-phone Published with exactly two sparse variants and one default; active offers, active Base prices, positive available stock; shared color-definition identity across schema and legacy workspace products; new template/landing/menu rows if reached. Row presence does not prove storefront eligibility. Do not create carts/orders or activate shipping feature flags.

MISSION B — CLEANUP AND NON-INTERFERENCE
1. Stop only the precisely recorded, task-owned Host PID after verifying its absolute executable path, if still running. NEVER stop processes by name/wildcard, and never touch shared :5088 or :3000 processes.
2. Remove only task-owned disposable database container and ignored build/run directories (including transient credentials). Verify task-owned ports are no longer listening and no scratch instance persists.
3. Compare shared postflight PIDs, health/provenance, and tooba_alpha read-only counts with preflight. Any deviation => INCIDENT and STOP, without silent remediation.

MISSION C — EVIDENCE AND STOP
1. Write concise findings to docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-117.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-117/**. Include offline isolation guard, single-launch watchdog, redacted exception/stage excerpt, scratch row-presence readback, shared parity, measured runtime, cleanup, and rollback. Never commit passwords, tokens, connection strings, full SQL logs or customer identifiers.
2. Commit and push ONLY this task's docs/evidence. Zero src/, test, migration, application configuration, frontend, shared DB or shipping flag modifications. Preserve Bridge receipt UNSTAGED. Verify HEAD == origin/main after push. STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.

FORBIDDEN
No source repairs in TB-117; no second launch; no shared runtime or DB writes; no alternate tenant/database fallback; no direct DbContext/SQL seed writes; no fake HttpContext; no feature flag activation; no broad tests; no process name/wildcard termination; no automatic TB-118.

ACCEPTANCE
PASS only if the exact former platform.edition.unconfigured barrier is gone, all legacy Catalog bootstrap stages complete, the isolated Host reaches /health 200 with verified scratch provenance, and cleanup plus shared parity succeed. If the former barrier disappears but a subsequent error prevents health, return PARTIAL and identify its first exact cause. If the original barrier recurs, return BLOCKED with evidence. Do not claim storefront labels, cart reachability, checkout cutover or commercial shipping readiness from this startup.

RESULT CONTRACT — BRIDGE-WAKE-V1
Report Task-ID/Parent-Task/Channel/Worker-Id/Agent-Type/Bridge-Task-Row, Expected-Base-SHA, actual Base-SHA and drift, Code-SHA NONE, Evidence-SHA, Status PASS/PARTIAL/BLOCKED, Stop-State STOP_FOR_ARCHITECT, automaticNextImplementationTask NONE; measured minutes; single launch/watchdog; stage-by-stage findings; first failure or verified health/provenance; scratch readback; shared parity; changed paths; cleanup; rollback; deviations; first remaining blocker. STOP FOR ARCHITECT.
END_TOOBA_TASK