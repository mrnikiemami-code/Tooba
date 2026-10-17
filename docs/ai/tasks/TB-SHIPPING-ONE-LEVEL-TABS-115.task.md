PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-115
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-114
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_LEGACY_CATALOG_DI_RUNTIME_AND_HOST_HEALTH_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: af836cc992f9cdc40425d98d0169eba7c39b37e5
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD. Stop investigative/implementation work by minute 22, reserve time for cleanup/evidence; report PARTIAL/BLOCKED if deadline is reached. One launch only, outer watchdog <=180 seconds.

ARCHITECT RULING
TB-114's code repair is accepted for the DI composition boundary, but has NOT been runtime-proven. This task is OBSERVATION ONLY. Prove the actual current-code empty-scratch Development startup crosses the former FashionTemplateCatalogSeed CatalogDbContext root-provider exception. Record whether /health 200 is reached and, if not, identify the FIRST subsequent exception precisely. Do not code around a new error.

PREFLIGHT — FAIL CLOSED

Claim exactly one Pending Bridge task on tooba-main after heartbeat, preserve docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-115.task.md UNSTAGED; read AGENTS.md and relevant pipeline rules.
Fetch origin/main and compare Expected-Base-SHA. Only non-overlapping earlier task-result/evidence bookkeeping drift is acceptable. Any overlapping code drift -> RECOVERY_CONFLICT / STOP. Preserve all pre-existing dirty/untracked files; no reset/stash/rebase/force-push.
Read TB-113 scratch recipe and TB-114 root-provider correction. Record read-only shared baseline: :5088 host PID, health and provenance; :3000 PID/status; tooba_alpha method/translation/cart/line/draft/order/product/offer counts. No shared process or DB mutation.

MISSION A — SINGLE ISOLATED RUNTIME START

Prepare NEW disposable PostgreSQL container, no volume, loopback-only port unique to task 115, with task-owned application/messaging databases and ephemeral credentials. Build/publish current HEAD Host into task-owned ignored directory. Use unique loopback Host port and task-owned run/content root; never use :5088/:3000 or the shared PG port.
Before starting Host, validate the ACTUAL merged effective config offline: exactly one Active store-alpha tenant; both database references point solely to scratch; no tooba_alpha, tenant-alpha or shared port; Development environment; RunLegacyBootstraps=true; CatalogDemo AllowResetAndSeed=false; ShippingOneLevelCatalog, ShippingRateAuthority, ShippingOperationalShadow all OFF/absent. Abort on any mismatch.
Start isolated Host ONCE with a watchdog <=180 seconds. Absolutely no second launch/retry, no 545-product reset-and-seed/seed-only, no raw fixtures, no manual SQL writes, no bypass or alternate startup entry point.
Determine, with redacted bounded logs: whether DevelopmentSchemaMigrator completes, whether the Catalog attribute/legacy workspace seed completes, whether FashionTemplateCatalogSeed progresses past the former scoped-service failure, and whether all subsequent legacy seeds complete. Attempt GET /health and /__platform-commerce ONLY if Host stays alive; report exact status/provenance. If startup fails, capture the FIRST new exception and its stack, last completed stage and responsible source location. Do not fix it in this task.
On scratch DB ONLY, read back (if tables exist): product status, exactly two sparse variants and one default for schema-mobile-demo-phone; offer-active/base-price/positive-stock join counts; distinct color/black identity, schema migrations; whether new template/landing/menu rows exist. Row presence is NOT a storefront-eligibility proof. Do not create cart/order or change shipping flags.

MISSION B — CLEANUP AND NON-INTERFERENCE

Stop only the task-owned Host PID after verifying its absolute executable path; never use name/wildcard/broad termination. Remove only task-owned scratch container and ignored run/output files, including secrets. Ensure task ports are no longer listening. Do not stop/restart the shared Host or Next.
Read-only postflight shared parity: same :5088 and :3000 owning PID/provenance/health and identical tooba_alpha counts; report any deviation as INCIDENT, never silently repair. Redact passwords, tokens and customer identifiers from committed evidence.
Commit and push ONLY task-owned docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-115.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-115/; task receipt remains UNSTAGED. Zero src/, migrations, appsettings, script, test or shared-data changes.

ACCEPTANCE
PASS only if the root-provider error is absent, isolated Host reaches /health 200 with scratch provenance, and the full startup stage is documented. If the old error is eliminated but a later one prevents healthy Host, return PARTIAL with exact FIRST new blocker; if old error recurs, return BLOCKED with proof. Do NOT claim storefront label parity, cart reachability, checkout cutover or commercial shipping readiness from this run.

RESULT CONTRACT — BRIDGE-WAKE-V1
Report Task-ID/Parent-Task/Channel/Worker/Mode/Bridge-Task-Row/Status/Stop-State/Expected-Base-SHA/Base-SHA/Code-SHA NONE/Evidence-SHA/automaticNextImplementationTask NONE; measured minutes; launch count/watchdog; stage-by-stage proof; first exception or health; scratch rows; shared PID/data parity; exact changed paths; cleanup; rollback; any deviations. STOP_FOR_ARCHITECT, no automatic next task.
END_TOOBA_TASK