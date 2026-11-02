PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-CARRIER-CONFIGURATION-AND-CHECKOUT-248-R1
Parent-Task: TB-SHIPPING-CARRIER-CONFIGURATION-AND-CHECKOUT-248
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — complete carrier capability and canonical deployment, credentials deferred
Mode: FINISH_QUOTE_EVIDENCE_SHIPMENT_LIFECYCLE_AND_DEPLOY_WITHOUT_PROVIDER_ACCOUNT
Expected-Base-SHA: 3639acca97cee377f58be3ab7e92784bb1fced97
Status: ISSUED
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

1. Binding user decision

Finish ALL software work so the sole deferred acceptance is actual communication with Tipax using credentials.
The user explicitly does NOT want a test account now. Do not request one, search private credential files, invent credentials, or stop implementation because credentials are absent.

Deliver on the user's canonical SarvNewVer checkout, FE3000 → Host5088, SingleStore/store-alpha/tooba_alpha. Preserve Marketplace owner support. No alternate port, account, cart or database may substitute for canonical delivery.

Override 248's "PASS requires real test account" criterion: PASS here means SOFTWARE_COMPLETE_DEPLOYED_PROVIDER_UNVERIFIED. Live provider certification remains explicitly deferred. Manual/Free must work while CarrierApi is unconfigured.

Read AGENTS.md, affected module instructions, 247/248 evidence and carrier plan. Verify actual source base; if main advanced, reconcile normally and report actual base. Preserve this task verbatim before implementation.

2. Fast execution with honest planning

Within 15 focused minutes: inspect 248 diff and identify exact remaining paths, pending migrations, runtime build provenance and a concrete remaining-work estimate. Distinguish existing code from missing functionality. Do not redo completed 248 foundations.

Execute one continuous delivery; give short checkpoints every 30 focused minutes, reuse successful focused checks and record cumulative elapsed time. No repeated broad sweeps, arbitrary green-count targets or fresh clocks per correction. Report a real architectural blocker immediately with a proposed bounded resolution; credentials are NOT a blocker.

3. Close 248's financial evidence gap

Order must not store money alone. Extend the existing captured selection/accepted Order evidence through owning contracts and additive migrations as necessary:

Provider, owner/account identity and configuration version, provider service, authoritative quote input identity, exact amount/currency/components, issued time and freshness/expiry policy.
Provider-issued references only when actually returned. Never mislabel a local correlation ID as a carrier quote/reservation.
Revalidate changed/expired inputs or price before commitment, require explicit reconfirmation where needed, zero reservation/order on refusal.
Quote price composition must avoid duplicate packaging/tax and preserve carrier collect separately.
Persist immutable accepted evidence in Order; historical reads must not call Shipping/carrier or reprice.
Existing frontend summary, per-group choices, typed failures, selection revision and final-click flow must remain coherent.
Carrier delivery estimates must not be presented as guaranteed authored hourly receiving appointments.

Use documented Tipax units and currency. Reject incomplete/negative/inconsistent responses. Unknown expiry must use a documented LOCAL freshness policy, not a fabricated provider guarantee.

4. Complete shipment lifecycle

Implement provider-neutral create-shipment, label/receipt where supported, track and cancel capabilities with Tipax as the first adapter. Verify supported operations and exact routes against official V4 documentation/OpenAPI; unsupported actions must be explicitly unavailable, not simulated.
Sources:
https://document.tipaxapi.org/#v4-docs
https://omtestapi.tipax.ir/swagger/v4/swagger.json

Required:

Lawful committed Order/fulfillment eligibility before shipment submission; never create during quote, selection, preview or page rendering.
Build shipment from accepted Order and authoritative sender/recipient/package facts, not live mutable price configuration.
Owner-authorized admin/seller operations in existing operational surfaces; each operation clearly states unavailable prerequisites. Do not add storefront shipment actions.
Persist durable shipment intent, provider receipt/tracking identity and operation state under the correct module owner. Keep financial Order evidence separate from mutable shipment status.
Durable operation key/concurrency control before external mutation. No reliance on an in-memory lock.
If Tipax lacks documented idempotency or a lookup-by-client-reference capability, an ambiguous create timeout becomes UNKNOWN: no blind retry or exactly-once claim. Reconcile explicitly when provider capabilities allow; otherwise require a clear operator decision.
Track/status mapping retains unknown provider statuses; cancellation must follow lawful provider state and preserve history.
Label output, if supported, is owner-authorized and must not expose secrets.
No simulated shipment success, fake tracking ID, carrier calls with fake credentials or real shipment during this task.

