PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-CAPTURED-ORDER-COMMIT-PARITY-233
Parent-Task: TB-SHIPPING-CAPTURED-SELECTION-TRANSACTION-CLEANUP-232-R2
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: REAL_ORDER_SUBMIT_REVALIDATION_AND_COMPACT_EVIDENCE
HARD45 / source28 / final17. One correction round. Stop at the box and report missing work; do not silently extend.
232/R1/R2 captured-selection storage is ACCEPTED WITHIN RECORDED SCOPE; do not reopen its money/group/transaction repairs. Read232-R2/ARCHITECT-REVIEW.md + compact order-evidence decision + current recovery + applicable backend instructions. No Bridge/enqueue/wake/post-result script. No automatic234.

OUTCOME — ONE REAL FINAL ORDER PATH, DEFAULT OFF
Implement captured selection -> final Order group evidence through the EXISTING CheckoutProcessManager/CheckoutDirectory atomic boundary. This is NOT an unused validation helper, not a new checkout coordinator, and not a frontend or payment-provider project.
Add ONE explicit CheckoutEnabled=false policy in existing StorefrontCapturedPreviewOptions, coherent with existing preview/selection settings. No runtime enabling. Captured Commit/PaymentDraft remain not_ready when it is OFF even if other gates later change. Only active SingleStore Store owner, Manual, no coupon in this slice; Marketplace/Seller/Free/CarrierApi keep explicit refusals. Nonblank coupon stays unsupported, not silently dropped.

KNOWN REAL SOURCE PATH — KEEP IT
StorefrontStartPaymentComposer -> StorefrontShippingService.CommitAsync -> StorefrontCheckoutService.SubmitAsync -> CheckoutDirectory.SubmitAsync -> CheckoutProcessManager.SubmitAsync.
CheckoutProcessManager: existing checkout/idempotency/access check FIRST; LoadActiveCart; QuoteSellerOrdersAsync(requireReservation:false); existing TransactionScope; inventory reservation; PersistCheckoutAsync; cart conversion; complete.
Do not bypass the established inventory/cart/abuse/process/idempotency participants. Do not place a second cross-module transaction around them. A new captured acceptance operation/hook on ICheckoutSubmitHost is allowed only INSIDE this real submit path, BEFORE first reservation/order write.

1 TRUSTED ACCEPTANCE FROM CURRENT SERVER DATA
At captured final submit require the exact expected selection root revision as TEXT in the actual public StartPayment/Commit request path (additive, no browser evidence/owner/money accepted); omitted/null/blank/malformed captured token refuses, explicit legacy request behavior unchanged. Model-only unused field is not enough: prove endpoint-to-handler-to-commit forwarding via existing delegate/harness.
Read the selected draft fresh with complete evidence/membership and real cart access/version, never stale tracked Include truth. All groups must carry evidence, same current cart partition, no captured/legacy mix. No replay of captured codes through legacy SaveSelectionAsync.
The final acceptance must be reached even if someone calls CheckoutDirectory.SubmitAsync directly with captured ShippingGroups; a caller cannot bypass it by constructing OrderShippingGroupPlan.AcceptedEvidence. Treat those values as comparison input, never standalone validation authority.

Re-resolve:
- actual prepared saved-address owner + current contents (or the accepted typed unsaved address), and its UNIQUE Geography identity; compare to persisted priced destination scalars. Changed/unresolvable/foreign address => stable conflict/unavailable, no old cheap destination adoption. Service-area identity remains unsupported unless existing trusted address can express it; no guessing.
- current cart/version and exact whole-line membership, explicit 228 Offer responsibility/type/seller, active real type/binding, full owner and selected219pointer snapshot/draft/source revision/selection version.
- selected group's SAME216 quote and SAME213 customer calendar, comparing ALL accepted evidence: owner, refs, group/row/type, exact freight/packaging/payable/collect/currency, selected date, fractional open/close ticks/timezone and min/max metadata. A changed pointer or unavailable date/row or quote => explicit conflict requiring new selection; never overwrite the accepted draft with current values or auto-retry.
- require current root SelectionRevision at the Order acceptance boundary. Use existing Order-owned transaction/CAS/row lock only as needed so a concurrent selection cannot be silently adopted midway through order persistence. No Shipping DB lock/join/FK; selected-pointer freshness is a point-in-time Contracts observation, not a claim of distributed serializability. Record exact validation point and race boundary.

2 THE FINANCIAL TRUTH IS THE FINAL ORDER QUOTE, NOT AN EARLIER SECOND CART
Existing217 computes preview Pricing/Promotion/Tax; actual CheckoutProcessManager then quotes SellerOrders again. Do NOT validate Shipping from one goods amount and persist Order lines from another if prices/promotions/tax changed.
Use the ACTUAL quoted SellerOrder lines and exact cart-line/order-line mapping from THIS submit to build the same group's final tax-inclusive goods facts. Reuse/extract the existing217 fact assembly and218 candidate/calendar composition with an internal typed facts input as necessary, rather than copying money/calendar rules or recursively calling Preview/Submit. The real final Shipping evaluation must use these final quoted line amounts.
Reuse the planner's trusted partition/binding/219 logic (one batch Offer read, type/catalog reads, one219 per distinct full owner+binding) and216 engine; refactor its existing resolution seam narrowly if needed. No separate calculator, no fake quantity/weight, no per-group Pricing/Promotion/Tax loop. Count foreign calls in the positive test; final goods quote ONCE for whole cart. No shipping charge in the goods sum. Preserve existing cart-price-change/checkout confirmation rules.
All foreign failure/cancellation propagates; typed business refusal is not empty success. Pre-reservation refusal means zero reserve/order/cart-convert/payment actions.

3 PERSIST EXACT COMPACT EVIDENCE IN EXISTING ORDER GROUPS
Pass the accepted immutable server evidence through existing OrderShippingGroupMaterializer/OrderShippingGroupPlan and PersistCheckoutAsync into EXISTING order.order_shipping_groups in SAME successful checkout atomic boundary. Preserve each group stable ID, seller, exact line membership, chosen date, row and money. No full authored JSON/week/region/translation copies, new snapshot table or cross-module FK.
Checkout shipping amount means payable-at-checkout; carrier-collect stored separately; no double charge. Preserve existing Payment/Settlement semantics; do not make CollectOnDelivery packaging into carrier freight, do not invent a funder.
Failures after reserve/order-write/cart-convert use existing compensation/transaction behavior; never leave a captured order half persisted. A normal idempotent retry returns the ORIGINAL authorized stored Order (even after draft/pointer edited), without re-pricing/history rewrite or duplicate reservation/order. Do not create current-state dependency for reading a previously accepted order.
ProjectPaymentDraftAsync may expose captured readiness ONLY under this explicit gate and same current validation. OFF/stale/unsupported remains not_ready/refused, never legacy-repriced evidence. No payment-provider/UI change.

4 FOCUSED ACTUAL EXECUTION
Use real CheckoutDirectory + CheckoutProcessManager + real OrderDbContext on existing disposable231 target15433/order_tb231/tb231 (identity beforeDDL, existing image, normal migrations, one container removed/portclosed). Controlled Contracts fixtures for foreign modules are honest fixtures, not live-provider claims.
- Positive TWO seller groups SAME Store binding/shared immutable snapshot, distinct matched rows and dates/hours/money, final Pricing->Promotion->Tax goods99/198 used by corresponding Shipping calls. Persist/read BOTH actual order groups+lines with compact evidence and correct checkout-vs-collect totals; provider materialization not just a fake plan capture.
- changed cart/root revision/address/pointer/type/row/amount/receiving date or interval refuses BEFORE reserve/write/convert; service-area/unsupported owner and nonblank coupon refuse. Include a final quote amount changed from an earlier preview to prove no stale money accepted.
- existing actual StartPayment request dispatch forwards exact long text; disabled final gate stops captured writes, legacy OFF control unchanged, direct CheckoutDirectory caller with forged/missing captured acceptance cannot bypass.
- failure at order-write/cart-convert boundary rolls back own order/group evidence with existing compensation contract; exact exception/token identity preserved.
- idempotent repeat creates one Order and returns same accepted evidence after publication changes, no historical repricing.
Only new exact class(es) plus narrowly affected existing CheckoutProcessManager/OrderShippingGroupMaterializer tests ONCE; affected projects build only. No31/50/119/188/194/285/310/312/wholeOrder/Shipping/Host/solution/guard replay. If a test needs a minimal boundary seam, reuse existing harness/interceptors, not production test hooks.

BOUNDS / HANDOFF
NO frontend/HMR, no browser, no Host/FE restart/rebuild runtime, no live assignment/cart/address/draft/publication/payment write, no shared migration apply. USER VISIBLE review window is USER unsaved work: never enumerate/read/automate/reload/close it. Leave local previewON/count7 unchanged; all checked-in new gatesOFF.
231+232 migrations sharedNOTAPPLIED, EF snapshot stale gap remains; do not regenerate baseline or rewrite prior migrations. No new schema unless unavoidable and then report exact reason/scope before broadening (routine optional fields on existing internal command are allowed).
No full production/Marketplace/payment/VisualACCEPT claim. Historical Order-only reader endpoint is later work; accepted order evidence must already be readable through real persistence in tests. Keep known230emptykey/inflight/cross-owner decoder/labels/OfferAPI/227residual/225-R1lostLOCALwork recorded.
TASK verbatim + RESULT.md, ALL3recoveryTOPs, exact-path commit/push/fetch parity; no amend/force/reset/stash/rebase. Real counts, actual time, limitations, transaction/Contracts race boundary. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. TB185paused/defaultOFF/PendingAllocation.
