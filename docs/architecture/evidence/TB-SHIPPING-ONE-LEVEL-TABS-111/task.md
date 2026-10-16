PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-111
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-110
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_TWO_PHASE_MIGRATION_RUNTIME_AND_SELLABLE_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: bc8c87e2853ee6958420ed6e31631eca3c6211d0
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
TB-110 fixes DevelopmentSchemaMigrator ordering: migrate ALL registered module schemas in canonical order, THEN execute module-owned post-migration steps in their established order. Focused synthetic tests are green but real empty-database runtime is UNVERIFIED. The exact prior barrier is Npgsql 42P01 missing party.parties when Catalog's post-migration sellable enricher ran before Party migration. This task is ONLY ONE bounded isolated startup to establish whether that barrier is resolved and whether an official sellable offer, price and inventory appear. Do NOT implement any newly discovered fix. Do NOT run the large 545-product reset-and-seed or seed-only endpoint. Do NOT imply storefront label/checkout readiness from counts.

PREFLIGHT — FAIL CLOSED

Claim exactly one Pending task on tooba-main; keep docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-111.task.md UNSTAGED. Read AGENTS.md, pipeline/controller, 106/108/109/110 evidence, 110 two-phase source and the 109 isolated recipe. Fetch origin/main: expected SHA must be ancestor. Classify and STOP on overlapping source drift; only non-overlapping result bookkeeping may be reconciled. Preserve all dirty/untracked paths, no reset/stash/rebase/force-push.
Read-only shared baseline FIRST: verify actual PID, /health and /__platform-commerce for 127.0.0.1:5088 (dev-singlestore / store-alpha / tenant-alpha), listener status :3000, and counts/hashes in tooba_alpha for shipping methods/translations, catalog products/offers, carts, drafts, orders. Never assume PID from 109. Preserve shared Host/Next and database untouched.

MISSION A — ONE ISOLATED RUNTIME PROOF

Create task-owned disposable postgres container/database and messaging DB, no shared volume, loopback-only distinct ports and task-generated credentials under ignored .tmp-111-* paths. Build current-code Host to a separate task-owned output directory. Run OFFLINE EFFECTIVE CONFIG GUARD: exactly one scratch tenant and scratch-only references, NO tooba_alpha/tenant-alpha/shared listener target. Confirm scratch DB side isolation. Never commit secrets/connection strings.
Configure ASPNETCORE_ENVIRONMENT=Development; RunLegacyBootstraps=true for this scratch ONLY, CatalogDemo AllowResetAndSeed=false, and ShippingOneLevelCatalog / ShippingRateAuthority / ShippingOperationalShadow OFF. Do not invoke official reset-and-seed or seed-only. One launch ONLY, unique task-owned Host loopback port and explicit scratch content root. External watchdog <=180 seconds. NO retries; once startup ends/fails/watchdog fires, proceed to inspection and cleanup.
Inspect EXACT runtime result. Required evidence:
All module schemas (especially party.parties, offer, pricing, inventory, tax) migrated before Catalog PostMigration: record migration history and catalog startup stage order. Compare against 109's 42P01; no assumptions.
The schema-mobile-demo-phone status Published, exactly two sparse variants with one default PHONE-BLK-128; preserve axes/fingerprints, no full matrix growth.
If Catalog enricher succeeded, read-only SQL for actual ACTIVE offer bound to a published product, corresponding valid price and strictly positive stock; distinguish row presence from complete storefront eligibility. If none exist, record counts and precise stage preventing them.
If another exception occurs, capture FIRST exception with owner/module, line or operation, last completed stage and relevant scratch state; STOP — do not code/fix/relaunch.
If server reaches healthy state, record /health, scratch /__platform-commerce and only safe read-only API checks. Do NOT create a cart, start Checkout, activate methods or change prices. No customer browser proof in this task.

MISSION B — NON-INTERFERENCE, CLEANUP
Stop ONLY the known task-owned PID after absolute exe path and listen-port verification. NEVER terminate by name/wildcard (Get-Process -Name Tooba.Host, taskkill /IM, pkill -f) and NEVER stop/restart :5088 or :3000. Remove only task-owned scratch container, files, credentials and isolated Host. Verify ports closed/container removed. Then check original shared PID, /health, provenance and data parity; any unexpected shared change = explicit incident, no speculative restart.

MISSION C — EVIDENCE AND STOP
Create docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-111.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-111/** (redacted logs, isolation guard, migration order, database readback, watchdog timing, cleanup/parity, verdict, next exact blocker if any). Commit/push only those task-owned docs; source modifications NONE and code SHA NONE. Never assert commercial readiness or Checkout price cutover. Historical tests TB-110 are not newly executed in this task. No broad tests. At minute 22 stop investigative work and reserve time for cleanup/evidence/result; 30 minutes HARD includes all phases.

ALLOWED PATHS
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-111.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-111/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-111.task.md (UNSTAGED receipt only)
Ignored task-owned .tmp-111-* runtime artifacts only.

FORBIDDEN
NO production src/** modifications, migration/schema/code changes, shared DB DML/DDL, raw Catalog fixtures, 545-product demo reset-and-seed/seed-only, second bootstrap, broad suite, process-name termination, shared :5088/:3000 restart, credentials in tracked artifacts, permission bypass, store/cart/order/checkout/payment write, activation, financial gate switch, or optimistic PASS.

ACCEPTANCE
PASS only if the isolated startup clears the 109 party schema blocker, completes the relevant module-owned seed chain, and provides real readback of a sellable published offer + price + positive stock along with full teardown/shared parity. PARTIAL if 109 barrier clears but a new blocker prevents completion; cite the exact first new blocker and no fabricated readiness. BLOCKED if isolation fails, the same ordering barrier persists, or safe cleanup cannot be established. STOP_FOR_ARCHITECT; Automatic-Next-Implementation-Task NONE.

RESULT CONTRACT — BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-111
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-110
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_TWO_PHASE_MIGRATION_RUNTIME_AND_SELLABLE_PROOF
Bridge-Task-Row: <actual>
Status: PASS|PARTIAL|BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: bc8c87e2853ee6958420ed6e31631eca3c6211d0
Base-SHA: <actual>
Code-SHA: NONE
Evidence-SHA: <actual>
automaticNextImplementationTask: NONE
<Report single launch/watchdog, order of migration and post-migration, first exception or healthy state, product/variant/default state, offers/prices/stock and eligibility limitations, scratch isolation, cleanup, shared PID/provenance/parity, elapsed time, changed paths and rollback.>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK