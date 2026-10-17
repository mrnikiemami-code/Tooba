PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-120
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-119
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_FULFILLMENT_OWNED_ONE_LEVEL_CATALOG_BOOTSTRAP_WIRING
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: dc2eb1bd71261cd95bc49ac77814105034279913
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD. Aim to finish implementation within 30 minutes; reserve the remainder for a single focused test run, evidence, commit/push and reporting. No loops, retry cascades, broad tests or repeated builds. If blocked or deadline reached, report PARTIAL/BLOCKED and stop.

ARCHITECT RULING
TB-119 proved an isolated guest checkout Shipping Projection returns seven valid legacy methods, but even with Tooba:ShippingOneLevelCatalog:Enabled=true the one-level catalog endpoint returns methods=[] because existing migration is DDL-only and no authorized runtime caller invokes the registered ShippingMethodBackfillPlanner/IShippingOneLevelBackfillStore. This task repairs ONLY the missing Fulfillment-owned, Development-scoped bootstrap seam, if source trace confirms it can be done safely with existing APIs. No manual SQL, no Host-owned shipping business logic, no weakening of tenant isolation. TB-121 (if authorized later) will own isolated runtime/UI proof. Do not claim UI or commercial readiness here.

PREFLIGHT — FAIL CLOSED

Claim exactly one Pending Bridge task on tooba-main after Working heartbeat; preserve docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-120.task.md UNSTAGED. Read AGENTS.md, pipeline/skill rules and the TB-119 evidence.
Fetch origin/main. Verify Expected-Base-SHA ancestor and inspect intervening source changes and all pre-existing dirty/untracked paths. If overlapping or ambiguous source drift: RECOVERY_CONFLICT and STOP. Do not reset, stash, rebase, force-push or adopt unrelated dirty files.
:5088/:3000, tooba_alpha and existing containers are STRICTLY READ-ONLY and optional. No start, stop, restart, kill, DB mutation or appsetting edits on shared instances. No scratch Host startup in this task.

MISSION A — MINIMAL MODULE-OWNED REPAIR

Trace ShippingOneLevelCatalog activation/read, ShippingMethodBackfillPlanner, IShippingOneLevelBackfillStore, Fulfillment DI and the existing Development bootstrap ordering. Verify the planner's inputs, tenant scope, language/localization dependencies, transaction/idempotency semantics, exact method/option identity and whether activation is a separate explicit operation. Do not assume a registered component is safe to invoke without checking.
Add the narrowest Fulfillment-owned Development bootstrap invocation of the EXISTING backfill capability, using canonical trusted tenant CommerceContext and an already-authorized Development orchestration seam. Host may only compose/invoke a module-owned seam; it must not implement Shipping catalog rules, SQL, mapping or backfill. No new duplicate backfill algorithm. Prefer reusing an existing module entry point; if necessary, expose one tiny Fulfillment-owned Development gateway following existing contract conventions.
Run only in Development and only when the configured one-level catalog gate permits it. Preserve fail-closed behavior when gate, tenant, authoritative source, required language or module dependencies are missing. Explicit activation must not be silently performed if existing policy requires a separate operator action. Preserve legacy Shipping Projection and public APIs. Respect service+option identity, deterministic ordering, translations and idempotent no-duplicate reruns.
If the backfill cannot safely run under the existing authority model (e.g. planner expects a manual operator action, missing tenant/language, or migration/activation dependency), STOP BLOCKED with exact source-backed minimum design; do NOT fabricate catalog rows, broaden production semantics or work around policy.
Keep all work within Fulfillment-owned Development integration and the minimal Host composition seam if required. Do not change Shipping/Order/Checkout/Payment business behavior, Catalog, migrations, schema, production startup paths, frontend, shared appsettings, public endpoints or feature-gate defaults.

MISSION B — FAST, FOCUSED VERIFICATION

Add only the minimum targeted tests: (a) when enabled with legitimate canonical source data, module-owned Development invocation populates the intended one-level catalog rows via existing backfill, preserves method/option identity and localized labels; (b) second invocation is idempotent; (c) disabled/missing authority fails closed with no mutation. Use existing fixtures/unit seams; no live startup or scratch database creation in TB-120.
Run the new focused tests ONCE, plus only directly affected existing tests if essential, and build only changed projects if required. Avoid full solution sweeps, Playwright, frontend rebuild, stress tests, repeated retries, and unrelated failures. Record counts and any known pre-existing failure; do not repair unrelated issues.
Do not infer successful storefront display from code-level tests; runtime acceptance belongs to a separately approved next task.

MISSION C — EVIDENCE, COMMIT, STOP

Record source call flow, authority/gate/activation decisions, exact bounded diff, test/build counts, risks, rollback and non-interference in docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-120.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-120/**. Keep evidence concise; redact all secrets and identifiers.
Commit ONLY bounded source/test/docs changes; push origin/main normally; verify HEAD == origin/main. Preserve pre-existing files and the Bridge task receipt UNSTAGED. No automatic next task.

FORBIDDEN
No SQL DML/DDL or DB seeding by Host; no direct scratch/shared database writes; no Host startup; no fake checkout actors or HttpContext; no cross-tenant context; no legacy data reset, 545-product expansion, migrations, prod changes, gate flip in tracked config or shared deployment. No iterative test loops, broad test suites, process termination, hidden retries or scope expansion.

ACCEPTANCE
PASS only if a minimal module-owned Development bootstrap is correctly wired to the pre-existing backfill under trusted tenant/gate semantics, focused evidence verifies population/idempotency/fail-closed (or clearly documents a policy-blocked outcome), and build/tests for changed surface pass. A blocked authority or required operator-activation path is BLOCKED, not silently bypassed. NO claim of /shipping DOM parity or production readiness.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return Task-ID/Parent-Task/Channel/Worker-Id/Agent-Type/Bridge-Task-Row/Status PASS|PARTIAL|BLOCKED/Stop-State STOP_FOR_ARCHITECT/automaticNextImplementationTask NONE, Expected-Base-SHA, Base-SHA/drift, Code-SHA, Evidence-SHA, elapsed minutes, traced missing caller and authoritative source, exact changed paths, gates/activation policy, focused test counts/build result, non-interference, deviations, rollback and first remaining blocker. STOP FOR ARCHITECT.
END_TOOBA_TASK
