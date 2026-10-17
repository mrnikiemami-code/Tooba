PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-123
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-122
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_SHIPPING_SELECTION_PAYMENT_HANDOFF_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 0204859dddf0eb726e48ad3b581fd2b5e7026940
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD. Aim to finish the one essential proof by minute 30; stop active investigation by minute 35, reserve 10 minutes for cleanup/evidence/result. One attempt per verification seam. No retry/relaunch loops. Report PARTIAL/BLOCKED rather than overrun.

ARCHITECT RULING
TB-122 conclusively proved, in a genuine isolated browser, that all seven rendered /fa/shipping method labels and prices match the enabled Fulfillment-owned one-level catalog and live shipping projection. Do not repeat this label-parity investigation. The next unproven customer-visible seam is the explicit shipping selection -> payment navigation handoff and preservation of the Order-owned draft without an early shipping/commit, order creation, cart clearing or payment action. TB-118 supplied code-level evidence only. This task is OBSERVATION/ACCEPTANCE ONLY, not a code-fix task. No general checkout or payment certification.

PREFLIGHT — FAIL CLOSED

Claim exactly one Pending task on tooba-main after Working heartbeat; keep docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-123.task.md UNSTAGED. Read AGENTS.md, bridge rules and the narrow TB-118/121/122 evidence and source paths. Fetch origin/main and verify Expected-Base-SHA is an ancestor. If overlapping changes or uncertain base, STOP RECOVERY_CONFLICT. Preserve all pre-existing dirty/untracked files; no stash/reset/rebase/force push.
Read-only establish shared :5088 / :3000 process identity, health/provenance, and tooba_alpha count baseline if feasible. Shared processes, browser tabs, databases, configuration and feature gates are IMMUTABLE. If isolation cannot be proven offline, STOP before launch.

MISSION A — ONE GENUINE ISOLATED BROWSER HANDOFF

Reuse the already validated TB-122 isolated scratch recipe; do NOT invent a new fixture or broaden the startup graph. One disposable no-volume PostgreSQL container with NEW loopback-only port, isolated app/messaging databases and environment-only ephemeral secrets; one isolated Development Host launch (external watchdog <=180 seconds); one isolated frontend launch with TOOBA_HOST_ORIGIN pointing to that Host, on unique loopback ports. Verify actual merged effective configuration offline before launch: exactly one canonical Active store-alpha tenant resolving only to scratch references; no tooba_alpha/tenant-alpha/shared port, no demo reset/seed expansion; ShippingOneLevelCatalog enabled ONLY in the disposable overlay; other shipping rate/operational gates unchanged/off. Verify runtime /health 200, /__platform-commerce scratch provenance and frontend proxy discriminator. Maximum ONE Host and ONE frontend launch, no restart on failure.
Establish the real guest-cart browser session only through the existing UI and authorized scratch endpoints/policy (as in TB-122); no fabricated session, fake identity, manual DB writes or copied customer secrets. Use a real available scratch offer. Reach /fa/shipping in the single task-owned browser tab and establish the visible seven-option projection as a precondition, without repeating exhaustive label screenshots.
Choose ONE representative shipping method with a stable methodCode; fulfill ONLY required address/date/window inputs through the actual UI, if the UI requires them. Record the exact selected methodCode and separately its serviceCode/optionCode from the browser-origin projection. Click the genuine «ادامه به پرداخت» button ONCE. Observe the resulting route and page without pressing a Pay/Confirm/Place Order button. Capture bounded sanitized browser network and DOM evidence proving the ordering: client selection -> at most the expected PUT /v1/storefront/shipping/selection -> successful navigation to /fa/payment (or precise blocker). Explicitly check no POST /v1/storefront/shipping/commit and no checkout/order/payment mutation was invoked by the shipping-to-payment transition. Read the shipping draft via an existing AUTHORIZED application read seam only if one exists; otherwise state that persistence is inferred only from successful save response plus payment UI readback, and DO NOT use direct SQL or add a diagnostic endpoint. Check whether the cart session remains present/usable and whether the payment page reflects the same selected method, to the extent observable.
If the UI cannot satisfy a required field, the payment page needs a different authorized flow, or no readback seam exists: STOP with first exact blocker and PARTIAL, rather than bypassing validation or creating missing data. Absolutely no order submit, shipping commit, capture/charge, shipment creation or external provider invocation.

MISSION B — BOUNDED EVIDENCE & NON-INTERFERENCE

NO src/frontend/tests/migrations/config changes. Do not run broad tests or repeat TB-118's 8/8 frontend test; runtime observation only. Single browser proof, one click at the customer handoff, no repeated retries. Capture concise redacted statuses, ordering of relevant requests, method identity, payment view and cart state. Never retain guest secrets, auth tokens, customer data, raw DB connection strings or broad logs.
Cleanup by verified task-owned absolute paths and recorded PIDs ONLY (Host and frontend); close only the task-owned browser tab; remove only the task-owned scratch container and ignored run/publish directories. Do not stop/restart shared services or containers. Read-only shared postflight comparison; report INCIDENT if anything differs, without attempting repair.
Commit/push ONLY docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-123.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-123/**. Bridge receipt remains UNSTAGED. Verify HEAD == origin/main after push. No automatic next implementation task.

ACCEPTANCE
PASS only if one real isolated browser session demonstrates: the explicit continue click saves an authorized shipping selection before navigation; payment route loads; selected method identity is retained at the observable owner/readback boundary; cart remains active; no shipping/commit or order/payment mutation occurs merely from navigation; and shared parity/cleanup are proven. If payment page does not expose shipping selection readback or other proof is incomplete, return PARTIAL precisely identifying the missing seam, not PASS by assumption. No claim of production readiness, payment acceptance or shipment lifecycle.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return Task-ID/Parent-Task/Channel/Worker-Id/Agent-Type/Bridge-Task-Row, Status PASS/PARTIAL/BLOCKED, Stop-State STOP_FOR_ARCHITECT, Expected-Base-SHA/actual Base-SHA/drift, Code-SHA NONE, Evidence-SHA, elapsed minutes, actual launch counts, essential UI/network/selection evidence, exact first blocker, changed paths, shared pre/post parity, cleanup, deviations, rollback and automaticNextImplementationTask NONE. STOP FOR ARCHITECT.
END_TOOBA_TASK
