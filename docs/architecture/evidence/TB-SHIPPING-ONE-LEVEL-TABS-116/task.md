PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-116
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-115
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_DEVELOPMENT_LEGACY_CATALOG_COMMERCE_CONTEXT_SCOPE_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 2ea499e98026a522f0bb98a2cce2cc5fe9f9cae5
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD; cease implementation by minute 22, retain time for targeted tests, evidence, commit and report. Stop with PARTIAL/BLOCKED rather than exceed.

ARCHITECT RULING
TB-115 demonstrated the TB-114 async DI scope works: the former root-provider exception is absent. Next first runtime blocker is platform.edition.unconfigured / Service Unavailable from ToobaNpgsql.ResolveForContext while FashionTemplateCatalogSeed resolves CatalogDbContext, because the NEW legacy bootstrap scope has no ICurrentCommerceContext assignment. DevelopmentSchemaMigrator.ApplyCoreAsync already assigns a trusted CommerceContext inside ITS OWN scope before touching DbContexts. Correct only the Development Host composition/bootstrap context boundary; never bypass ToobaNpgsql fail-fast or Tenant isolation. This is SOURCE + FOCUSED TESTS ONLY. TB-117 owns any new live/scratch startup.

PREFLIGHT — FAIL CLOSED

Claim exactly one Pending task on tooba-main, Working heartbeat; preserve docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-116.task.md UNSTAGED. Read AGENTS.md, protocol, current skills, TB-110/114/115 evidence and relevant source.
Fetch origin/main. Expected-Base-SHA must be ancestor; inspect drift and existing dirty/untracked files. Accept only non-overlapping task-owned bookkeeping; source overlap/ambiguous drift => RECOVERY_CONFLICT and STOP. No reset/stash/rebase/force-push; do not adopt old dirty screens or artifacts.
Shared :5088/:3000 and tooba_alpha are strictly read-only and optional. NO start/stop/restart/kill, NO migrations/DML/DDL, NO scratch Host/DB run in TB-116.

MISSION A — TRACE AND REPAIR TRUSTED COMMERCE CONTEXT

Trace Program.cs Development startup steps, tenant selection and connection-reference authority, DevelopmentSchemaMigrator.ApplyCoreAsync existing trusted CommerceContext creation and ICommerceContextAssigner.Assign, CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync, CatalogModule's DbContext options, ToobaNpgsql.ResolveForContext. Confirm exact first failure and where context is lost between scopes. Trace whether scopes for other subsequent bootstraps need context; do not fix hypothetical failures.
At the existing Host Development composition boundary, set the SAME trusted tenant/edition/connection-reference semantics for the single existing legacy Catalog bootstrap async scope BEFORE calling CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync. Prefer reusing an existing narrowly scoped trusted setup seam rather than duplicating tenant selection or constructing synthetic request headers. If a small shared Host Development composition helper is necessary, it may be introduced ONLY if ownership stays Host composition and its configuration, tenant selection and assignment follow the canonical migrator path exactly. No independent hardcoded store-alpha, no tenant-alpha, no cross-tenant context, no fallbacks to shared database; preserve SingleStore and other supported edition behavior and the existing fail-closed path when tenant/config absent.
Keep exactly one disposed async scope covering the full six-seed legacy Catalog bootstrap sequence, including exceptions and cancellations. Preserve ordering, seed flags and abort behavior, and scoped data lifetime. Do not move Catalog business seeding into Host, modify seed data, weaken ToobaNpgsql/ICommerceContext, alter DI lifetimes or disable validation.
If the canonical CommerceContext cannot safely be established here without significant refactoring/cross-module coupling, STOP BLOCKED_COMMERCE_CONTEXT_AUTHORITY; document exact authority and minimal design; do not guess.

MISSION B — MINIMAL PROOF

Add focused Host Development tests proving the real call path establishes a non-null commerce context with canonical edition/tenant/connection reference INSIDE the same scope before CatalogDbContext options resolution, and fail-closed when authoritative tenant/config is unavailable. Test scope isolation across successive scopes and disposal on throw/cancel where practical; use existing context-related tests/fixtures where available. Prefer composition tests or pure seam unit tests. No broad test sweep.
Run new tests, relevant existing Host Development/commerce-context guards, and Host build; record exact pass/fail counts and provenance for any known unrelated failure. A negative control of the pre-fix handoff is welcome only if bounded and restored. Do not start isolated/live Host, rebuild shared runtime, create scratch DB, or run full legacy seeds.
No changes to Catalog domain/public APIs/readiness, Shipping/Order/Checkout/Fulfillment, Production MigrationRunner, migrations, appsettings or frontend. No feature gate activation, pricing or stock changes.

MISSION C — EVIDENCE / STOP

Save concise source call-flow and authorization mapping, code diff, focused tests/build, rollback, and shared non-interference in docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-116.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-116/**. No secrets, credentials, tokens, customer data or full connection strings in evidence. Leave Bridge receipt UNSTAGED.
Commit ONLY bounded Host composition/test/docs edits, push origin main normally, verify HEAD == origin/main. STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.

FORBIDDEN
No root provider resolving scoped DbContext; no fake HttpContext/actor; no tenant or database fallback; no direct DbContext/SQL seed writes; no demo reset-and-seed/545-product expansion; no broad Host tests; no process termination by name or otherwise; no shared changes. Strict 30-minute HARD stop.

ACCEPTANCE
PASS only if the original TB-115 missing-CommerceContext root cause has a source-backed minimal fix and focused evidence shows trusted context assignment occurs in the same scoped lifetime before scoped database dependencies resolve, fail-closed guards survive, and relevant build/tests pass. No claim of /health 200, fully running templates, storefront label parity or checkout readiness before separately authorized TB-117 runtime proof.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return Task-ID/Parent-Task/Channel/Worker-Id/Agent-Type/Bridge-Task-Row, Expected-Base-SHA, actual Base-SHA and drift, Code-SHA, Evidence-SHA, Status PASS/PARTIAL/BLOCKED, Stop-State STOP_FOR_ARCHITECT, automaticNextImplementationTask NONE, measured minutes, specific cause and corrected authority hand-off, exact changed paths, test counts and negative control if applicable, unchanged shared state, deviations, rollback and first remaining blocker. STOP FOR ARCHITECT.
END_TOOBA_TASK