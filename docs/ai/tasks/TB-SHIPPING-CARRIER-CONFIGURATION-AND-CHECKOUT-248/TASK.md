PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-CARRIER-CONFIGURATION-AND-CHECKOUT-248
Parent-Task: TB-SHIPPING-CARRIER-ADAPTER-FOUNDATION-247
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — owner-configured carrier API through existing checkout
Mode: CONNECT_OWNER_CARRIER_SETTINGS_REFERENCE_MAPPING_AND_CAPTURED_CHECKOUT
Expected-Base-SHA: 9d3f0ad296dec1d26dc25fb51a4b0d949ccf4f8b
Status: ISSUED
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

1 — User outcome

Implement the next delivery from the checked-in carrier plan. The merchant must be able to configure a carrier account, choose its real service, map origin/destination, and use API pricing through the EXISTING shipping read → selection → payment preview → final acceptance path. Tipax V4 is the first adapter; another carrier must be additive. Manual and Free methods remain usable.

This is IMPLEMENTATION, not another report or adapter-only foundation. Do not claim runtime activation from DI registration, mock HTTP or a green build.

Read AGENTS.md, module-local instructions, the 247 evidence, and:
docs/planning/shipping/CARRIER-ADAPTER-AND-TIPAX-V4-PLAN-2026-11-02.md
Official provider contract:
https://document.tipaxapi.org/#v4-docs
https://omtestapi.tipax.ir/swagger/v4/swagger.json

2 — Canonical environment and startup checkpoint

Use the user's existing SarvNewVer checkout, frontend :3000, Host :5088, SingleStore/store-alpha/tooba_alpha. Inspect actual identity and source/runtime parity before mutation. Do not replace this with a task DB, buyer, cart, port or profile and call that the delivery. Existing Marketplace owner isolation remains binding.

Preserve the real account/cart, user tabs and unsaved edits. No screenshots requested or required. Do not automate a user's open editing tab. Use an independent authorized test session when required.

First checkpoint (within 15 focused minutes):

Confirm exact repo/base, runtime, DB and edition.
Inspect existing secret storage, owner settings, geography mapping and adapter consumers.
State file-level plan, migrations needed and credential availability.
Check the 247 implementation against the official contract before using it: units, token lifetime/refresh, response validation and all required request fields.
Report any missing dependency immediately; continue independent implementation rather than waiting silently.
3 — Architectural ownership

Follow existing AMSC/module boundaries. Shipping owns carrier configuration and provider execution. Geography owns internal destination identity; external mappings must use its Contracts or a lawful integration mapping boundary, never a foreign DbContext. Fulfillment owns canonical method/service identity. Order owns accepted financial/delivery evidence.

Reuse ICarrierRateProvider + registry. Provider-specific wire models stay in Infrastructure. No Tipax switch in Order, frontend or Domain, no second pricing engine, no foreign persistence access, no fake owner/role, no hardcoded merchant/city/service identifiers.

Provider credentials MUST be resolved for the exact owner/account. The 247 process-global options/token cache is a foundation only: do not reuse one account/token across sellers or tenants. Scope/cache tokens by account identity and configuration version; rotation/disable invalidates prior credentials. No secret values in DTO reads, URLs, logs, git or persisted public configuration.

4 — Required implementation
A. Carrier account and connection settings

Implement on existing shipping administration surfaces:

Owner-scoped carrier profile: provider code, environment, secret reference, active/inactive state.
Reuse the platform secret facility. If none exists, implement the smallest secure server-side boundary consistent with repo architecture; never persist plaintext credentials in Shipping rows or invent a production secret store.
Explicit connection test with actionable localized status.
Real reference lookup/sync for provider services, payment/pickup/distribution/packing/package-content values.
Store selected reference identities, never infer numeric provider IDs from labels.
Neutral capabilities make unsupported provider operations explicit.
Validate own/foreign account access for SingleStore and Marketplace seller/platform owners.
B. Method, origin and destination mapping

For CarrierApi calculation mode:

Select configured carrier account + provider service/reference options.
Configure real origin address and coordinates when required by the provider; existing admin UI should clearly show missing required inputs.
Map internal Geography destination IDs to carrier external IDs with explicit provenance and uniqueness.
Missing mapping/weight/dimensions/value refuses as configuration/request incomplete; no guessed city, guessed package weight, zero-price fallback or silent Manual substitution.
Only show applicable fields; keep manually defined tariff rows preserved when toggling Manual/Free/API. Inactive rules must not price the API result.
Switching to Free produces the established Free behavior without deleting saved Manual tariff configuration.
Use the repository frontend skill before UI changes. Read language registry and currency from their owners; no hardcoded two-language assumption.
C. Captured API quote through checkout

Connect the existing captured pipeline to the neutral carrier port:

