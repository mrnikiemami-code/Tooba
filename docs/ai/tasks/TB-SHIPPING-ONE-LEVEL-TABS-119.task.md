PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-119
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-118
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_STOREFRONT_SHIPPING_PROJECTION_AND_LABEL_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 141844f038977479ee8091b1a17f258af5c74303
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD. By minute 30 stop investigation/setup, reserve 15 minutes for cleanup, evidence, commit and report. One attempt per proof; no debugging loops, no repeated tests, no broad sweeps. If the bounded real proof is not feasible, report PARTIAL/BLOCKED and stop.

ARCHITECT RULING
TB-117 proved isolated Development Host startup healthy. TB-118 proved the shared storefront hero renders but projection is identity-gated (401 checkout.authentication_required), and one-level labels are fail-closed OFF with Tooba:ShippingOneLevelCatalog absent. The shipping selection code saves the draft only on explicit Continue and has no frontend caller for early commit. This task is a single isolated end-to-end READ proof of actual shipping projection and one-level label/option rendering. It is NOT a production cutover or shipping commercialization task. Never change shared :5088/:3000/tooba_alpha, never bypass checkout identity or authorization, never misrepresent seeded rows as storefront eligibility.

PREFLIGHT — FAIL CLOSED

Claim exactly ONE pending task on tooba-main. Preserve docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-119.task.md UNSTAGED. Read AGENTS.md/protocol, TB-117 and TB-118 reports, and relevant source/fixtures.
Fetch origin/main; Expected-Base-SHA must be an ancestor. Examine source drift/dirty paths. Overlapping or ambiguous changes => RECOVERY_CONFLICT and STOP. No reset/stash/rebase/force-push, no adoption of unrelated work.
Optional read-only shared baseline: :5088/:3000 PIDs, health/provenance and exact tooba_alpha counts. Shared Host/Next/DB are NEVER a test target for mutation. No shared process lifecycle actions.

MISSION A — AUTHORITATIVE FEASIBILITY CHECK (MAX 10 MIN)

Trace one existing sanctioned Development scratch bootstrap/fixture route that yields a real scratch cart/draft, a real approved checkout identity (GuestAllowed only if configured through the canonical policy seam), applicable shipping methods, and real storefront projection. Determine whether one-level label source can be populated via existing module-owned Development setup without enabling a shared flag, direct SQL, synthetic HttpContext, arbitrary identity bypass or cross-tenant fallback.
If an authorized cart + identity + methods + label catalog cannot be established using existing approved isolated fixtures within the task, STOP with exact blocking seam and minimal next design. Do not invent a backdoor, endpoint, broad seed routine or test harness.
Work out how isolated frontend /fa/shipping will actually call the isolated Host (not shared :5088). Verify provenance for both ends before proof. If frontend cannot be isolated safely and promptly, limit proof to actual authorized isolated API projection and explain UI gap; no claim of visual parity.

MISSION B — ONE ISOLATED PROOF (ONLY IF PREFLIGHT PASSES)

Reuse TB-117's fail-closed offline guard: one disposable scratch PostgreSQL with unique loopback port, task-owned app + messaging DB, one Development Host on unique loopback port, ephemeral secrets, no shared connection reference or port. No volume, no reset-and-seed, no 545-product expansion. Enable Tooba:ShippingOneLevelCatalog ONLY in the disposable isolated overlay, never in tracked appsettings or shared environment. RunLegacyBootstraps=true only if needed by the existing safe recipe.
Exactly ONE isolated Host launch; external watchdog <=180 seconds for startup. No relaunch. After /health 200, verify /__platform-commerce references only the scratch tenant/database. Any mismatch aborts without HTTP writes.
Use only existing authorized module-owned scratch fixture actions, at most one minimal scratch cart/draft setup and one approved identity/policy setup if needed. Scratch writes, if unavoidable, must be explicitly listed and solely through existing authorized application APIs/seeds. NEVER manual SQL DML/DDL, fake headers/actors, authentication bypass, direct DbContext writes, checkout commit, payment capture, order creation or shipping label activation in shared config.
Attempt ONE authenticated/authorized POST /v1/storefront/shipping/projection on scratch. Record status, option codes, canonical method labels, one-level display label mapping and source provenance with personal data redacted. If the isolated frontend can be safely bound, render /fa/shipping once against scratch and record shipping-method testids, label text and screenshot or DOM evidence. Do not claim UI evidence if API-only.
Selection preservation: prefer existing TB-118 source + 8/8 focused test evidence. Do not issue an actual PUT/commit merely to prove navigation. Explicitly distinguish observed API/DOM from static source/test conclusions.
If first substantive blocker occurs, capture first error and stop. No retry, no feature-creep fixes, no additional services, no broad regression sweeps. The nonfatal returns/settlement outbox 42P01 warnings from TB-117 are outside scope unless they directly block this proof.

MISSION C — CLEANUP / REPORT

Tear down ONLY task-owned Host PID after absolute executable-path verification and the task-owned scratch container; delete ignored per-run files/secrets. Never terminate by wildcard/name. Verify scratch ports closed.
Read-only shared postflight comparison; any shared PID/provenance/count change => INCIDENT and STOP, no silent repair.
Write docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-119.md and concise docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-119/**. Redact all secrets, tokens, customer identifiers and full connection strings. No source/config/migration/frontend edits in this proof-only task.
Commit/push ONLY task-owned docs/evidence, verify HEAD==origin/main; leave Bridge receipt UNSTAGED; STOP_FOR_ARCHITECT.

ACCEPTANCE
PASS only if actual isolated, authorized storefront projection returns methods AND proves one-level display labels originate from the enabled catalog under scratch provenance, with no shared mutation. UI parity may be marked PASS separately ONLY with genuine isolated frontend DOM/screenshot evidence. If API proof passes but UI cannot be isolated, report PARTIAL for the full mission and specify the precise remaining gap. If setup is unavailable, report BLOCKED with the canonical missing dependency. No shipping commercial-readiness, checkout cutover or payment claim.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return Task-ID/Parent-Task/Channel/Worker-Id/Agent-Type/Bridge-Task-Row, Expected-Base-SHA, Base-SHA and drift, Code-SHA NONE, Evidence-SHA, Status PASS/PARTIAL/BLOCKED, Stop-State STOP_FOR_ARCHITECT, automaticNextImplementationTask NONE, measured minutes, exact proof source and authority/provenance, permitted scratch writes if any, projection response/labels and UI evidence if any, first blocker, shared parity, changed paths, cleanup, deviations and rollback. STOP FOR ARCHITECT.
END_TOOBA_TASK