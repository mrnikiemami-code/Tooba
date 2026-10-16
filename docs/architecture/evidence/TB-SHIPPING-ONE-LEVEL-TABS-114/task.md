PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-114
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-113
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_DEVELOPMENT_CATALOG_LEGACY_SEED_DI_SCOPE_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 5c5c5d2a0072e4ed965063776c2bf299d5d05b7f
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD. Stop implementation at minute 22; reserve remaining time for focused proof, evidence and push. No overrun.

ARCHITECT RULING
TB-113 proved that the Catalog color/black seed collision is resolved, two-phase migrations complete, the legacy workspace seed completes, and real Active offers/Base prices/positive stock exist on scratch. First subsequent startup blocker is a DI lifetime violation: Program.cs around line 368 calls CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(app.Services, ct), and FashionTemplateCatalogSeed.ApplyAsync at line 59 resolves scoped CatalogDbContext from that ROOT provider. Correct ONLY the Development bootstrap scope boundary; retain module ownership and every existing seed's behavior. This is SOURCE + FOCUSED TESTS ONLY. DO NOT start any Host, real bootstrap or shared environment in this task.

PREFLIGHT — FAIL CLOSED

Claim exactly ONE Pending Bridge task on channel tooba-main; preserve task receipt docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-114.task.md UNSTAGED; read AGENTS.md, pipeline/controller, relevant backend skill, TB-110 through TB-113 evidence and current source.
Fetch origin/main. Expected-Base-SHA must be ancestor of actual HEAD; classify all drift. Only task-owned prior evidence bookkeeping may be non-overlapping. If src overlap or ambiguous unrelated drift, STOP RECOVERY_CONFLICT. Preserve all existing dirty/untracked paths. No reset, stash, rebase, force push or adopting unrelated modifications.
Read-only shared health/status check if helpful, but absolutely no start/stop/restart/kill of shared Host :5088 or Next :3000; no write to tooba_alpha, no SQL fixtures, no migration execution.

MISSION A — IDENTIFY AND REPAIR ROOT→SCOPED BOUNDARY

Trace Program.cs:368, CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync, FashionTemplateCatalogSeed.ApplyAsync and all downstream scoped dependencies; compare with DevelopmentSchemaMigrator's proven async-scope usage. Record concrete call tree and confirm the exact invalid provider handoff; do NOT infer other exceptions.
Implement the SMALLEST correct explicit async DI scope enclosing the EXISTING legacy Catalog bootstrap sequence. Prefer the Host composition boundary at the invocation site (create IServiceScope/IAsyncServiceScope and pass scope.ServiceProvider) where possible, preserving cancellation and disposal even when seed throws. If the method already internally owns a scope, repair only the actual faulty handoff and explain why. Do NOT store scoped contexts in singleton objects, disable ValidateScopes, resolve from root with a workaround, create custom service provider, or move domain/seeding authority into Host. No new route, config, feature flag, migration, contract, data change or generic framework.
Confirm scope is appropriate for ALL existing legacy Catalog bootstrap calls, not merely FashionTemplateCatalogSeed's first DbContext resolution. Preserve order, once-only invocation, exception propagation, and Development-only gating. Do not change ProductPublishReadiness, variant invariant, Catalog seeds, shared Development scripts, Order/Fulfillment/Shipping/Checkout/Payment/Settlement.

MISSION B — FOCUSED TESTS / NEGATIVE CONTROL

Add the narrowest automated regression test exercising the REAL composition/call-site seam (or extract minimal testable composition helper if essential and ownership-safe). Use a real ServiceCollection/ServiceProvider with ValidateScopes=true and a scoped fake dependency or similarly reliable proof; verify root-provider handoff fails and scoped-provider handoff succeeds, with async disposal on success and exception/cancellation. No Testcontainers needed merely for this DI boundary.
Run only new focused tests + the relevant existing Host Development composition guards; build affected Host project. Record exact counts. If feasible within budget, show negative control temporarily reintroducing root handoff fails and then restore actual fix, recording only final corrected code.
If a safe targeted seam cannot be implemented without changing unrelated API contracts or product code, STOP BLOCKED_DI_SCOPE_OWNERSHIP and report exact files/call frames; no speculative refactor.

MISSION C — EVIDENCE AND STOP

Save concise root-cause and before/after call flow, minimal diff, focused-test logs, guard/build evidence and shared non-interference under docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-114.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-114/**. Keep passwords/tokens/PII absent. Preserve task receipt UNSTAGED.
Commit ONLY this bounded source/test/docs change, normal push origin main, verify HEAD matches origin/main; no force push. Stop at STOP_FOR_ARCHITECT. No automatic next task.
Rollback is a single commit revert; no schema/data/host restart consequence.

FORBIDDEN
No isolated Host launch; no scratch Postgres run; no shared Host or frontend restart; no process termination (including name-based); no direct EF/SQL seed data; no demo reset/seed; no broad Host test suite; no business logic change; no root DI scope-validation suppression; no unbounded research/test loops. Hard timebox includes final reporting.

ACCEPTANCE
PASS only if the actual legacy Catalog bootstrap receives a real scoped provider, the original root-provider failure is prevented by structural and test proof, scoped objects are disposed correctly and focused tests/guards pass. PARTIAL if code is safe but proof incomplete. BLOCKED if the ownership boundary cannot be safely changed. Do not claim an isolated Host is healthy or storefront shipping labels work without TB-115 runtime proof.

RESULT CONTRACT — BRIDGE-WAKE-V1
Report Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Bridge-Task-Row, Expected-Base-SHA, actual Base-SHA and drift classification, Code-SHA, Evidence-SHA, Status, Stop-State=STOP_FOR_ARCHITECT, automaticNextImplementationTask=NONE, measured duration, exact DI failing boundary and corrected boundary, changed paths, focused test/build/guard counts, lifecycle/disposal/cancellation proof, negative control if done, shared non-interference, deviations and rollback. STOP FOR ARCHITECT.
END_TOOBA_TASK