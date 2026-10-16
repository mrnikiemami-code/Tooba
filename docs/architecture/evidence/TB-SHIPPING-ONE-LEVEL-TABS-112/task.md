PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-112
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-111
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_CATALOG_DEVELOPMENT_ATTRIBUTE_SEED_COLLISION_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: c862d6c63ef571addb8c593f5bd63997a86e29dc
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
TB-111 proved real scratch two-phase Development startup now completes Catalog post-migration and produces a Published schema-mobile-demo-phone with precisely two sparse variants (one default), two active priced stocked offers. It then fails in the LATER legacy Host workspace demo seed: WorkspaceDemoProductSeed.SeedProductAsync tries CreateAttributeDefinitionAsync("color") after the Catalog-owned CatalogAttributeSchemaDevelopmentSeed already inserted the same normalized code, yielding catalog.attribute.code.duplicate. This is a Development seed idempotency/coordination problem, NOT a reason to weaken Catalog uniqueness or change public attribute creation behavior. Fix precisely the legacy seed's compatible reuse of existing attribute definitions, with semantic checks. Do not relaunch scratch startup in this task; TB-113 will provide runtime proof.

PREFLIGHT — STOP ON CONFLICT
Claim exactly one Pending task on tooba-main; preserve docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-112.task.md UNSTAGED. Read AGENTS.md, pipeline/controller, TB-108–111 evidence, WorkspaceDemoProductSeed, CatalogAttributeSchemaDevelopmentSeed, Catalog attribute-definition read/write contracts, and existing Catalog seed tests. Fetch origin/main: Expected-Base-SHA must be ancestor; classify docs-only evidence drift separately; STOP RECOVERY_CONFLICT on source overlap. Preserve all pre-existing dirty/untracked files. Shared :5088/:3000 and tooba_alpha read-only only.

MISSION A — EXACT COMPATIBILITY CONTRACT

Trace the legacy WorkspaceDemoProductSeed's CreateAttributeDefinitionAsync("color") and other possibly colliding attribute codes, including later use of the returned definition identity. Identify the Catalog-owned existing lookup/read seam for normalized code and the full semantics required by downstream variants: attribute type, input kind, variant-axis participation, allowed options/value identifiers, status and scope. Do NOT assume that matching a code alone proves compatibility; do not reuse an incompatible definition or silently overwrite it.
Prefer a tiny Development-only get-or-create helper in the owning Catalog seed code that: reads existing definition by canonical code; returns it ONLY if all downstream-required semantics match; otherwise returns/stops with an explicit, stable incompatibility diagnostic naming only the code and mismatched rule(s); creates via the EXISTING Catalog API if missing. Respect legacy contracts and reuse all existing invariants, without a new global uniqueness behavior or direct EF/raw SQL. Consider other seed attributes only if demonstrably on the same immediate path. If no existing read contract can supply sufficient semantics within allowed paths, STOP BLOCKED_CATALOG_SEED_COMPATIBILITY with evidence and precise proposed widening; don't invent a fragile workaround.

MISSION B — MINIMAL SOURCE AND TESTS
3. Implement ONLY the demonstrated Development-seed collision fix. Keep Catalog module ownership, method identity, existing definitions/values/options and downstream variant semantics stable; preserve previously valid bootstrap behavior on a database without prior attributes. No bulk demo generation, no extra product/variant/offer creation. Do not add a production endpoint, service-wide duplicate catch, or alter CreateAttributeDefinitionAsync's public rejection policy.
4. Add focused tests (prefer synthetic/in-memory or Testcontainers only if necessary) for: empty catalog creates required attribute normally; existing compatible color is reused with the SAME persistent identifier; existing incompatible color is refused before downstream writes; repeated seed preparation is idempotent; variant axes/option IDs remain valid. Test only the affected operation, not full Legacy Bootstrap. Execute narrow tests/build only. Do not run broad known-failing Host suite, full 545-product seed, or live browser flows.
5. Do not repair additional issues discovered after the immediate collision in this task. Report first additional blocker if test/source proof surfaces one.

MISSION C — EVIDENCE / STOP
Write docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-112.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-112/** recording root-cause source lines, semantic reuse decision, changed paths, focused test names/counts, exact rollback, shared no-change check and next scratch prerequisite. Commit/push only task-owned source/evidence. Keep task receipt UNSTAGED. Stop implementation by minute 22; 30-minute HARD STOP with PARTIAL/BLOCKED if necessary. STOP_FOR_ARCHITECT; no automatic next task.

ALLOWED PATHS
src/backend/Modules/Catalog//Development/WorkspaceDemoProductSeed.cs (only actual verified Catalog-owned path)
src/backend/Modules/Catalog//Development/** (additional narrow seed helper only if within same owner)
src/backend/Host/Tooba.Host.Tests/** (only focused Development Catalog seed regression tests)
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-112.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-112/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-112.task.md (UNSTAGED receipt only)
If actual owner is outside allowed paths, or an Application/Contracts read seam needs extension, STOP with a precise authority/widening request rather than silently broadening scope.

FORBIDDEN
No shared Host/Next restart/stop; no task scratch Host start in this task; no migration/schema/real DB DDL/DML; no direct EF or raw fixture SQL; no public Catalog create uniqueness weakening; no rewriting existing attributes, options or variant axes by guess; no readiness-rule modification; no 545-product demo seed or reset/seed API; no Checkout/Order/Fulfillment/Shipping/Payment/Settlement change; no gate flip; no process-name/wildcard termination; no auth bypass; no secrets in evidence; no reset/stash/rebase/force push.

ACCEPTANCE / STOP
PASS only when a code-collision-safe and semantically validated Catalog Development seed read-or-create path has focused regression proof and keeps existing ID/options/variant semantics. PARTIAL if source/test work useful but full semantics unproven. BLOCKED_CATALOG_SEED_COMPATIBILITY if the appropriate canonical read interface is absent or existing definitions are semantically incompatible and cannot safely be reused. No runtime/sellable/storefront-ready claim in 112.

RESULT CONTRACT — BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-112
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-111
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_CATALOG_DEVELOPMENT_ATTRIBUTE_SEED_COLLISION_REPAIR
Bridge-Task-Row: <actual>
Status: PASS|PARTIAL|BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: c862d6c63ef571addb8c593f5bd63997a86e29dc
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual>
automaticNextImplementationTask: NONE
<state actual semantic attribute compatibility, duplicate-code prevention, test counts, elapsed minutes, shared parity, rollback, and required TB-113 isolated runtime proof>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK