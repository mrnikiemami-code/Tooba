PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-STOREFRONT-PRIVATE-BROWSER-166
Parent-Task: TB-SHIPPING-STOREFRONT-OWNED-RATE-BROWSER-165
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 200d904204c2f491659844ffa80199ed08bc2a5b
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_REPRODUCIBLE_PRIVATE_SEED_AND_AUTHENTICATED_BROWSER_ACCEPTANCE
Timebox: HARD 45 minutes; stop and report PARTIAL if proof cannot finish; no endless retries.

MISSION
Complete the outstanding TB-165 authentic customer browser leg using a SELF-CONTAINED, reusable and strictly isolated scratch fixture. Prove method-owned exact-option prices and unavailable-method notices in the REAL storefront alongside two seller-specific shipping groups; preserve the existing default-OFF production flag. Do NOT substitute unit tests, mocked browser payloads or a new wireframe for real product proof.

PRE-FLIGHT

Verify HEAD == origin/main == Expected-Base-SHA; STOP if diverged, never rebase/reset/force-push.
Read the TB-159..165 RESULT evidence and the canonical SHIPPING-ADMIN-FLAT-METHOD-CONTRACT and SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT. Reuse the TB-154, TB-157, TB-164 harnesses and product bootstrap/seed mechanisms rather than inventing parallel business APIs.
Identify exact scratch seed dependency chain before spending time on the browser: tenant + sanctioned customer fixture, two real seller parties, one genuine purchasable offer per seller, stock/prices, category-specific active tax rules, geography chain, one-level catalog, method-owned tipax:standard 205000 IRR and tipax:express 480000 IRR policies, coverage declarations/grants as needed. Use valid real domain records and product-authorized endpoints/seeders; never fake client quotes or bypass permissions.

BOUNDARIES

Only task-owned loopback Host, Next and PostgreSQL, unique scratch database/container and ports. Fail-closed isolation guard BEFORE mutation; explicitly forbid shared :3000/:5088/:5432, tooba_alpha, shared store-alpha tenant data, RabbitMQ/Redis mutation, existing sessions and other workers. Scratch store-alpha hostname mapping is allowed only when the actual tenant id points to the unique disposable scratch tenant.
No final purchase, PSP, SMS, settlement payout, real carrier API, production data mutation, migrations on shared DB, hardcoded customer money, or invented parcel measurements/coverage. Keep all three committed appsettings ShippingOwnedRateAuthority.Enabled=false; turn it ON only in the task-owned runtime overlay.
No Admin redesign, global style changes, schema refactor, Host DbContext/business rules. Preserve existing unrelated dirty files and stashes.

EXECUTION (SINGLE BOUNDED SLICE)
A. Build an idempotent/clean-start scratch fixture script with an explicit prerequisites check and useful failure reasons. Use already-supported migration runner and product auth fixture. If fixtures cannot be safely created within timebox, STOP as PARTIAL with exact missing contract rather than bypassing the domain.
B. Launch private Host and private Next pointing ONLY to that Host; authenticate a genuine development customer. Observe actual GET/POST storefront path rather than constructing frontend state manually.
C. With owned-rate gate ON, establish two different sellers using SAME canonical tipax:standard: two genuine group cards, distinct server group IDs, distinct minimum dates if per-seller preparation is configured, each 205000 IRR and payment-draft total shipping 410000 IRR. Verify per-group dates/windows independently, Continue gates incompleteness, save -> payment preview -> back -> reload preserves IDs and schedules, no duplicated line membership or premature checkout/order/payment.
D. Prove unavailable method UI against a REAL server negative: deactivate express-owned rule OR set express Restricted without own grant via owner-authorized scratch write; projection unavailableMethods includes method-specific reason, page shows read-only Persian notice, express is not selectable, tipax:standard remains eligible at 205000, and failed attempt to select express cannot corrupt persisted draft. Do not attribute unavailable reason to a seller or give it a fabricated price.
E. OFF control (same isolated setup, safe Host relaunch with flag false) preserves legacy method prices and produces no unavailableMethods. Never activate flag in committed configuration.
F. Capture sanitized real HTTP JSON, DB readbacks, browser DOM assertions, screenshots, gate-on/off isolation proofs and cleanup logs. Secrets and fixture credentials must not be committed. Remove only task-owned resources by PID/path and container-name match, verify private ports closed and shared services remain intact.

PASS CRITERIA

Genuinely runnable reproducible scratch seed recipe + authenticated REAL browser proof of two cards, owned pricing, unavailable-method warning, group independence and back/reload, authoritative payment draft, negative PUT integrity and OFF parity.
Tested/observed no sibling/provider inheritance, no zero/FREE guess, stable ShippingGroupIds, no side effects on checkout/payment, and full cleanup.
Tests run only as necessary for code changes; no broad repeated suites.
If ANY mandatory live leg absent, report PARTIAL/BLOCKED and exactly what remained unverified, never PASS.

EVIDENCE / HANDOFF
Record scripts, assertions, screenshots and safe logs under docs/architecture/evidence/TB-SHIPPING-STOREFRONT-PRIVATE-BROWSER-166/; commit only scoped artifacts and necessary minimal fixes. Include exact Base/Code/Evidence/HEAD/origin SHA, source diff, test counts, isolation matrix, unresolved blockers, elapsed time. Fast-forward push only. STOP_FOR_ARCHITECT; Automatic-Next-Task: NONE.
END_TOOBA_TASK
