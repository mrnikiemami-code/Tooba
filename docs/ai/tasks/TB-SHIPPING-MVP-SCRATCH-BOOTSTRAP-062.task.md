PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MVP-SCRATCH-BOOTSTRAP-062
Parent-Task: TB-SHIPPING-MVP-ISOLATED-HTTP-061
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_ISOLATED_DEV_AUTH_BOOTSTRAP_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 5b83c7c826fa494ae2d838ddf88b8e45c41561a3
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT AUTHORITY
061 proved strong scratch isolation but failed because fresh Development startup yields admin actor and membership without role/capability tuples. This task repairs the canonical Development-only authorization bootstrap ONLY if the actual source of the omission is proven. Do not workaround authorization, inject tuples manually, touch real Tooba databases, enable broken legacy Catalog demo seeding, or restart the user's Host. Goal is an independently verified canonical bootstrap on a disposable scratch database; actual Shipping HTTP CRUD can remain for a subsequent task.

PREFLIGHT — STOP ON CONFLICT

Claim exactly one pending Bridge task on tooba-main after BRIDGE-WAKE, keep received task receipt unstaged. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md, master recovery Shipping checkpoint, and 059–061 reports/evidence.
git fetch; confirm HEAD==origin/main; check Expected-Base-SHA ancestry. Only documented evidence/bookkeeping-only fast forward with no source overlap is acceptable. Any other drift/conflict -> RECOVERY_CONFLICT/STOP. Preserve every unrelated dirty/untracked file; no reset, stash, rebase or force-push.
Trace source for AdminDevActorBootstrap.EnsureAsync, AccessControlDevelopmentSeedPrelude, AccessControlDirectory.EnsureBootstrapAsync and SyncUserCapabilityTuplesAsync, DevelopmentSchemaMigrator.ApplyAsync/MigrateSchemaOnlyAsync, migration ordering, startup flags and all existing relevant tests. Determine why role tables are EMPTY in 061 despite an actor. Do not assume a security bypass is the fix.

MISSION A — MINIMAL CANONICAL BOOTSTRAP REPAIR
Locate and correct ONLY the specific Development-only startup/seed ordering or registration defect that prevented the established platform-admin role, role_permissions and authorization tuples from being provisioned in an empty scratch database. Reuse the existing supported bootstrap logic, permission catalog and tenant scope. No direct fabricated role/tuple insert, manual DB seed to make tests pass, AllowAnonymous, capability bypass, production startup behavior change, or new permission identifiers. If correction would require relaxing locked Host Admin/Security guards, moving ownership across certified boundaries, editing unrelated Catalog demo seed, or significant architectural redesign, do NOT expand: record BLOCKED and STOP. Preserve idempotence and fail-closed auth; avoid automatic bootstrap in Production.

MISSION B — BOUNDED VERIFICATION, STRICT ISOLATION
Run essential unit/integration checks for canonical bootstrap ordering/idempotence and 401/403 denial behavior. If a real isolated startup is needed and fits the budget, reuse 061's proven scratch-only launcher guard on a NEW postgres:17 task-exclusive loopback container, unique database and credentials with no access to any existing database. Confirm actual effective connection target OFFLINE (060 guard, fingerprint/config SHA) AND from running process AND database-side before any writes. Run module-scoped migrations only on this disposable database as required; do NOT use the all-modules runner against existing databases. Never enable Tooba:CatalogDemo:RunLegacyBootstraps=true (061 proves it fails in unrelated Catalog). Desired scratch evidence: the supported actor+platform-admin bootstrap yields nonzero roles, role_permissions, user_role_assignments and a real 200 for authorized admin geography/rates GET while unknown/no actor still gets 401/403; repeat start if safe to prove idempotence. If this cannot be achieved in 30 minutes, report PARTIAL without inventing success. Shipping mutation roundtrip and null-scope HTTP 400 may be deferred to the next bounded task; do not expand scope.

ALLOWED PATHS

The minimal EXISTING source file(s) implementing Development-only Admin/AccessControl bootstrap discovered in Mission A, only if a proven defect demands modification. State exact paths and boundary proof in result; do not add files under frozen Host/Admin or Host/Security trees.
Existing relevant bootstrap test project files (focused tests only).
docs/planning/shipping/SHIPPING-MVP-SCRATCH-BOOTSTRAP-062.md
docs/architecture/evidence/TB-SHIPPING-MVP-SCRATCH-BOOTSTRAP-062/**
docs/ai/tasks/TB-SHIPPING-MVP-SCRATCH-BOOTSTRAP-062.task.md receipt only, never stage.

FORBIDDEN

ANY connection/query/write against tooba_alpha, localhost:5432 or any pre-existing DB; no Dev/Production schema mutation, no real Host :5088 restart.
No hand-entered role/tuple, credential fabrication, direct capability grants outside the canonical code, disabling auth, permission-catalog modification, guard weakening, or legacy Catalog demo bypass.
No Shipping/Geography commercial rows or default countrywide coverage, Checkout/Order/Payment/rate cutover, frontend rewrite, broad refactoring or speculative seed changes.
No generated large logs, unbounded polling, hidden task continuation, fake PASS, forced git operation.

ACCEPTANCE / STOP
PASS only with source-backed root cause, minimal safe fix if required, and focused proof canonical Development bootstrap provisions the established admin capability without bypass, while unauthorized users remain denied. If existing standard bootstrap is impossible without broader changes, BLOCKED with exact code evidence and smallest proposed follow-up. Never label PASS from actor membership alone. <=30-minute hard stop, report actual elapsed. No requirement to complete Shipping HTTP write flows in this task.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MVP-SCRATCH-BOOTSTRAP-062
Parent-Task: TB-SHIPPING-MVP-ISOLATED-HTTP-061
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_ISOLATED_DEV_AUTH_BOOTSTRAP_REPAIR
Bridge-Task-Row: <actual claimed row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 5b83c7c826fa494ae2d838ddf88b8e45c41561a3
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual>
automaticNextImplementationTask: NONE
Report exact root cause, safe solution, changed paths, focus tests, scratch isolation/provenance if runtime used, authority evidence, remaining blockers, actual elapsed, commit+push; never reveal credentials.
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
