PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-109
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-108
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_CATALOG_BOOTSTRAP_POST_DEFAULT_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 729fa793a58508ab05e207a7a60ebec0cfda19d9
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
TB-108 source/focused-test acceptance: canonical Catalog-owned SetDefaultAsync preserves the two sparse variants, and CatalogAttributeSchemaDevelopmentSeed assigns PHONE-BLK-128 as the sole default before PublishProductAsync. Current question is RUNTIME, not another implementation: does the supported Development startup proceed past the exact missing=[variants] failure, and, if it does, is a genuine sellable offer created by the existing legacy bootstrap? Perform ONE isolated startup only, bounded by watchdog <=180 seconds. Do not run the official 545-draft-product reset-and-seed or seed-only endpoint. If later initialization fails or offers remain absent, record the exact new barrier and STOP; do not broaden scope to fix it. This task intentionally does NOT attempt an authenticated customer-cart projection or Checkout.

PREFLIGHT — STOP ON CONFLICT
Claim one Pending task on tooba-main. Preserve docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-109.task.md UNSTAGED. Read AGENTS.md, pipeline/controller, 105–108 evidence, CatalogDevelopmentSeed and the 108 focused test. Fetch origin/main; Expected SHA must be an ancestor, and any intervening source drift overlapping the seed/default setter triggers RECOVERY_CONFLICT. Preserve existing dirty/untracked files; no reset/stash/rebase/force-push. Before any process or database operation, prove the shared Development listeners and tuple for :5088, :3000 and tooba_alpha by READ-ONLY checks. The previous 106 teardown accidentally stopped shared :5088; this must not recur.

MISSION A — ISOLATED TARGET AND SINGLE BOUNDED START

Reuse the proven 106 scratch-only recipe with task-specific identifiers: disposable PostgreSQL container without shared volume, loopback-only unique port; task-owned schema and messaging databases; effective config with exactly one tenant and only scratch connection references. FAIL CLOSED if any resolved host/database/connection reference points at tooba_alpha, tenant-alpha, or the shared DB listener. Produce offline isolation guard and DB-side proof before invoking startup; never store credentials in tracked evidence.
Build current-code Host to a task-owned separate output directory. Launch ONLY the task-owned executable from that absolute path on unused loopback port with ASPNETCORE_ENVIRONMENT=Development and explicit scratch content/config root. Enable the existing RunLegacyBootstraps=true only inside isolated effective configuration; keep CatalogDemo reset/seed endpoints disabled. Keep ShippingOneLevelCatalog, ShippingRateAuthority and ShippingOperationalShadow all OFF. Start exactly ONCE with an external watchdog <=180 seconds. Capture stage progress and bounded/redacted logs. No retry and no second startup on a failure.
Record the first outcome precisely:
If the 108 product now PUBLISHES, verify on scratch DB its published state, exactly two original diagonal variants, exactly one default PHONE-BLK-128, axes and fingerprints intact; confirm the 105 diagnostic no longer reports missing=[variants] for it.
If later startup succeeds, inspect by read-only SQL whether official Catalog-owned bootstrap created any eligible published product with an active sellable offer, valid price and positive inventory. Record factual counts; do not claim full storefront eligibility from a count alone.
If a later stage fails (or watchdog expires), capture the exact first exception/stage and the last completed stage, plus the observed scratch data state. Do not patch/restart/retry or attempt 545-product demo seeding. Classify as PARTIAL with a specific next blocker.
If the original missing=[variants] persists, report BLOCKED with method/variant/readiness proof and NO guessed root cause.

MISSION B — SAFE CLEANUP AND PARITY
Stop only the exact PID/process handle that this task created after verifying its absolute executable path and expected listening port; gracefully terminate if alive and escalate ONLY for that exact task-owned PID if necessary. NEVER terminate by name or by wildcard (Get-Process -Name Tooba.Host | Stop-Process, taskkill /IM, pkill -f, etc.). Remove only this task's scratch container, run/output directory and temporary credentials. Verify no task-owned listeners/containers remain and re-check the shared Host /health, platform provenance, PID/ownership and read-only table counts against preflight. If a shared listener has changed unexpectedly, STOP and report immediately without attempting speculative recovery or shared restart.

MISSION C — REPORT/NO SCOPE EXPANSION
No production-source changes are authorized, no new tests needed if code unchanged; cite TB-108 focused tests only as historic evidence. Write docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-109.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-109/** with single-run logs, independent isolation, watchdog timing, exact stage outcome, scratch row counts and cleanup/shared parity. Commit/push task-owned documentation only. Leave task receipt UNSTAGED.

ALLOWED PATHS
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-109.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-109/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-109.task.md (UNSTAGED RECEIPT ONLY)
Task-owned ignored .tmp-109-* runtime files only.

FORBIDDEN
NO src/** edits, migrations, appsettings commits, manually invented Catalog/offer/price/inventory rows, raw SQL DML fixtures, seeded real data, reset-and-seed, seed-only, second bootstrap, Host/Next shared restart, ProcessName-based termination, permission bypass, payment/order/checkout write, gate cutover or commercial-readiness claims. No long tests or retry loops. Stop planning implementation work by minute 22 and reserve remainder for cleanup/commit/result; 30 minutes includes everything and is HARD STOP.

ACCEPTANCE / STOP
PASS if one isolated startup proceeds past the original variant-default publication blocker, exact sparse/default invariants are verified, its final startup/sellable-offer state is recorded truthfully, and complete isolation/teardown/shared parity hold. PARTIAL if original publish succeeds but next startup/sellable prerequisite blocks, with first exact blocker. BLOCKED if isolation cannot be established, original publish still fails, or safe cleanup cannot be guaranteed. Do not claim storefront label or Checkout proof here. STOP_FOR_ARCHITECT, automaticNextImplementationTask NONE.

RESULT CONTRACT — BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-109
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-108
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_CATALOG_BOOTSTRAP_POST_DEFAULT_PROOF
Bridge-Task-Row: <actual>
Status: PASS|PARTIAL|BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 729fa793a58508ab05e207a7a60ebec0cfda19d9
Base-SHA: <actual>
Code-SHA: NONE
Evidence-SHA: <actual>
automaticNextImplementationTask: NONE
<include original blocker resolved?, first new blocker/stage, method of isolation, one-run watchdog seconds, sparse variant/default readback, offers/prices/stock readback, process cleanup, shared PID/provenance/parity, exact paths, elapsed time>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK