PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-CAPTURED-COUPON-CONTEXT-PARITY-236
Parent-Task: TB-SHIPPING-FULL-OWNER-VALIDATION-AND-PREVIEW-COHERENCE-235-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — shared coupon context on the existing owner vertical
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: IMPLEMENT_COMMON_COUPON_READ_SELECTION_PREVIEW_FINAL_CONTEXT
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 0. Outcome / start / time
IMPLEMENT the required coupon path; do not return another survey, standalone helper or blanket coupon_unavailable.
Existing Store / Marketplace Platform / Seller owner paths must keep working. SAME coupon intent and accepted
post-discount+tax goods must reach read, selection, payment preview and final submit. No new discount engine.
Reviewed source9b765e81b; resolve Expected-Base-SHA to the full commit containing THIS TASK before edits. Fetch safely,
inspect tree, read AGENTS/TOP recovery,235-R1/ARCHITECT-REVIEW.md and the complete-owner-and-coupon delivery scope.
Persist this artifact verbatim in docs/ai/tasks/ and this evidence TASK.md. One active task only.
HARD45 / source28 / proof+recovery17, one correction. Record claim/start/source-stop/result wall clock. Stop at cutoff,
report exact completed capability and remaining blocker; no silent extension or forbidden broad test substitute.

## 1. Mandatory architecture / file structure
Read .cursor/skills/tooba-backend-feature/SKILL.md before coding. Owning capability is EXISTING Order checkout/
Storefront shipping. Existing paths:
- Order.Application/Storefront/{Models,Services,Shipping/CapturedPreview}: transport models, facade, planner/writer.
- Order.Application/Checkout/Contracts: typed goods/acceptance inputs and response only.
- Order.Infrastructure/Checkout/Persistence: same Directory partials, accepted facts and quote orchestration.
- Order.Domain/Checkout and Order.Infrastructure persistence mappings/migrations: minimum own-Order draft state.
- Order.Endpoints/Storefront: ONLY additive existing transport forwarding/validation if necessary; no new endpoint.
- Order.Tests/Checkout: reuse existing owner-vertical/real-submit/relational harnesses, not another test framework.
Reuse ICheckoutPromotionPort / CheckoutPromotionAdapter / PromotionDecisionEngine plus existing Pricing/Tax.
Promotion owns eligibility, code matching and discount allocation; Order owns confirmation/quote comparison/storage.
No foreign Domain/Application/Infrastructure references, foreign DbContext/schema joins, Host business logic,
service locator, extra quote coordinator, generic context framework, discount arithmetic or calendar implementation.
Existing HTTP ownership stays module-local with current ISender/request/handler, validators/Result/error mapping;
no direct persistence shortcut or authorization bypass. Keep stable typed localized errors/descriptors. If a new
business fault is truly needed, add it to the owning catalogue/resources, not hardcoded UI prose or message parsing.
No unrelated AMSC/guard/snapshot reset. Scoped DI, Contracts-only dependencies and file/namespace ownership remain.
All configured locales use existing localization/fallback authority, not a hardcoded language registry.

## 2. One coupon path through existing commands
Current concrete gaps:
- StorefrontShippingProjectionRequest/ProjectAsync and captured BuildAsync/ResolveAsync have no coupon input;
  planner creates SubmitCheckoutCommand(CouponCode:null).
- selection writer, captured payment facade/public service and final acceptance reject nonblank coupon.
- Directory.ShippingGroupGoods already preserves command.CouponCode while quoting goods with shipping=0;
  final QuoteSellerOrders already passes it to Promotion before Tax. Reuse those paths, don't requote.
Add trailing optional coupon intent on existing read transport and forward through actual endpoint/handler/service/
planner. Update actual callers; source compatibility alone is not reachability proof. Existing selection/preview/
StartPayment inputs already carry CouponCode: carry it LOSSLESSLY under Promotion's existing semantics everywhere.
Use ONE small Order coupon-intent representation if required (absence/boundary whitespace consistent across entry
points), never an independent case-fold/truncate/eligibility rule. Inspect current canonical Promotion code matching
and length authority first; do not invent lowercase/uppercase or a different code policy.
- Blank = absent consistently; nonblank unsupported/invalid must not silently become absent or successful zero
  discount. Respect existing Promotion applied/rejection output and current checkout error policies.
- Partial coupon applicability to some sellers/lines is lawful under existing Promotion rules; do not reject a
  mixed cart merely because another line has no applicable promotion. No new global prorating or stacking policy.
- Forward exactly the same coupon to one whole-cart goods quote per evaluation. Goods facts stay SUM of that
  group's final TaxInclusiveSnapshot, shipping/packaging excluded, through the existing facts builder.
- Final uses THIS submit's already-quoted Order lines; no recursive217/preview or second Pricing/Promotion/Tax pass.
Remove blanket coupon_unavailable only where this coherent path is implemented; never relax guards first.

## 3. Minimal confirmed context, exact comparison, existing atomicity
A coupon change must not reuse a saved shipping choice silently, even if it happens to match the same rate row.
A changed promotion/pricing/tax outcome with SAME code must also require reconfirmation when group goods differ,
even if freight happens not to change. Do not rely only on snapshot/row tokens or aggregate cart goods.
Use actual existing authoritative quote/facts outputs:
- Persist the confirmed coupon identity on the OWN Order shipping-draft root (absent explicitly null).
- Persist confirmed exact post-discount+tax goods amount per OWN draft shipping group, tied to its currency and
  whole-line membership; browser amount is an echo for comparison, never pricing authority.
- Reuse equivalent existing fields if present. Inspection at reviewed base found no Coupon field on CartShippingDraft;
  group facts already exist in217. If fields are absent, this task explicitly authorizes ONE additive Order
  migration for minimum nullable root coupon + per-draft-group goods confirmation scalars only. Optional aggregate
  discount scalar ONLY if required to reuse the existing QuotedDiscountAmount/PROMOTION_CHANGED guard.
  No new table/history subsystem, JSON/JSONB, per-line shipping copy, cross-module FK, new index without need,
  copied authored document or Promotion snapshot. Existing Order line discount/tax snapshots remain final history.
- Exact decimals, same currency/precision checks as Order; reject before mutation if not representable, never DB
  rounding/coercion as validation. Missing old confirmation on an existing captured draft => explicit reselection,
  no fabricated zero/discount/backfill. Genuine legacy evidence-null rows stay on their old path.
- Save coupon + complete group goods context INSIDE 232's SAME atomic replacement/CAS/rollback boundary and clean
  tracker ownership. Carry same-root preview revision as exact TEXT; preserve attempt-only cleanup/caller token.
- Read response exposes the actual context/goods as exact text needed for selection confirmation. Add minimal
  expected goods echo(s) to captured selection if needed to refuse a changed read->save context; no client owner/
  currency/price/discount is trusted. Strict shape validation on new untrusted fields, missing is not zero.
- Payment preview and final acceptance compare current intent + real per-group final facts with the persisted
  confirmed context. Coupon A->B, clear, expired/rejected or amount-changed => existing owning conflict/promotion
  error and new confirmation, before any reserve/order/convert. Do not silently rewrite persisted selection.
- Public idempotent replay FIRST returns original authorized Order after coupon/pointer/address changes with no
  extra quote/promotion/reservation; no new preamble that blocks replay on today's context. Foreign actor denied.
- Do not rewrite231/232 or other existing migrations. New migration WRITTEN, disposable-target APPLIED ONLY;
  shared NOT APPLIED. Stale EF snapshot baseline: no global regeneration/adoption/reset; map/prove just new scalars
  against real DB and disclose scaffolding limit. Reuse existing migration conventions/check naming/Down.

## 4. Two residual guards in SAME paths (no extra repair loop)
a) ComposePreviewSnapshot currently uses shippingOverride?.RequestedDeliveryDate ?? command.RequestedDeliveryDate.
   An intentional null means groups disagree, so old command date must NOT return. Branch on override OBJECT
   presence for nullable date fields; explicit null stays absent. Minimum date likewise. Method/window remain
   only honest shared values, no invented cross-group promise.
b) ResolveGroupOwner nullable type check currently allows BOTH NULL assignment/evidence type. Require a nonempty
   Guid and real active type on EACH line; missing type cannot skip activation check. Existing valid writer data
   unaffected. Stable owning refusal, zero reservation/order.
235-R1's test advances root BEFORE calling ProjectPaymentDraftAsync, not between outer and inner reads. Correct
the evidence and exercise the actual boundary via an EXISTING TEST port callback (e.g. one-shot active cart
read) after facade draft read and before core fresh read, or call existing typed facade with genuinely retained
old root while an independent actual writer advances it. No production hook/private-map seeding.
Assert aggregate date is NULL when accepted groups differ even if old command date is set; coherent new accepted
revision/money/groups/dates or explicit conflict. Dates must actually differ in the tested replacement.

## 5. Focused proof — usable backend vertical, one disposable target
Task-owned existing postgres17-alpine 127.0.0.1:15433/order_tb231/tb231 only; assert exact live identity BEFORE DDL
with existing helper, apply normal Order migration chain including any new additive migration, remove container/
verify port silent. No shared5432/reset/adoption. Foreign Contracts labelled controlled, Order DB/services real.
Reuse the actual mixed-owner fixture from235-R1 and accepted SingleStore positive:
1 Same valid coupon reaches real shipping ProjectAsync/read planner -> real SaveSelection -> public payment preview
  -> real public commit with ≥2 seller groups (Marketplace Platform and Seller owners represented). Record exact
  coupon at each Promotion request and one whole-cart quote per evaluation; own final goods (e.g.100-10+9=99 and
  200-20+18=198, not pretended tax0) reach corresponding216 calls; own row/fee/date/owner evidence persists.
  Include a coupon applying to one group and not another under current rules; never share allocations.
2 Coupon changed/cleared between read/save or save/preview/submit refuses stale context; SAME code promotion result
  changes goods while freight/row remains same also refuses. Assert no reserve/order/conversion on final refusal,
  no mutation/revision leak on rejected save, and later successful reselection uses actual new context.
3 Invalid/rejected coupon is surfaced under existing Promotion policy (not silently ignored); blank control uses
  no coupon; source/provider error and caller cancellation propagate exactly. No invented usage/redemption engine.
4 Positive replay after changed coupon/pointer returns original authorized order with no additional quote/reserve.
  Persisted original line discounts/tax/group evidence remain unchanged.
5 Null override/real stale-root preview and missing assigned type cases in section4; legacy OFF/no coupon control.
6 Existing shipping read transport body/delegate forwards coupon to actual service through current handler;
  no HTTP listener required here, but don't substitute manual request construction for delegate forwarding proof.
Run new exact coupon/parity class(es) plus the directly affected owner-vertical and public-edge class ONCE,
affected Order build only. No412/410/fullCheckout/Storefront/Order/Shipping/Host/solution/guards or historical suite
confidence loops. Fix genuine new failure within the one correction round, then stop honestly at hard limit.

## 6. Preservation / delivery
NO frontend source/HMR/browser/user-window access, Host/FE rebuild/restart/runtime gate change, shared migration,
live buyer/cart/address/assignment/draft/publication/payment/business write. This authorizes ONLY the new minimal
own-Order schema on the disposable target, not activation or a live checkout.
USER VISIBLE review window is USER UNSAVED WORK: never enumerate/read/automate/reload/close. LocalpreviewON/count7
unchanged; all checked-in Enabled/SelectionEnabled/CheckoutEnabled stay false.231/232 sharedNOTAPPLIED; disclose
new migration sharedNOTAPPLIED and EF snapshot gap. No Carrier/funder/slot/Payment/Settlement policy invented.
TB185paused/defaultOFF/PendingAllocation. Known230emptykey/inflight/decoder, Offer API,227mobile residual,
225-R1lost LOCAL edits remain recorded. Coupon/owner backend proof is NOT practical release completion.
After this review, existing frontend wiring + Order-only historical read + real desktop360/390/mobile practical
trials remain required; no new fixed task-count/time promise and no final SingleStore-only/noncoupon exclusion.

Persist TASK+RESULT, append accurate235-R1 evidence correction, ALL THREE recovery TOPs, exact changed paths/
schema/policy/service mapping, actual elapsed/corrections and unrun gaps. Read actual task issuance before source;
later docs may merge without history rewrite. Exact-path commit/push/fetch, no amend/force/reset/stash/rebase,
leave unrelated line-ending drift. STOP_FOR_ARCHITECT; no237 envelope.

## RESULT CONTRACT — canonical fixed envelope
Post ONE complete real Result only, no probe/test/dry-run content. Use the exact fields/order below, filling
ACTUAL_* with actual verified values or NONE; no unresolved placeholders in the transmitted Result.
Status exactly PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT. Worker PASS != Architect ACCEPT.
Expected-Base-SHA is the FULL resolved issuance SHA of THIS artifact; body strictly follows automaticNext...NONE.
Wire content (omit Markdown fences):
```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-CAPTURED-COUPON-CONTEXT-PARITY-236
Parent-Task: TB-SHIPPING-FULL-OWNER-VALIDATION-AND-PREVIEW-COHERENCE-235-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — shared coupon context on the existing owner vertical
Mode: IMPLEMENT_COMMON_COUPON_READ_SELECTION_PREVIEW_FINAL_CONTEXT
Bridge-Task-Row: ACTUAL_CLAIMED_ROW_ID
Status: ACTUAL_ALLOWED_STATUS
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: ACTUAL_FULL_ISSUE_SHA
Base-SHA: ACTUAL_FULL_START_SHA
Code-SHA: ACTUAL_FULL_CODE_SHA_OR_NONE
Evidence-SHA: ACTUAL_FULL_EVIDENCE_SHA_OR_NONE
automaticNextImplementationTask: NONE
ACTUAL_RESULT_BODY
END_TOOBA_WORKER_RESULT
```
Append the exact task-closing marker on the last line of this artifact immediately after END_TOOBA_WORKER_RESULT.
Nothing follows it. Body: actual rootcause/implementation/path+namespace/Contracts/DI/error mapping; Promotion
authority and coupon matching policy; minimal schema/precision/storage shape; per-group quote-context ledger and
positive/negative/replay call counts; exact executed tests and target identity/cleanup; scope/gates/runtime
preservation; actual elapsed/corrections; changed paths/commits/push/parity; NOT DELIVERED and blockers.
No HTTP/live/PSP/visual/physical DB claim from controlled fixtures. STOP_FOR_ARCHITECT; next implementation NONE.

END_TOOBA_TASK
