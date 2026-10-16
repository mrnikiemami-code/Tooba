PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-108
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-107
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_CATALOG_SPARSE_DEFAULT_VARIANT_SEAM
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 1ff62762ba7679418db53208f47bb2ee6d412bc5
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
107 BLOCKED_DEFAULT_VARIANT_APPLICATION_SEAM accepted. AUTHORIZE Option A ONLY: Catalog owns one narrow default-variant operation on an existing sparse product; it MUST reuse the existing directory-level ClearDefaultFlags + SetDefault(true) invariant, WITHOUT applying the Cartesian matrix and WITHOUT adding/removing/archiving any variant or axis. The CatalogAttributeSchemaDevelopmentSeed shall explicitly designate the first existing demo variant (PHONE-BLK-128) default before publishing. No domain rule relaxation. This task is SOURCE + FOCUSED TEST ONLY; do NOT launch a new scratch Host or legacy bootstrap in 108. Isolated runtime proof is a separate next task. Aim to finish in 20-25 minutes; 30 minutes is HARD STOP, not an invitation to overrun.

PREFLIGHT — STOP ON CONFLICT
Claim exactly one pending task on tooba-main via BRIDGE-WAKE-V1. Keep docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-108.task.md UNSTAGED receipt. Read AGENTS.md, pipeline/controller, relevant Catalog backend feature skill, 105–107 evidence and current Catalog variant directory/ICatalogDirectory contracts and seed. Fetch origin/main. Expected SHA must be an ancestor; prove any intervening changes are only nonoverlapping bookkeeping. STOP RECOVERY_CONFLICT on overlapping source drift. Preserve all pre-existing dirty/untracked files; no reset/stash/rebase/force-push.

MISSION A — ONE CANONICAL NARROW DEFAULT OPERATION

Within Catalog's existing variant-directory/use-case authority, implement SetDefaultAsync(productId, variantId, ct) (precise name to match conventions). Reuse existing ClearDefaultFlags and SetDefault domain behavior instead of reimplementing the uniqueness algorithm in the seed. Validate product ownership/identity, target variant belongs to requested product, target is not archived, and existing relevant permission/guard conventions. When the requested variant is already the sole default, it must be idempotent. Use a single SaveChanges/transaction following the directory's established persistence pattern; never leave two defaults or none after a successful call. Fail closed on missing product, missing/mismatched variant and forbidden archived target, using existing Result/error idioms. DO NOT call ApplyProductVariantMatrixAsync; that is Cartesian and destructive for this seed's sparse diagonal. Do not introduce duplicate default-setting logic or a new generic framework. If the existing access path cannot safely reuse the helpers and protections in this timebox, STOP PARTIAL/BLOCKED instead of inventing a bypass.
Expose only the minimum Catalog Application/facade contract needed for the Development seed to call this operation (ICatalogDirectory if required); implementation remains in Catalog. No public admin HTTP endpoint or frontend action in 108. Avoid unrelated Contracts changes and architecture guards unless truly required.
Change ONLY CatalogAttributeSchemaDevelopmentSeed so it deterministically sets the first of its two created variants (PHONE-BLK-128) as default BEFORE PublishProductAsync. Preserve both original diagonal variants and both axes (color + storage); no extra combinations, offers or seed records. Keep the 105 Development diagnostic in place for next runtime proof.

MISSION B — FOCUSED TESTS, NO LIVE STARTUP
Add the smallest meaningful Catalog tests asserting: existing sparse two-variant set remains exactly two with original identities/fingerprints/axes; exactly one default after assignment; repeated assignment is no-op; changing default clears previous; wrong product / missing variant / archived variant are refused; publish-readiness variants requirement passes when the other five readiness prerequisites are met; no full matrix application occurs. Prefer existing test infrastructure; do not build a new integration harness merely for this unit. Run ONLY the new focused Catalog tests and necessary compile/build or near-neighbour guards. Record actual counts and any pre-existing failures honestly.

ALLOWED PATHS
src/backend/Modules/Catalog//ProductVariantDirectory.cs
src/backend/Modules/Catalog//IProductVariantDirectory.cs
src/backend/Modules/Catalog//ICatalogDirectory.cs
src/backend/Modules/Catalog//CatalogDirectory.cs
src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/CatalogAttributeSchemaDevelopmentSeed.cs
src/backend/Modules/Catalog//Tests/ (focused Catalog tests in actual existing test location)
src/backend/Host/Tooba.Host.Tests//Catalog*Tests.cs (focused existing test location if necessary)
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-108.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-108/
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-108.task.md (UNSTAGED RECEIPT ONLY)
If an exact implementation file path differs, use its existing Catalog path only; document it. Any cross-module change or broader path requires STOP_FOR_ARCHITECT.

FORBIDDEN
No ProductPublishReadinessReader or variant readiness-rule weakening; no raw EF writes in the seed; no Cartesian matrix apply; no variant/archive/axis/offer/price/inventory expansions; no public route; no Host/Next restart; no scratch or shared DB bootstrap; no DML on tooba_alpha; no Order/Fulfillment/Shipping/Checkout/Payment/Settlement changes; no activation or shipping-gate changes; no broad Host test suite. Never use name-based process termination (Get-Process -Name Tooba.Host | Stop-Process, taskkill /IM, pkill -f); in 108 NO process stop is authorized at all. Never log secrets or credentials.

ACCEPTANCE / STOP
PASS only when the narrow canonical default setter and seed call are implemented, focused tests pass, and sparse variant IDs/axes are proven preserved. PARTIAL if a safe subset alone is completed; BLOCKED with precise missing seam if contract integrity cannot be kept. Runtime scratch proof intentionally DEFERRED; do not claim real startup success, sellable Offer creation, storefront label proof or commercial readiness. STOP work at or before 30 minutes including reporting and commit; if near minute 22, stop expansion, run the smallest essential verification and package evidence. Commit/push only task-owned files. STOP_FOR_ARCHITECT; do not create 109 automatically.

RESULT CONTRACT — BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-108
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-107
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_CATALOG_SPARSE_DEFAULT_VARIANT_SEAM
Bridge-Task-Row: <actual>
Status: PASS|PARTIAL|BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 1ff62762ba7679418db53208f47bb2ee6d412bc5
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual>
automaticNextImplementationTask: NONE
<explain exact paths, tests/counts, default invariant proof, safety, runtime proof deferred, actual elapsed, deviations>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK