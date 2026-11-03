PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R3
Parent-Task: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — close quantity-safe execution, relational proof and authorized handoff
Mode: PROVISION_DISPOSABLE_PROOF_AND_COMPLETE_EXISTING_GROUP_EXECUTION
Expected-Base-SHA: 4ec12a05ee2421782ddd5fb91b4b0ef3c4b3027b
Status: ISSUED
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

1. Continue to usable delivery

User authorizes sequential fixes until delivery, one active task at a time. Read full parent tasks, current instructions and evidence. Do not reset the scope or count InMemory proof as PostgreSQL proof.
Canonical delivery remains user's SarvNewVer FE3000 → Host5088 / SingleStore/store-alpha/tooba_alpha, with Marketplace exact owner support. No Tipax account/test required, screenshots or real carrier calls. Do not fulfill/cancel/pay the user's order.

R2 is honestly PARTIAL. Close its disclosed software gaps, execute real integration and authorized read-only UI acceptance. Preserve current page design; no new generic giant form or duplicate group panel.

2. Source review finding — completion can bypass fulfillment check

OrderCommittedCarrierShipmentFactsReader.Map sets reason=package_completion_required before:
if (reason is null && hasQuote && ownerPresent) ResolveFulfillmentEligibility(...).
Thus a legacy incomplete package SKIPS fulfillment eligibility. Shipping readiness later removes the completable reason after saved package completion; it can expose execution without checking packed/remaining quantities.

Fix business eligibility independently from completeness: accumulate order/fulfillment constraints even when package/sender data is missing; completion may clear only its own data reason, never mask cancelled/exhausted/unallocated state.
Use the real fulfillment authority and actual line quantities. The current shippableTotal>0 across the group is insufficient to authorize sending the FULL accepted group package when only part of one line is available. Determine exact executable package line membership/quantities and protect it durably against competing manual/carrier allocation. Preserve immutable customer method/financial history.
Missing facts/failed fulfillment read must fail closed with actionable reason, not a500 or allowed fallback.

3. Finish multi-parcel / remaining-line scope

R2 explicitly reports no parcel/remaining-line representation. Inspect existing fulfillment shipment/items model FIRST. Reuse its package/quantity authority, do not create a second inventory/shipping allocation engine.
Implement only the missing bridge:

Each physical package/dispatch has stable identity, exact accepted group, owned line quantities and own package facts.
Carrier operation/idempotency tied to that package/dispatch, not one irreversible create key for the entire group.
Multiple packages from one group possible without repeating the whole group's weight/value/quantities.
Group split never changes selected method/account/owner or charges/reprices the customer.
Manual and carrier operations cannot double-allocate the same remaining quantities; concurrent requests enforced at durable owner boundary.
UNKNOWN/Pending reserves its package intent; no blind resend. Cancellation/reconciliation updates through lawful existing transitions.
Package completion remains operational data, not overwritten accepted quote evidence.
Keep compact execution UI within existing shipment/group area; only real missing package data and line quantities are editable. Use existing components/frontend skill. No raw IDs, provider numeric codes or repeated sender forms.
4. Provision the required disposable target — missing env is not a blocker

Provision ONE task-owned PostgreSQL through available Docker/container tooling on a verified free loopback port (prefer15433 if free), e.g. task container tb249r3-pg, fresh task DB/order_shipping_tb249r3 and dedicated user. Do not reuse/reset canonical databases.
Generate test password privately; never print/commit credentials. Assert exact host/port/database/user and task ownership BEFORE DDL, then normal required Order/Shipping/Fulfillment migration chains.
Set the test variables using private runtime configuration; absent TOOBA_* variable alone is not an environmental failure.
If container engine is unavailable, attempt one bounded lawful alternative task-owned local instance. Do not restart shared Docker engine or disturb user processes. A genuine failure must include actual command/error/capability, not just unset variable.
Cleanup only own container/process; retain sanitized receipts, verify target port closed. Disposable proof never substitutes for canonical deployment.

5. Required REAL working-path matrix

Use real Order facts reader, Shipping readiness/directory, fulfillment quantities/allocation and PostgreSQL storage. Stub only external provider boundary with a labelled controlled adapter/HTTP. Do not replace the positive path with fabricated facts/readiness.

Legacy incomplete physical package on a lawful API group: save completion→save sender→GET ready→Create once; accepted Order money/method/history unchanged.
SAME legacy setup with no packed quantities, delivered/cancelled/exhausted state: completion does NOT unlock; zero provider requests.
Partial packed quantities and TWO packages from one accepted group: each operation owns exact line quantities and package facts; no repeated full-group payload.
Parallel manual/carrier allocation and concurrent same-key/different-key creates: no quantity double-use, one outbound request per lawful operation, zero extra financial writes.
Durable pending EXECUTION INPUT exists in PostgreSQL BEFORE provider callback receives create; simulated ambiguous timeout→UNKNOWN; replay/reconcile without duplicate create.
Separate origin/destination/postal; configured sender protected from Create override; missing routing/account identity remains blocked.
Inactive account and operation-state gates agree across capability/list/get/direct mutations; UNKNOWN may reconcile, not blindly cancel/recreate.
Actual slim frontend create/completion body traverses current endpoint delegate/listener + validator + handler, no placeholder identifiers.
Component interaction (not decoder-only): completion→readiness→action; throw/error/retry and concurrent group locks, no status-driven extra reads or stale response install.
Existing Manual/Free fulfillment/payment/returns stay intact.

Run focused relevant checks and affected builds once after fixes. Do not replay broad suites.

6. Authorized screen acceptance on canonical runtime

A401 on capabilities is only authentication proof. Obtain a sanctioned normal admin session through existing BFF/login using authorized local test mechanism; no role grants, fake headers/JWT/cookie injection, or user password search. Do not automate a user editing tab.
Inspect READ-ONLY user's order locator 01a185a2-2e5a-7000-a3b4-eee8c3710158 if present:

Existing page layout retained, products/method/shipping groups understandable.
PendingPayment has correct blocker/no carrier create.
Manual order does not show carrier setup form.
Correct sender labels/postal, no raw ID prompts/technical paragraphs.
Use independent task session and DOM/request measurements, no screenshots.
Also render eligible/incomplete/UNKNOWN cases in an isolated controlled UI fixture against actual component so actions and messages are tested without mutating the user's order.
If legitimate admin login is unavailable, report exact access dependency and keep that check explicitly unmet; do not pretend public /fa200 or401 proves authorized UI.
7. Deploy and record

Necessary additive migrations authorized with exact identity + private backup + official runner. No direct business SQL, reset/reseed or guessed merchant data. Preserve DataProtection keys, local edits, tabs, rates, cart/orders.
Deploy corrected canonical3000/5088 from delivered tree only; no new permanent port/DB handoff.
Preserve checked-in defaults and working local Manual/Free/captured preview state.

Within first10 focused minutes confirm provisioning and exact implementation plan/estimate. Checkpoint25 minutes, cumulative ledger, explicit remaining paths. Do not stop for accounts or silently reset clocks.
Task verbatim:
docs/ai/tasks/TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R3/TASK.md
Evidence:
docs/architecture/evidence/TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R3/{TASK.md,RESULT.md,RECEIPTS.md}
Update three recovery TOPs + carrier plan, R2 remaining ledger and actual source/runtime parity.
Normal push, no force/reset/amend/discard of user work. Full BRIDGE-WAKE-V1 envelope with actual Base/Code/Evidence SHA, exact stage, migration ledger, executed cases, cleanup, elapsed time and any unmet requirement.
PASS requires completed mandatory scope and proof. External Tipax test only may remain deferred. Otherwise precise PARTIAL, no self-certified all-complete status.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
END_TOOBA_TASK
