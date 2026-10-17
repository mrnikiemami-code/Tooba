PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-107
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-106
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_CATALOG_DEMO_DEFAULT_VARIANT_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: aceeb46acbfc89024e2def182da3ade73d20714a
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD; stop implementation at minute 22 and preserve remaining time for evidence, cleanup, commit, and report.

ARCHITECT RULING
TB-106 directly observed missing=[variants] during empty-scratch startup. SQL proves both variants have valid axes/values, no duplicate fingerprint, but neither is_default=true. Ownership is the Catalog Development attribute-schema seed, NOT general variant creation, publish rules, Order, or Shipping. Repair ONLY that seed by selecting exactly one default variant using an EXISTING Catalog application/domain operation that enforces single-default. Do not introduce direct EF writes or force-publish. The entire goal of this task is to make the existing demo product pass variant readiness, not to create offers or prove storefront labels yet.

PREFLIGHT — STOP ON CONFLICT

Claim exactly one pending Bridge task on tooba-main. Preserve this task receipt UNSTAGED. Read AGENTS.md, pipeline/controller, Catalog feature skill, TB-105/106 evidence, CatalogAttributeSchemaDevelopmentSeed, variant management application contracts, single-default invariants and the mandatory architecture recovery.
Fetch origin/main; verify Expected-Base-SHA ancestor; reconcile only non-overlapping result bookkeeping. On source overlap or uncertain dirty file ownership => RECOVERY_CONFLICT, stop. Never reset/stash/rebase/force-push or stage unrelated paths.
Confirm shared :5088/:3000 provenance and read-only tooba_alpha baseline. Do not stop/restart/modify shared processes, gates or data.

MISSION A — MINIMAL CATALOG-OWNED REPAIR

Locate the existing application/domain method for assigning a default variant. Prefer calling the canonical command/service already used by Admin matrix-apply, only if it is safe for variants created directly by the seed. Do not duplicate the default-selection algorithm or expand its public semantics.
After the two variants are created/identified in CatalogAttributeSchemaDevelopmentSeed, select ONE deterministic existing variant as default (stable choice, e.g., first created). Preserve two valid variants, axis option IDs and fingerprints. Re-running seed must not create duplicate variants or oscillate the default; existing non-seed merchant product state must not change.
Do not alter ProductVariantDirectory.CreateWorkspaceVariantAsync global semantics, ProductPublishReadinessReader, ProductPublishRules, publishing constraints, Catalog lifecycle, product-price-offer-stock rules, or other bootstraps. If the existing canonical operation cannot be used from this seed without broader refactor, STOP with BLOCKED_DEFAULT_VARIANT_APPLICATION_SEAM; propose the smallest adaptation, don't invent an unsafe setter.
Only Catalog-owned source within the specific Development seed file and a narrowly required Catalog-owned helper may change; one focused test file may be added/modified. No Host, Contracts, Order, Fulfillment, Shipping, Checkout, Payment, Settlement, migration, config or frontend edits. If a wider path is necessary, report a blocker instead of expanding silently.

MISSION B — FOCUSED EVIDENCE

Add the smallest automated regression test proving that the seed's two valid variants have exactly one default and are accepted by the existing variant-readiness rule; ensure the readiness gate itself remains unchanged. Test repeat-safe behavior if feasible without broad fixtures.
Run targeted Catalog-related tests and compile only relevant changed projects; do not run known-failing broad Host suites or retry DLL locks.
IF Mission A is complete with sufficient time remaining, run at most ONE isolated scratch Host startup based on TB-106's proven scratch-only recipe, legacy bootstrap enabled, with a watchdog <= 240 seconds. Capture observed Missing[]/publish outcome and whether it proceeds past the previous failure. Passing the original stage is enough; do not wait for a large sellable-data seed, do not run 545-product reset-and-seed or seed-only, do not retry. If time is insufficient, skip scratch run and explicitly mark LIVE_SCRATCH_NOT_VERIFIED.
Hard isolation barrier before launch: task-owned no-volume Postgres container, localhost-only port, tenant/reference overlay containing NO tooba_alpha or tenant-alpha, separate output/working directory, process-only settings and secret files ignored by Git. Never connect to shared database for writes.
CRITICAL PROCESS SAFETY: NEVER use Get-Process -Name Tooba.Host | Stop-Process, taskkill /IM, pkill by name, Stop-Process by wildcard, or kill by port without proof. Capture the exact spawned isolated process PID/handle, verify its executable path under this task's isolated output directory, and stop ONLY that PID. If ownership cannot be proven, stop and report BLOCKED_SAFE_CLEANUP; never terminate the shared :5088 Host. Cleanup only task-owned scratch container/paths. Verify shared PID, /health and provenance remained unchanged.

ACCEPTANCE AND REPORT

PASS if smallest seed default-variant repair is committed, focused tests pass, no readiness-rule change, and no shared change; explicitly distinguish source/test evidence from optional isolated startup evidence.
PARTIAL if correct bounded code/tests exist but scratch confirmation is unavailable and the result is explicitly not full bootstrap proof.
BLOCKED if no safe canonical setter or isolation/cleanup cannot be proven. Never claim sellable offers or storefront projection parity from this task.
Record exact paths, SHA, tests, actual elapsed minutes, observed default count, source-line evidence, blocked checks, cleanup facts, shared pre/post state, and rollback instructions in docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-107.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-107/**. Push only task-owned code/evidence; keep task receipt unstaged.
Do not extend 30-minute HARD stop for cleanup; begin cleanup and result compilation by minute 22.

RESULT CONTRACT — BRIDGE-WAKE-V1
Report BEGIN_TOOBA_WORKER_RESULT / END_TOOBA_WORKER_RESULT with Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode, Status PASS|PARTIAL|BLOCKED, Stop-State STOP_FOR_ARCHITECT, Expected-Base-SHA, Base-SHA, Code-SHA, Evidence-SHA, automaticNextImplementationTask: NONE. Never self-dispatch another implementation task.
END_TOOBA_TASK