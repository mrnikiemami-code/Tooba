PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-113
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-112
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_CATALOG_ATTRIBUTE_COLLISION_RUNTIME_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 8f00b8f528291ff5a311cfa0e5ee4f2deba9dec7
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD (stop new work at minute 22; reserve 8 minutes for cleanup/evidence/result). NO OVERRUN.

ARCHITECT RULING
TB-112 source+focused-test repair reused the compatible Development Catalog attribute code color and its black option, addressing TB-111's catalog.attribute.code.duplicate collision. Eight tests passed, but EMPTY-SCRATCH RUNTIME remains UNVERIFIED. TB-111 already proved two-phase migrations, published schema-mobile-demo-phone, two sparse variants with one default, and two ACTIVE priced stocked offers; its Host later crashed on the legacy WorkspaceDemoProductSeed duplicate color definition. This task performs ONE and only ONE fresh isolated Development Host startup from CURRENT code to prove that exact collision is absent and identify first subsequent barrier. It is OBSERVATION ONLY: no source/test/config/recovery code edits, no new behavior or production release claim. A row count is not proof of customer/cart/storefront eligibility.

PREFLIGHT — STRICT

Claim exactly one Pending Bridge task on tooba-main, provide task row/receipt, preserve docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-113.task.md UNSTAGED. Read AGENTS.md, pipeline contract, TB-111/112 evidence and the TB-111 isolated recipes. Fetch origin/main; expected SHA must be ancestor; classify diff, STOP RECOVERY_CONFLICT on overlapping source change. Own bookkeeping drift may be reconciled only if non-overlapping. Preserve ALL existing dirty/untracked files; no reset/stash/rebase/force-push.
Establish READ-ONLY shared baseline first: port 5088 actual PID, /health, /__platform-commerce provenance and gate, port 3000 actual PID/status, and read-only tooba_alpha counts: 7 shipping methods, 14 translations, carts/lines/drafts/orders, products/offers. These are comparison expectations, NOT assumptions. Never stop/restart/kill/modify shared processes or databases.

MISSION A — ONE WATCHDOG-BOUNDED ISOLATED START

Build current-code Host to a NEW ignored task-owned output directory. Set up a fresh no-volume disposable PostgreSQL container with distinct loopback-only port (not shared 5432 and not prior scratch ports) and task-owned scratch application + messaging databases. Credentials temporary under ignored directory only. Before any Host launch, inspect the FULL EFFECTIVE config offline: precisely one Active store-alpha tenant, scratch-only connection references, zero shared connection strings, zero tooba_alpha/tenant-alpha references, no shared port/volume; provide fail-closed guard artifact. Do not log credentials.
Development-only scratch configuration: ASPNETCORE_ENVIRONMENT=Development; RunLegacyBootstraps=true; CatalogDemo AllowResetAndSeed=false; Tooba:ShippingOneLevelCatalog / ShippingRateAuthority / ShippingOperationalShadow absent or OFF. Do NOT execute /admin/catalog/demo/reset-and-seed or seed-only (545-product expansion). Use isolated task-owned Host port and content root. ONE LAUNCH, NO RETRY, external watchdog <=180 seconds. Do not make any shared Host/Next invocation aside from GETs.
Observe actual first startup outcome. Explicitly capture whether catalog.attribute.code.duplicate recurs, whether catalog-dev.attribute.incompatible-reuse occurs, whether published schema-mobile-demo-phone has 2 sparse variants with exactly 1 default, whether active Base-priced offers with positive stock exist, and whether /health reaches 200 before watchdog. If startup crashes, report the exact first new exception (type, code, module, source frame) and last completed stage. Do not fix it or relaunch. If healthy, record true HTTP /health and provenance from isolated Host; do NOT infer customer/cart/storefront proof. Never touch Checkout/Payment/Settlement or toggle production flags.
Read-only scratch SQL limited to status/quantity/referential integrity; prove unique color definition and stable black option, product/variant counts and default, active offers joined to corresponding price+stock, and any later seed data. If schema/table is missing, report missing as absent; never write fixtures or migrations by hand.

MISSION B — TEARDOWN + NON-INTERFERENCE

Stop ONLY the task-owned Host process, and only by verified absolute executable path plus PID; NEVER Stop-Process by name/wildcard or processes on shared ports. Remove only the task-owned scratch PostgreSQL container, run dirs and credentials. Confirm 0 listeners on task-owned ports and no scratch container. Never use broad PID cleanup.
Recheck shared :5088 PID, health, provenance/gate, :3000 PID/status, and tooba_alpha counts vs baseline, READ-ONLY. If an unexpected shared change appears, STOP with INCIDENT and record exactly what changed; do not silently repair or restart anything.
Save bounded, REDACTED evidence: docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-113.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-113/. No raw secrets, tokens, passwords, long stdout or ephemeral DB identifiers in committed artifacts. Docs/evidence only; NO src/, tests, migrations, Host config, frontend, scripts or shared data changes.

TIME MANAGEMENT
At minute 22 STOP all investigation and start teardown/report. A build/setup that cannot leave time for one <=180s start plus cleanup => PARTIAL/TIMEBOX_STOP without launching. No extra test suite, broad guard run, second launch, rerun, or opportunistic source edit. Hard wall-clock ceiling 30 minutes INCLUDING cleanup and posting evidence. Keep the next task delegated to Architect.

ACCEPTANCE / STATUS
PASS if one isolated run completes to HEALTHY and shows no color duplicate, one compatible attribute/option identity, two sparse variants/one default and priced stocked offer. PARTIAL if the collision is demonstrably gone and the first subsequent barrier is precisely captured (with complete teardown/parity). BLOCKED if isolation/config or trustworthy evidence cannot be established. Do not call it PASS merely because original error vanished while the Host crashed later.

RESULT CONTRACT — BRIDGE-WAKE-V1
Report Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Bridge-Task-Row, Expected-Base-SHA, actual Base-SHA, Code-SHA=NONE, Evidence-SHA, Status, Blocker-Code if any, automaticNextImplementationTask=NONE, Stop-State=STOP_FOR_ARCHITECT, measured minutes, number of launches and watchdog elapsed, actual HTTP result, original collision YES/NO, new first blocker, product/variant/default + offer/price/stock evidence, changed paths, exact cleanup and shared PID/data parity, deviations and rollback. Push only docs/evidence via normal git push, STOP_FOR_ARCHITECT.
END_TOOBA_TASK