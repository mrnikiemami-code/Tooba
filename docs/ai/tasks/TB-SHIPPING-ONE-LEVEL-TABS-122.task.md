PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-122
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-121
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_STOREFRONT_SHIPPING_DOM_LABEL_PARITY_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: b7060db9e363b6e47641dcc0d15626d962b68053
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD. Target completion <=30 minutes. Stop investigation/setup by minute 30; reserve final 15 for cleanup/report. No loops, repeat builds, broad test suites, or repeated environment launches. If prerequisites cannot be established safely, return PARTIAL/BLOCKED.

ARCHITECT RULING
TB-121 PASS is accepted for isolated API projection label parity: 7/7 methods match the enabled, populated Fulfillment one-level catalog. Browser/UI DOM parity is NOT yet proven. This task is OBSERVATION ONLY. Show whether the actual storefront /fa/shipping UI renders those same seven method labels and identities when bound to an isolated Development Host with a genuine scratch guest cart. Do not claim checkout, payment or production readiness. Do not modify shared environments or enable shared feature flags.

PREFLIGHT — FAIL CLOSED

Claim exactly ONE Pending Bridge task on tooba-main after heartbeat; preserve docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-122.task.md UNSTAGED. Read AGENTS.md/pipeline instructions and TB-119/120/121 evidence.
Fetch origin/main. Expected-Base-SHA must be ancestor; inspect source drift/dirty paths; if overlapping or ambiguous, STOP RECOVERY_CONFLICT. No reset/stash/rebase/force-push. Preserve all unrelated modified frontend files and evidence.
Read-only shared baseline if available: :5088, :3000, /__platform-commerce, and existing tooba_alpha counts. Never stop/restart shared processes or modify shared DB, appsettings, feature flags, frontend service, or containers.

MISSION A — MINIMAL ISOLATED BROWSER BINDING

First inspect existing frontend API base URL configuration, route /fa/shipping, and existing documented test/browser capture mechanisms. Choose ONE minimal safe, task-owned binding of an isolated Next/frontend instance to the isolated Host, without changing tracked files. Do not assume that a browser capture is possible; fail closed if no safe binding or session/token mechanism is available. No synthetic auth headers, mocked projection, substituted static HTML, fabricated cart/session or bypass of checkout identity.
Reuse TB-121's guarded isolated runtime recipe: ONE disposable PostgreSQL container (no volume, unique loopback port, task-owned DBs and ephemeral env-provided credentials), ONE published Host launch with external watchdog <=180 seconds and task-owned output/run dirs; distinct Host/frontend loopback ports; effective config offline guard validating scratch-only tenant/connection references, Active store-alpha, Development, RunLegacyBootstraps=true, AllowResetAndSeed=false, ShippingOneLevelCatalog Enabled ONLY in disposable overlay, ShippingRateAuthority/ShippingOperationalShadow absent. No launches/retries beyond this one Host instance. If isolated frontend requires a separate process, start it at most once, with an external bound and explicit task-owned PID; stop only by verified PID.
Use ONLY canonical existing APIs inside scratch to set GuestAllowed policy, create one guest cart and add one genuine offer line; obtain required browser cart session via established storefront mechanisms. No manual SQL writes, forged identity or production data. Do not commit shipping, create orders, call checkout/payment or mutate shipping selection merely for proof.

MISSION B — ONE FOCUSED DOM / UI PROOF

Verify isolated Host /health and /__platform-commerce provenance, then read one-level admin catalog via sanctioned Development actor and retrieve a single authorized storefront shipping projection. Require exactly seven methods and record canonical methodCode, serviceCode, optionCode, localized label without secrets.
Navigate the task-owned frontend instance to actual /fa/shipping using the genuine scratch cart/session. Capture browser-rendered DOM or reliable browser automation evidence (screenshot optional) for data-testid=shipping-page, shipping-steps, shipping-methods, and all shipping-method-{methodCode} elements. Compare visible labels + identities against BOTH scratch projection and one-level catalog for all seven. Verify hero steps/cart/shipping/payment semantics. If UI stays in the no-projection fallback, identify exact browser-side failure (routing/cookie/CORS/proxy/session) and STOP without attempting serial workarounds.
Do not click continue to payment, do not invoke PUT shipping/selection or shipping/commit, and do not claim selection persistence from read-only proof; TB-118 has prior focused source/test coverage. One observation pass, at most ONE narrowly scoped diagnosis if it fails. No full Playwright suite, no screenshots loop, no broad tests or code repairs in this task.

MISSION C — CLEANUP AND EVIDENCE

Stop only task-owned isolated Host/frontend PIDs verified by absolute path/launch record, remove only task-owned DB container and temporary dirs/secrets, confirm task ports closed. Never kill by wildcard/name and never touch shared :5088/:3000 or postgres-db/rabbitmq.
Read-only shared preflight/postflight parity. If change observed, report INCIDENT; do not silently repair.
Commit/push ONLY docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-122.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-122/**, retaining Bridge task receipt UNSTAGED. No src/, test, appsettings, migration, feature-gate, or frontend edits. HEAD==origin/main after push. Redact cookies, cart secrets, tokens, credentials, customer data and full connection strings.

ACCEPTANCE
PASS only with trustworthy real isolated browser-rendered DOM proof of 7/7 identities and Persian labels matching BOTH enabled Fulfillment one-level catalog and authorized shipping projection, while all isolation/cleanup guards hold. PARTIAL if API works but browser parity cannot be established; BLOCKED on unsafe binding/identity or missing prerequisites. Explicitly separate API success from UI success. No assertion of commercial readiness or payment/checkout completion.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return Task-ID/Parent-Task/Channel/Worker-Id/Agent-Type/Bridge-Task-Row; Expected-Base-SHA, Base-SHA/drift, Code-SHA NONE, Evidence-SHA, Status PASS/PARTIAL/BLOCKED, Stop-State STOP_FOR_ARCHITECT, automaticNextImplementationTask NONE; measured minutes, process launch counts, watchdog, trusted scratch provenance, DOM method count and 7/7 label/identity comparison or exact first blocker, shared parity, cleanup, precise changed paths, deviations, rollback and remaining non-claims. STOP FOR ARCHITECT.
END_TOOBA_TASK
