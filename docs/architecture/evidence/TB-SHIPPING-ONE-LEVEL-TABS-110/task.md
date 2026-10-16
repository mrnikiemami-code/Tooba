PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-110
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-109
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_DEVELOPMENT_MIGRATION_POSTMIGRATION_ORDER_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 072127f7f60a11c0de5583a472b78c27bcf8a5d3
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
TB-109 proved the TB-108 sparse default-variant repair works in one isolated startup: the Catalog demo product is Published, two sparse variants remain, and exactly one is default. The first new failure is Npgsql 42P01 relation party.parties does not exist, from CatalogAttributeSchemaSellableEnricher -> PartyDevelopmentSeedGateway before Party migrates. This is a Development migration/post-migration ORDERING prerequisite, not a Catalog readiness or Shipping price defect. Fix the smallest correct orchestration boundary, with proven preservation of registered module migration order and existing production bootstrap semantics. Do not build tables by hand, bypass Catalog, or edit Checkout.

PREFLIGHT — STOP ON CONFLICT
Claim exactly one Pending task on tooba-main and preserve docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-110.task.md UNSTAGED. Read AGENTS.md, pipeline/controller, TB-106–109 findings, DevelopmentSchemaMigrator and its interfaces, startup wiring and module registration, the canonical migration runner and related focused guards. Fetch origin/main; expected SHA must be an ancestor; STOP RECOVERY_CONFLICT if overlapping source drift. Preserve all existing dirty/untracked files. Confirm the :5088/:3000 and tooba_alpha shared-state baseline READ-ONLY; never restart them.

MISSION A — OWNERSHIP AND ORDERING AUDIT (bounded, first)

Map actual call order in DevelopmentSchemaMigrator.ApplyCoreAsync / MigrateOrderedAsync and prove which module-owned migration or post-migration steps assume schemas belonging to modules not yet migrated (Party, Offer, Pricing, Inventory, Tax and any other observed dependencies). Compare against current canonical ordered module-registration contract. Distinguish Development-only post-migration seed work from necessary migrations and the production/runner path.
Choose the smallest ownership-correct change. Preferred only IF source evidence supports it: finish ALL registered module schema migrations in the existing canonical module order first, then execute registered Development post-migration steps in their existing deterministic order, without running any step twice. Maintain startup cancellation and fail-fast semantics, absence of post-migration execution after migration failure, schema migrator idempotency and exactly-once Development seed behavior. Do NOT reorder core modules by guess or change production migration authority. If step registration semantics or dependencies make this unsafe, STOP BLOCKED_POSTMIGRATION_ORDER_AUTHORITY with the exact conflicting contracts; no speculative modification.

MISSION B — IMPLEMENTATION AND ESSENTIAL TESTS ONLY
3. Make only the bounded orchestration change required by A; do not edit Catalog publish rules, ProductVariantDirectory, Party schema, migrations, Catalog product/offer/price/stock seed data, or any Checkout/Order/Fulfillment/Shipping/Payment/Settlement source. Preserve all existing Host-composition-only rules and module-owned post-migration logic. No new generic framework.
4. Add/adjust focused regression tests exercising two synthetic module migrators/steps to prove: migrations A and B both finish before post-step A accesses B; canonical migration order maintained; post-step order stable; cancellation/failure short-circuits; no duplicate execution; legacy safe behavior for any no-step case. Keep tests narrow and avoid broad Host suites or locked output loops. If an existing test covers a rule, reuse it instead of duplicating it.
5. Build/test a targeted Host DevelopmentSchemaMigrator test project using a separate output dir if the live Host locks DLLs. No need to launch any scratch Host in this task: runtime proof belongs to TB-111. Do not claim any sellable offer or customer cart is now reachable.

MISSION C — EVIDENCE AND STOP
Write docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-110.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-110/** with before/after ordering graph, source paths, exact changed paths, targeted tests with measured counts, risk/rollback and unresolved next runtime check. Commit/push ONLY task-owned changes. Keep receipt UNSTAGED. Stop implementation by minute 22 and reserve time for proof, reporting and push. At 30 minutes HARD STOP even if incomplete; status PARTIAL or BLOCKED honestly. Do not auto-advance.

ALLOWED PATHS
src/backend/Host/Tooba.Host/Development/DevelopmentSchemaMigrator.cs (only if verified exact path)
src/backend/Host/Tooba.Host.Tests/** (focused DevelopmentSchemaMigrator tests only)
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-110.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-110/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-110.task.md (UNSTAGED receipt only)
If the real source is in a different path or a different owned interface must change, STOP and report the precise widening request, not a silent scope expansion.

FORBIDDEN
No shared Host/Next restart or stop; no scratch bootstrap in this task; no DB DDL/DML on real environments; no raw SQL fixtures; no demo reset-and-seed/seed-only; no gate changes; no process-name-based termination; no readiness relaxation, cross-module Infrastructure imports, migration history rewrite, live cart/order/checkout/payment/settlement write or commercial-ready claim; no secret in logs/evidence; no reset/stash/rebase/force push. Preserve non-task changes.

ACCEPTANCE / STOP
PASS for a source-proven, narrowly tested ordering correction that completes all canonical schema migrations before Development post-migration seed work while preserving order and failure/cancellation behavior. PARTIAL if useful scoped evidence or test work completed but correctness unproven; BLOCKED_POSTMIGRATION_ORDER_AUTHORITY if no safe correction exists within permitted ownership. Runtime startup, offers, storefront labels remain unverified until a separately authorized bounded scratch run. STOP_FOR_ARCHITECT.

RESULT CONTRACT — BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-110
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-109
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_DEVELOPMENT_MIGRATION_POSTMIGRATION_ORDER_REPAIR
Bridge-Task-Row: <actual>
Status: PASS|PARTIAL|BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 072127f7f60a11c0de5583a472b78c27bcf8a5d3
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual>
automaticNextImplementationTask: NONE
<include ordering-before-after, exact ownership and path, migration/post migration test evidence, failed tests if any, no-shared-change proof, elapsed minutes, rollback, TB-111 runtime prerequisite>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK