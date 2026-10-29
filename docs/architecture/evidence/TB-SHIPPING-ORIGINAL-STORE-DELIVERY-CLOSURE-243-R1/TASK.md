PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ORIGINAL-STORE-DELIVERY-CLOSURE-243-R1
Parent-Task: TB-SHIPPING-ORIGINAL-STORE-COMPLETE-HANDOFF-243
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — close the actual 243 regressions
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: CLOSE_WEIGHT_AUTHORITY_BINDING_WORKAROUND_AND_REPORTED_LIVE_FAILURES
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

0 — SAME DELIVERY, NOT A NEW PROJECT
User's binding request remains «ببین من می خوام کار کلا تحویل بشه».
243 reported PASS but FAILED its own delivery gate: concurrent StartPayment500, duration120 read with no slots,
and deactivated post:standard to force post/NULL. Root also found source defects in the new weight reader.
Architect decision243 = PARTIAL, NOT full handoff. This correction completes that SAME original-store delivery.
Do not repeat commissioning, add another application port, switch DB, build a new quote/calendar/payment engine
or open244. No screenshots/images/visual captures. ONE primary3000/5088, SingleStore/store-alpha/tooba_alpha.
Reviewed source7ea79556fa91fa8fe00b5fa45b349023d8ae7c88 / main5e73e899cdb10f2812e57ec014e346a7b026c27c.
Operational focus50min with explicit10min owning-cause checkpoint. Original deadline MISSED; this is not a new
completion-time promise. Report material scope blockers promptly rather than silently spending hours.

Preserve243 positive: real original draft01a16eeb…, explicit60min, real laptop offer964ac1b4…, known stored1.4kg,
actual Order01a1858c-1dfe…14–15, normal BFF review cart01a1858d-06ee… unsubmitted.
Do not repeat entire Marketplace/coupon/history campaigns. Do not touch user profiles9350/9351 or any handed-off
review window after handoff; create a separately labelled normal task session only if needed, SAME primary URL.
Verify PID/command line before swaps; no kill-by-name/globalDocker restart/reset/reseed/direct business SQL.
Use the project frontend skill for any UI changes, no redesign or further unrelated locale work.

1 — REPAIR THE NEW WEIGHT READER IN ITS ACTUAL OWNER
Source: Catalog.Infrastructure/Directories/CatalogDirectory.cs and existing Order Checkout partial.
GetShippingWeightGramsByVariantIdsAsync currently:
- selects ALL Number definitions with display Unit g/kg, ignoring semantic identity and IsActive;
- resolves only variant rows, then on ANY null result (including invalid/conflicting override) falls back to
  product-level weight, contrary to the contract's "unknown on invalid/conflicting" promise;
- drops all product definitions when ANY variant weight exists instead of overriding PER SAME definition;
- decimal amount*1000 and Order group total += can overflow; the latter is unchecked and can wrap into a wrong tier.
CatalogAttributeDefinition explicitly documents Unit as OPTIONAL DISPLAY UNIT and has stable machine Code and
IsActive. Display unit alone does not establish "this is total shipping weight".

Keep Catalog owning this semantic lookup, Order Contracts only. Inspect actual stored laptop definition Code/id,
existing canonical weight semantics/seeds. Recognize only an established active shipping/product weight definition
by stable semantic identity (never translated label or guessed "first kg attribute").
If no established weight identity exists, report the precise missing authority before inventing a new metadata
system; do not hardcode this product's GUID/1400 or accept every kg attribute.
Document the actual definition semantics that justify this laptop's measurement. Unit is conversion metadata only.
Do not broaden a display-unit convention into an unreviewed company-wide physical-measurement policy.

Build effective values per definition: variant value overrides the SAME product definition when present; absent
override may inherit product value. A PRESENT invalid/zero/negative/conflicting override remains UNKNOWN, NEVER
healed from a product fallback. Do not lose other effective definitions when one variant value exists; contradictory
effective weight values refuse/unknown. Inactive or unrelated g/kg definitions must not become shipping authority.
Parse stored canonical numeric syntax using Catalog's actual canonical-number rules, not NumberStyles.Number
silently interpreting malformed comma strings as valid weights. No rounding/truncation/default1/0.
Range-check before conversion, checked arithmetic for total, return honest unknown/stable refusal on invalid/
unrepresentable/overflow amounts. Cancellation/provider failure must propagate, not be disguised as missing weight.
Preserve one batched Catalog Contracts read on each existing read/final path, no repeated per-line service calls
or second pricing/cart read. Same measurement semantics at preview/selection/payment/final.

Focused tests on REAL changed reader/coordinator, not tests of duplicated helper logic:
product1.4kg=>1400g; valid same-definition variant override; absent override inheritance; PRESENT invalid override
does not inherit; conflicting effective definitions; unrelated/inactive kg attribute excluded; decimal overflow,
fractional gram and group-sum overflow; missing line weight=>unknown; quantity aggregation exact.
Use isolated fixtures for corrupt/extreme inputs, NEVER alter original business data or weights to make proof green.
Run affected Catalog/Order targeted builds/tests. New port must compile all actual implementers/tests, not only one
updated ShippingGroupGoodsFactsHarness; discover implementers with rg, no solution-wide test campaign.

2 — REMOVE THE POST:STANDARD DEACTIVATION WORKAROUND
243 explicitly deactivated existing post:standard so ResolveCatalogBindings falls back to post/NULL.
This disables an existing service and strands DEV230 selection. It is NOT accepted as correct owner/binding flow.
Privately retain current config receipts, then restore ONLY the post:standard active state changed by243 through
normal authorized Fulfillment API. No SQL/pointer rewrite or deactivation of other methods to force a match.
Find the ACTUAL binding resolution owner (rg ResolveCatalogBindings), trace catalog service/options/one-level
method identity plus selected publication identity.
Repair existing resolution so explicit legitimate post/NULL and post:standard commitments are resolved by their
REAL full owner+catalog binding, with no option shadowing or label/first-row guess.
Do not synthesize post/NULL for every service merely because it has options; verify actual catalog identity for
each offered method. If a method is legitimately not active/registered, use the normal owning registration flow
or return precise unavailable; do not change global catalogue to hide a resolver bug.
Keep one219 read per distinct selected full owner/binding and no "latest" fallback.
Prove original post/NULL hourly service remains priced on its laptop while post:standard remains active and its
existing legitimate assignment/capture stays available. No arbitrary reassignment of unrelated products.

3 — DIAGNOSE AND CLOSE THE TWO ACTUAL LIVE RECEIPTS, NO GUESSING
A. Concurrent StartPayment500:
Use the saved243 Host exception/trace and request bodies, identify EXACT first escaping provider/owning code/
constraint. Do NOT repeat the withdrawn238 correlation-column theory.
Repair only owning current checkout/payment/inventory/exception translation seam once cause is established.
No generic catch(DbUpdateException), treating errors as success, random idempotency key, unbounded retry,
in-memory lock or provider-exactly-once claim. Respect actor/access, checkout/provider identity, original exception
and cancellation token, existing transaction/attempt-only rollback.
A bounded real same-key pair must produce no500/no duplicate order/reservation/payment/attempt:
return original winner where established contract permits; an actual in-flight loser may be exact typed4xx
conflict if its winner is not yet committed. After completion, same-key replay must return original ids.
Any other provider failure remains accurately surfaced; do not map all persistence errors to duplicate/conflict.
UI double-click should still emit <=1request; do not call a two-HTTP-call test a UI counter proof.

B. Duration120 response:
Recreate via normal admin save+publication and a FRESH projection with verified current destination/cart/coupon.
Capture actual root/group/candidate reason plus selected snapshot/revision/format. If the earlier0slots was
unverified destination/stale client context, prove that exact reason and a valid current read; no calendar rewrite
based only on count0. If a genuine defect, fix its existing owner.
For same Monday09–17:60=>8slots,120=>4slots09–11/11–13/13–15/15–17, restore60=>8slots. New response cannot install
old slots or silently retain now-invalid14–15 choice. Original14–15 Order GET must remain unchanged.
Use ONLY slot-duration field on original draft for this already-authorized check; leave final60 published.
Preserve other hours/conditions/rates/weight/type/translation and existing orders. No applied migration edit.

4 — FINAL PROOF / ONE HANDOFF / RECOVERY
Stay primary3000/5088 SingleStore/store-alpha/tooba_alpha. Existing known review profile/cart is user work;
use separate task session for proof, no consuming/submitting user's unsubmitted cart. Retain private credentials.
Update final source production BOTH FE+Host only when needed; immutable builds before bounded swap, no watcherHMR.
No new app ports, no Marketplace DB promotion, no broad schema/seed/product/stock operations.
Re-run only actual changed facts + typecheck if frontend touched. Report commands/counts/remaining baseline reds.

Completion receipt must have:
- actual Catalog semantic definition (code/id/unit/active) ->effective1400g ->read/final matching quote;
- post:standard ACTIVE together with lawful original post/NULL, no configuration hiding;
- actual120-minute4slots and restored60-minute8slots from correct current context;
- real chosen non-first hour through browser/HTTP selection/payment/Order (earlier valid Order may be reused as
  history baseline; do not generate orders repeatedly when no source change affects that accepted path);
- concurrency no500, exact replay ids/counts, user-review cart still unsubmitted and normal auth/me200 primary.
No PSP/paid/external-provider exactly-once/Visual ACCEPT. No screenshot work.
Append243 RESULT correction (PASS was not architect accepted); write this RESULT/RECEIPTS and update ALL3TOPs:
AGENTS.md, START-HERE-IF-CHATGPT-IS-LOST, SHIPPING-ARCHITECT-RECOVERY (START-HERE was left ISSUED by243).
Exact authoritative state, full SHA/source/runtime parity; preserve full histories, no stale "PASS entire delivery".
Original deadlineMISSED. TB185 paused/defaultOFF/PendingAllocation, checked-in defaultsOFF/local gates as before.
Exact-path normal commit/push. No amend/force/reset/stash/rebase. STOP_FOR_ARCHITECT; no automatic244.

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ORIGINAL-STORE-DELIVERY-CLOSURE-243-R1
Parent-Task: TB-SHIPPING-ORIGINAL-STORE-COMPLETE-HANDOFF-243
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — close actual 243 regressions
Mode: CLOSE_WEIGHT_AUTHORITY_BINDING_WORKAROUND_AND_REPORTED_LIVE_FAILURES
Status: PASS|PARTIAL|BLOCKED|RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: <full issueSHA>
Base-SHA: <full actualSHA>
Code-SHA: <full SHA or NONE>
Evidence-SHA: <full SHA>
automaticNextImplementationTask: NONE
<ONE primary URL+actual usable stage; semantic weight/precedence/overflow proof; both real bindings ACTIVE;
actual500 cause+fix/exact concurrent outcomes;120->60 slot context+unchanged Order; final user session untouched;
focused commands/results, source/build/DB parity, time/gaps. No fabricated acceptance or images>
END_TOOBA_WORKER_RESULT
Append the task-end marker separately on the last line in the transmitted result.

END_TOOBA_TASK
