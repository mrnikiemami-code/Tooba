PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-121
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-120
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_ONE_LEVEL_CATALOG_RUNTIME_LABEL_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 68e9648bb38cf98aceb670b48855e163655278f2
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD. Aim to finish within 30 minutes. Stop active investigation by minute 33; reserve remainder for cleanup/evidence. At most ONE isolated Host launch (external watchdog <=180 seconds). No loops, repeated test runs, broad suites or speculative fixes. Stop PARTIAL/BLOCKED when proof is not possible safely.

ARCHITECT RULING
TB-120 PASS installed the Fulfillment-owned Development-only, gate-controlled one-level catalog backfill using canonical ShippingMethodBackfillPlanner/IShippingOneLevelBackfillStore and trusted DevelopmentTenantCommerceContext scope. TB-119 proved seven real legacy projected shipping methods but the enabled one-level catalog was empty. TB-121 is OBSERVATION / RUNTIME PROOF ONLY. Establish whether a newly booted, fully isolated Development Host with the one-level gate enabled automatically populates catalog rows and serves matching Persian labels through the actual Fulfillment and Storefront projection seams. Do not claim UI DOM parity unless genuinely observed. Do not change code to force success.

PREFLIGHT — FAIL CLOSED

Claim exactly one Pending task on tooba-main after Working heartbeat; preserve docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-121.task.md UNSTAGED. Read AGENTS.md/pipeline rules, TB-119 and TB-120 evidence, the new seed and relevant Host/Fulfillment call sites.
Fetch origin/main; verify Expected-Base-SHA is ancestor and record any bookkeeping-only drift. Overlapping source edits or unclear provenance => RECOVERY_CONFLICT / STOP. Preserve dirty/untracked work; no reset, stash, rebase or force push.
Shared Host :5088, Next :3000, tooba_alpha and existing Docker containers are READ-ONLY. Never start, stop, restart, migrate or mutate them. Capture a minimal read-only baseline and later parity.

MISSION A — ONE ISOLATED DEVELOPMENT RUNTIME

Reuse the proven TB-117/TB-119 scratch recipe only: fresh disposable no-volume PostgreSQL on a UNIQUE loopback port, separate task-owned application/messaging databases, ephemeral password outside committed evidence; isolated Host published to task-owned ignored directory and bound to a UNIQUE loopback port. No shared connection reference, database, port or credentials.
Validate the EFFECTIVE configuration OFFLINE before launch: exactly one Active store-alpha pointing exclusively at scratch references; Development; RunLegacyBootstraps=true; AllowResetAndSeed=false; Tooba:ShippingOneLevelCatalog:Enabled=true ONLY in disposable overlay; ShippingRateAuthority and ShippingOperationalShadow absent/off. Reject any shared or ambiguous target. No tracked appsettings changes.
Launch Host exactly ONCE with absolute executable path and watchdog <=180 seconds; no relaunch or retry. Observe Development migrations, existing legacy Catalog bootstrap, new Fulfillment ShippingOneLevelCatalogDevelopmentSeed, any failure stage, /health and /__platform-commerce. If startup fails, record FIRST precise blocker and STOP investigation; do not patch code.
If healthy, use existing authorized READ endpoints to obtain GET /v1/admin/shipping-services/shipping-methods/one-level?language=fa with existing Development admin actor only if the existing sanctioned fixture supports it. Record row count, serviceCode/optionCode identities, localized names and active states. Confirm whether the seven legacy method identities are represented. Do NOT invoke activation, manual SQL insert, synthetic actor or unapproved bypass.
Where existing TB-119 guest-cart and GuestAllowed Development fixtures can be applied safely to SCRATCH via their authorized APIs only, perform ONE bounded storefront shipping projection and compare projected methods[].label/serviceCode/optionCode to the enabled one-level catalog. Avoid creating order, checkout, payment, shipment or committing selection. If prerequisite fixture cannot be established within bounds, report PARTIAL and the precise gate; do not improvise direct SQL.
An isolated frontend DOM render is OPTIONAL only if already supported by a safe, quick existing tool and can be completed inside budget without a second Host launch; otherwise explicitly mark UI parity NOT PROVEN and defer it. Never claim browser parity from API output alone.

MISSION B — MINIMAL VERIFICATION AND CLEANUP

NO new unit tests, broad suites, frontend rebuild loops, code changes, schema fixes, migrations outside the isolated standard startup, or feature activation beyond the isolated overlay. One observation pass per seam; one narrowly bounded correction for a probe scripting mistake is allowed against the SAME Host only if needed, with explicit evidence; no retry loop.
Stop ONLY the recorded task-owned Host PID after validating its absolute path. Delete ONLY the task-owned disposable container and ignored temporary directories. Confirm task ports closed; retain no password or secrets. Never terminate by process name or wildcard.
Compare shared PID/health/provenance and tooba_alpha read-only counts before and after. Any deviation => INCIDENT and STOP, no auto-repair.

EVIDENCE AND RESULT
Save concise docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-121.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-121/**: exact startup stages, gate provenance, method/option mapping, catalog rows, projected labels, any blocked UI proof, noninterference, cleanup and rollback. Commit/push task-owned docs/evidence ONLY; leave Bridge receipt UNSTAGED; verify HEAD == origin/main.

ACCEPTANCE
PASS if: one isolated Host with scratch provenance reaches /health 200; TB-120 seed runs without schema/tenant bypass; the enabled one-level catalog returns nonempty rows with valid service/option identity; and an authorized scratch storefront projection demonstrates label mapping sourced from the one-level catalog. If catalog rows exist but projection/identity gate blocks proof, return PARTIAL. If startup or seed fails, return PARTIAL/BLOCKED with the FIRST exact cause. PASS does NOT certify DOM parity, shipping selection persistence, checkout/payment or commercial readiness. Nonfatal warnings must be reported separately rather than repaired within this task.

RESULT CONTRACT — BRIDGE-WAKE-V1
Report Task-ID/Parent-Task/Channel/Worker-Id/Agent-Type/Bridge-Task-Row/Status PASS-PARTIAL-BLOCKED/Stop-State/Expected-Base-SHA/Base-SHA/drift/Code-SHA NONE/Evidence-SHA/automaticNextImplementationTask NONE; measured minutes; single launch/watchdog; config/provenance proof; catalog and projection evidence; precise first failure if any; shared parity; changed paths; cleanup; deviations; rollback; unproven claims. STOP FOR ARCHITECT.
END_TOOBA_TASK
