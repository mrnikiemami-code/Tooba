PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-102
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-101
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_STOREFRONT_LABEL_PROJECTION_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 09870abc0140c0366696a07fa868f79307f25221
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD — STOP at deadline and return PARTIAL/BLOCKED; do not finish late.

ARCHITECT RULING
101 is accepted PARTIAL: new Host deployed on shared Development but no accessible, authenticated cart permits a truly read-only storefront projection. The missing proof MUST be obtained on a fully disposable isolated PostgreSQL + current-code Host, with a synthetic legitimate cart and supported authenticated identity. This task AUTHORIZES only scratch-database seed/cart writes required to reach the real POST /v1/storefront/shipping/projection read-only evaluation. NEVER write to shared tooba_alpha or shared :5088/:3000. No order placement, checkout/payment initiation, changing price or method eligibility. A reported 100 source-level unit test does not count as HTTP proof.

PREFLIGHT — HARD SAFETY GATE

Claim exactly one pending task on tooba-main and preserve the task receipt UNSTAGED. Fetch main; verify Expected-Base-SHA ancestry. Accept only non-overlapping previous result bookkeeping drift; STOP RECOVERY_CONFLICT on src overlap. Preserve every dirty/untracked file. No reset/stash/rebase/force-push.
Read AGENTS.md, TOOBA pipeline, 099 isolated auth harness, 100 Order/Fulfillment label seam/tests, 101 reachability findings and 101 host evidence. Identify the minimal existing supported cart-creation/authentication path and storefront projection request. Do NOT invent raw schema fixtures that evade normal cart access rules. Prefer API-created synthetic cart and legitimate credential login; if this cannot be achieved within the bounded scope, STOP BLOCKED_ISOLATED_CART_PROOF.
Capture SHARED :5088/:3000 health and provenance, catalog counts and cart counts with READ-ONLY operations. Do not restart, signal or configure shared services. Record no credentials or PII.

MISSION A — PROVEN SCRATCH ISOLATION + REAL CART

Reuse 099's scratch-only pattern on a NEW disposable loopback PostgreSQL container and database with task-generated credentials kept only in ignored ephemeral files. Do not clone user/customer/order data. Use migrations and minimal supported Development/test bootstrap to create a real authenticated scratch actor, a storefront-eligible legacy method post:standard, a corresponding one-level method with a deliberately distinct fa name such as «پست معمولی آزمایشی», its valid language translation, and an ACTIVE cart owned by that actor with the minimum genuine cart line/item/address data needed for projection.
Before any scratch DML prove offline effective config isolation (no tooba_alpha; all connections resolve to scratch only), Host /__platform-commerce indicates isolated deployment and scratch connection, and database-side pg_stat_activity cannot touch shared database. Gate Tooba__ShippingOneLevelCatalog__Enabled=true only in isolated Host process. Tooba:ShippingRateAuthority and Tooba:ShippingOperationalShadow stay OFF. Keep credentials out of git/evidence.
Obtain a genuine authorized session through the existing login mechanism, not an invented admin/customer actor header or ACL bypass. If no valid owned cart and approved scope can be created safely, stop with BLOCKED and report the exact access gap; no shared fallback.

MISSION B — REAL STOREFRONT PROJECTION PARITY

Capture a baseline from the scratch storefront projection with the one-level gate OFF and the same authenticated cart: methodCode post:standard, legacy label, method list/order, prices, lead days, flags and available other fields. Use the actual HTTP POST /v1/storefront/shipping/projection, which is semantically read-only. Do not touch payment, create orders, or confirm checkout.
With ONLY isolated gate ON, call the SAME endpoint with the SAME cart, request scope, language and identity. Assert the method code and full eligible set are identical, and the sole intended change is human Label for post:standard to the one-level fa translation. Assert prices, lead days, isFree, service/option codes and customer payment fields are byte-for-byte identical across responses. Missing translated language => legacy label fallback if time allows; do not expand scope.
Save sanitized actual request shape, response status and narrowly redacted before/after fields, without bearer/session/guest secrets or real customer identifiers. If HTTP projection fails (auth, no cart, invalid physical inputs), report honest PARTIAL/BLOCKED; do not claim success from a direct Fulfillment admin read or a unit test. An isolated frontend screenshot is OPTIONAL and must not delay this proof.

MISSION C — CLEANUP / REPORT

Stop isolated Host; remove scratch containers, test credentials and task-owned run dirs; verify port closed and the shared :5088/:3000 processes, provenance and tooba_alpha row counts unchanged. No live database DML at all.
No production code edits expected. If source issue appears, STOP and report for architect rather than expanding scope. Run only the minimal focused test if one is relevant; avoid broad Host suite and its unrelated baseline failures.
Produce docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-102.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-102/** with scratch isolation, provenance, authentication/cart creation evidence, both genuine HTTP responses, invariant comparison, cleanup and exact elapsed minutes. Commit/push only these task-owned docs and evidence; receipt UNSTAGED. Do not modify master recovery unless expressly in scope.

FORBIDDEN

Any shared :5088/:3000 restart, mutation of shared tooba_alpha or real carts/orders, real shopping transaction, checkout price authority/ShippingRateAuthority/ShippingOperationalShadow cutover, Payment/Settlement/COD changes.
Credential guessing, ACL/identity bypass, fake storefront response, hand-built fake HTTP route, claiming a test-only fake as genuine live proof.
Timebox overrun; if 30 minutes approach, finish cleanup and return PARTIAL/BLOCKED immediately.

ACCEPTANCE
PASS only if authenticated actual HTTP storefront projection on an owned synthetic cart proves gate OFF vs ON label substitution with all other material fields identical, plus scratch-only provenance and complete cleanup. PARTIAL if partial genuine proof exists; BLOCKED if safe real cart/bootstrap/projection is inaccessible. No further task automatically. STOP_FOR_ARCHITECT.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode, Bridge-Task-Row, Status PASS/PARTIAL/BLOCKED, Stop-State STOP_FOR_ARCHITECT, Expected-Base-SHA, Base-SHA, Code-SHA, Evidence-SHA, automaticNextImplementationTask NONE; exact changed paths, actual tests, timed minutes, scratch startup/auth/cart provenance, actual gate-OFF/ON HTTP status and label/price parity, scratch cleanup and shared-state read-only parity.
END_TOOBA_TASK