5. Owner settings and UI completion

Review 248 account-scoped token/cache and DataProtection secret references:

Tokens/cache keyed by account/owner/configuration version; rotation/disable invalidates old state.
Reads never reveal credentials; wrong owner/tenant cannot read or invoke another account.
DataProtection keys persist across canonical restarts using the existing secure mechanism; do not regenerate keys and orphan protected secrets. Document backup/deployment dependency.
Registered provider endpoints are validated server-side; no arbitrary client-supplied URL.
Existing Shipping authoring shows provider/account/service/reference mappings and origin requirements cleanly.
Existing operational order/shipping screen exposes supported lifecycle actions and status.
Missing credentials shows "اتصال شرکت حمل تنظیم نشده است" (localized equivalent), with no outbound request, loop, zero-price fallback or silent Manual fallback.
Manual/Free saved tariffs are retained across mode switches, inactive tariffs do not affect API quotes.
Use repository frontend skill, language registry and existing design components. No screenshots, no broad redesign, no duplicated control panels.

6. Verification without a provider account

Use controlled HTTP at the adapter boundary, labelled CONTROLLED, and real module/application/persistence paths for the rest.
Meaningful focused proof:

Account owner isolation, disabled/incomplete configuration zero network, rotation and persistent key behavior.
Exact auth/reference/pricing/create/track/cancel wire shapes for supported operations, malformed responses, timeout and cancellation.
Real read→selection writer→payment preview→acceptance: immutable provider evidence, stale refusal before financial writes; Manual/Free controls.
Durable concurrent same-operation shipment submissions produce at most one outbound create; ambiguous response stays UNKNOWN and is not retried blindly.
Tracking/cancellation transition rules, unknown statuses and foreign access denial.
Order history unchanged after carrier configuration or later quote changes.
Missing provider account leaves canonical UI usable with clear state; no repeated network calls.
Existing shipping choice retention, summary and navigation behavior remain covered by adjacent focused checks.

Do not claim the above proves actual Tipax acceptance. No PSP/payment-success claim. Controlled relational tests use an identified disposable database and never reset user data.

7. Canonical deployment is mandatory

The user authorizes necessary additive migrations and rebuild/restart on canonical 3000/5088 to deliver this feature.

Verify exact DB/edition/process identity and take a private backup before DDL.
Review 248 migration 20261102170000_CarrierConfiguration plus new migrations, then apply with the official runner; no hand DDL/history edits/reset/reseed.
Safe production-build/restart from canonical source; preserve unsaved files/tabs, do not reload or automate user's editor window.
Verify Host health, FE route, authenticated authorized settings/operations reads and source/runtime parity after restart.
Do not change real cart lines, merchant prices/weights/rates, submit the user's cart, create a real shipment or turn on provider connectivity.
Manual/Free stay available; CarrierApi capability is installed but unconfigured until real credentials and mappings are supplied.
Check-in defaults stay OFF where existing policy requires; do not turn all captured checkout functionality OFF in the running user's working environment.
No second runtime/port handoff.

8. Acceptance and handoff

PASS only if mandatory software is implemented, focused checks pass and canonical migration/runtime deployment is verified.
Use decision state: SOFTWARE_COMPLETE_DEPLOYED_PROVIDER_UNVERIFIED.
Only actual external Tipax connection/acceptance may remain deferred. Missing code, UI, persistence, migration or deployment is PARTIAL, not a credential blocker.

Explicitly enumerate documented provider operations that are unavailable from the provider, if any. Do not call a scaffold or "planned later" shipment lifecycle delivered.

Commit/push normally; no force/amend/reset/stash user work. Reconcile moved main safely. Update carrier plan from delivered facts and update:
AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md

Task copy:
docs/ai/tasks/TB-SHIPPING-CARRIER-CONFIGURATION-AND-CHECKOUT-248-R1/TASK.md
Evidence:
docs/architecture/evidence/TB-SHIPPING-CARRIER-CONFIGURATION-AND-CHECKOUT-248-R1/{TASK.md,RESULT.md,RECEIPTS.md}

Return BRIDGE-WAKE-V1 worker envelope with actual Base/Code/Evidence SHA, canonical usable URLs and exact stage, migrations applied/readback, runtime parity, focused proof, controlled-vs-live boundary, elapsed time, remaining external acceptance only (or precise unmet software).
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