Build provider inputs from authoritative package/goods/origin/destination facts.
Use the provider's returned payable amount/components and known currency/unit conversion; never add tax/packaging twice.
Persist only necessary quote evidence in the existing accepted selection graph with an additive, reviewed migration if needed: provider/account/service identity, configuration version, quoted inputs identity, exact amounts/currency, issued/expiry policy and provider quote reference when actually supplied.
A client-generated correlation reference is NOT a provider-issued quote or reservation.
Respect provider expiry when documented. If absent, define an explicit local freshness policy and label it as local.
Revalidate selection and final acceptance without recursive quotes or hidden provider calls on render.
Changed amount/service/inputs or expired context requires reconfirmation; no silent repricing.
Payment preview and final accepted Order use the same financial context; historical Order reads do not call the carrier.
Distinguish delivery estimates from guaranteed receiving slots. Do not manufacture hourly appointments for a carrier that has not promised them.
Typed localized refusal for disabled account, auth failure, missing mapping, no service, timeout, invalid response and stale quote.
Cancellation propagates. Bound timeouts and retries; no automatic shipment creation or payment side effect during a quote.
Retain Manual/Free behavior and current destination-refresh/selection/summary/navigation fixes.
5 — Boundary of this task

Plan slices R1/R2/R3 are the mandatory scope here. Shipment creation/label/tracking/cancel (R4) is a separately stated lifecycle capability; do not imply it exists because pricing works. This task must record the exact remaining lifecycle contract and dependencies without starting an automatic next task.

Do not activate production or send a real shipment. Credentials are not available in the chat; inspect sanctioned local secret configuration without exposing it. If a Tipax test account is absent, deliver implemented inactive configuration + checkout integration with controlled contract tests, and mark LIVE PROVIDER ACCEPTANCE BLOCKED BY TEST ACCOUNT. Do not fabricate login success or mark all-scope PASS.

6 — Verification and acceptance

Run focused checks only, then build affected projects/frontend once:

Disabled/incomplete profile: zero network calls.
Own account allowed; foreign tenant/seller/account denied.
Rotation/disable cannot reuse the old cached token.
Exact official wire, grams/dimensions/value units, malformed/negative/missing response and unknown currency refused.
Real provider reference mapping; absent/foreign mapping blocked.
API read → real selection writer → preview → final acceptance preserves own per-group money/evidence; Manual/Free controls stay lawful.
Expired/changed quote refuses before reservation/order; unchanged acceptance succeeds.
Timeout/provider failure/caller cancellation: bounded requests and zero financial side effects.
UI input changes do not cascade into duplicate calls; no quote on every keystroke; same-tick save/final-click containment preserved.
Old Order evidence remains unchanged after carrier configuration/quote changes.

Controlled provider HTTP tests are valid contract evidence, not live provider evidence. Use disposable DB only for relational tests, identified before DDL; it does not replace canonical delivery.

If sanctioned test credentials exist, run a real read-only connection/reference/pricing trial. Use canonical buyer flow only with an explicitly suitable test cart; do not commit the user's current cart without permission. No screenshot/visual-acceptance gate.

7 — Execution discipline and delivery

At checkpoint provide a realistic estimate from inspected dependencies, separating implementation from provider-account wait. Do not invent a 15-minute completion promise for the whole scope. Report progress at 30 focused minutes and before any projected overrun; keep one cumulative ledger, no reset clocks.

Do not reopen unrelated UI, navigation, seeds, taxonomy, Outbox or PSP work. Do not weaken a guard to turn tests green. Stop only on a concrete architectural/external blocker; document exact completed code and remaining dependency.

Commit and push normally, no force/amend/reset/user stash. Preserve user's local work. If base moved, reconcile source deliberately and report actual source base. Build canonical runtime from the delivered tree only when safe, without discarding unsaved work. Keep checked-in activation defaults OFF until real configuration and acceptance justify local activation.

8 — Evidence, recovery and result

Preserve this TASK verbatim:
docs/ai/tasks/TB-SHIPPING-CARRIER-CONFIGURATION-AND-CHECKOUT-248/TASK.md
Evidence:
docs/architecture/evidence/TB-SHIPPING-CARRIER-CONFIGURATION-AND-CHECKOUT-248/{RESULT.md,RECEIPTS.md}

Update AGENTS.md and both recovery TOPs with actual current state, source/evidence SHA, runtime parity, remaining lifecycle/credential dependencies and no automatic continuation.

Return one BRIDGE-WAKE-V1 worker result with:
Task-ID / Parent-Task / actual Base-SHA / Code-SHA / Evidence-SHA
Status PASS|PARTIAL|BLOCKED / Stop-State STOP_FOR_ARCHITECT
Usable canonical URL + exact usable stage
Owner/account/settings and mapping receipts (sanitized)
Quote-to-Order evidence and focused checks
Live vs controlled boundary
Data/migration/runtime mutations and preservation
Elapsed actual time, remaining gaps
automaticNextImplementationTask: NONE

PASS requires all mandatory scope plus real test-account acceptance. Otherwise report PARTIAL/BLOCKED honestly. A ready adapter alone is not an API-enabled checkout.
END_TOOBA_TASK