# TASK — TB-SHIPPING-CAPTURED-SUBMIT-INTEGRATION-233-R2

Verbatim task (Bridge `tooba-main`, worker `tooba-worker-01`, parent `TB-SHIPPING-CAPTURED-FINAL-QUOTE-ACCEPTANCE-233-R1`):

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-CAPTURED-SUBMIT-INTEGRATION-233-R2
Parent-Task: TB-SHIPPING-CAPTURED-FINAL-QUOTE-ACCEPTANCE-233-R1
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: REPAIR_SOURCE_BLOCKERS_AND_EXECUTE_REAL_SUBMIT
HARD45 / first positive source cutoff28 / final17. One correction. Real stopwatch: stop at deadline, no75min extension. No Bridge/enqueue/wake/post-result scripts. Read233-R1/ARCHITECT-REVIEW.md FIRST. No234.

NOT A FIXTURE-ONLY TASK
Root inspected actual ce7356217: code cannot pass positive submit today. Fix the EXACT blockers below and run actual CheckoutDirectory.SubmitAsync. No new quote engine/coordinator, no token-only finish. 231/232 accepted behaviors stay unchanged. Public Storefront Commit/PaymentDraft remain CLOSED throughout, all checked-in gatesOFF; no runtime/UI/shared migration work.

FIRST: DI + IDENTITY (before a giant fixture)
1 CheckoutDirectory now takes capturedComposer whose goodsFactsReader resolves SAME CheckoutDirectory: constructor CYCLE. Extract ONE reader-free composition core containing existing218 ValidatePlan/ValidateFacts/candidate/216/calendar logic. Existing218 wrapper reads217 then delegates; facts-input path and directory use SAME core without depending on217 reader. MOVE code, do not duplicate. No service locator/IServiceProvider/Lazy/second directory or DbContext. A real OrderModule scope with controlled foreign dependencies must resolve ICheckoutDirectory, ICheckoutShippingGroupGoodsFactsReader, existing composer and captured acceptance; same reader/directory scoped identity preserved. This resolution test is mandatory even gatesOFF.
2 Final facts and capturedPlans must use SAME stable persisted ShippingGroupId-based GroupKey; current facts use GUID but plans use MethodCode. Two sellers on SAME binding must have distinct group keys. Keep method service/option separate, no label/index/first-match correlation.
3 ComposeFromFactsAsync must ValidateFacts against request.Command.CartId/ExpectedCartVersion, NOT facts.CartId/facts.CartVersion themselves. Wrong cart/version must fail before216.
4 Remove unused sellerByOffer.ToDictionary: two cart lines may lawfully share an OfferId. Keep exact cart-line map, no OfferId/quantity inference.

REAL POSITIVE FIXTURE — USE ACTUAL PROCESS, NOT PRIVATE MAP SEEDING
Extend existing ShippingGroupGoodsFactsHarness minimally: replace its deliberate no-call reservation/conversion stubs with opt-in controlled Contracts that return valid per-line reservation ids and record conversion. Keep REAL CheckoutDirectory/CheckoutProcessManager/Pricing->Promotion->Tax code and actual OrderDbContext provider/migrations. Existing no-write preview tests keep their old stubs by default. Existing ICheckoutCommitBarrier can inject failures; no production hooks, no reflection/manual _orderLineIdByCartLineId population.
Invoke public CheckoutDirectory.SubmitAsync so QuoteSellerOrdersAsync populates the map naturally, then actual acceptance/materializer/PersistCheckoutAsync runs.
Mandatory positive TWO sellers SAME Store binding/sharedSnapshot, distinct persisted groups/line sets, own area+row/chosen date+fractional hours/fees; final goods99/198 reach corresponding216 calls. Persist/read ACTUAL checkout, SellerOrders, order shipping groups and member lines with exact owner/long tokens/100ns ticks/payable/collect; no authored JSON. Assert one whole-cart quote, zero recursive217preview, one assignment batch and219 read per full binding. Don't require live foreign modules: clearly labelled controlled Contracts, real Order persistence.
Build the one fixture FIRST; if it cannot reach positive inside source28, STOP with exact failing call/stack, not another set of parser facts. Do not claim PASS or partial source acceptance merely from build.

TRUST/ATOMICITY REPAIRS IN SAME ACCEPTANCE (no new product features)

Require coherent Enabled+SelectionEnabled+CheckoutEnabled+valid date count for captured route; inactive/unset tenant/edition or missing optional DI authority => typed refusal, no null-reference/default.
Compare stored.CartVersion with authoritative current cart and command.ExpectedCartVersion. Compare EVERY stored evidence owner to actual active Store tenant EXACT Ordinal; don't merely derive tenant. No Seller/Marketplace inference.
Verify assignment dictionary key+OfferId+SellerPartyId against actual cart; reject null/missing/foreign/contradictory answers; lawful same-offer multiple lines supported. Verify batched types no duplicate/foreign answers and active exact id. Verify saved service/option active in real Shipping catalog (NULL option identity exact, no post:standard guess). Reuse existing canonical typed StorefrontCapturedPublicationBinding cache key; don't invent delimiter owner identity.
Re-read stored.SavedAddressId through existing AddressBook Contracts using the real authorized actor when present; verify its current country/province/city/postal/recipient snapshot against what would actually persist and against unique priced destination. Command display strings alone are NOT server address authority. Do NOT inject StorefrontCheckoutService into directory (cycle). Unsaved address also must equal the accepted stored address and resolve exact destination. Never invent country: use existing trusted country resolution and test the actual country-code parameter; current countryCode:null may not be assumed a successful lookup by a permissive fake. Unsupported service area refuses.
Check quote/pointer owner-bound snapshot/draft/revision/format/type + all money/date/ticks/timezone/minmax against accepted evidence; no silent rewrite. Preserve coupon-unavailable/Manual-only/unknown measurements.
Move acceptance into existing CheckoutProcessManager TransactionScope BEFORE reservation and hold/check the Order root expected SelectionRevision through persistence (narrow Order-row lock/CAS, no nested transaction). No foreign schema FK/DbContext, no distributed Shipping lock/serializability claim. Document point-in-time pointer validation precisely.
Materialize ONLY validated server groups on captured path, never raw caller evidence fallback. Existing true legacy path behavior remains, own-Order probe allowed; no foreign calls for genuine legacy.
Keep idempotency/access check FIRST: repeat an existing authorized order returns its ORIGINAL evidence after pointer change, no new quote/reservation/order.

FOCUSED PROOF GATES (same fixture, no broad replay)
A actual DI scoped resolution + wrong facts cart/version rejection.
B positive two-group submit/provider readback above; lawful duplicate OfferId-line control.
C parameterized early-refusal cases: owner mismatch, contradictory assignment, inactive binding, changed saved address/pointer/row/money/date, stale root/cart, coupon; no reserve/order/convert. Incoherent gates and missing authorities explicit refusal. Current missing-token/gateOFF omission controls retained.
D another context advances selection before Order acceptance => exact conflict. After accepted root lock a competing update cannot produce an order with mixed root revision; bounded coordination, no confidence loop.
E existing barrier failure after order write/cart conversion rolls back compact Order groups/evidence using established compensation, original exception/token preserved. Idempotent repeat after pointer change returns same original groups, one Order/reservation.
Use ONE task-owned disposable postgres17-alpine15433/order_tb231/tb231; identity beforeDDL, normal chain once, remove and verifyclosed. Shared database untouched. New exact integration/DI cases + directly affected facts/composer/materializer classes ONCE; affectedOrder builds only. No36/28/31/50/119/188/194/285/310/312/fullOrder/Host/solution/guard replay. Source-only trust paths are NOT executed proof.

FINISH / PRESERVE
No frontend/HMR/browser/HostFE runtime rebuild/restart, no live cart/address/assignment/draft/pointer/payment write, no shared migration apply/schema/baseline edit. 231/232 migrations sharedNOTAPPLIED, staleEFsnapshot disclosed.
USER VISIBLE review window is USER unsaved work: never enumerate/read/automate/reload/close; localpreviewONcount7 unchanged. Public Commit/PaymentDraft not_ready; no StartPayment/payment-provider activation, no full checkout/VisualACCEPT claim.
Task verbatim+RESULT in task folder; append233-R1RESULT correction on actual source blockers; ALL3recoveryTOPs precise. Exactpathscommit/push/fetch, no amend/force/reset/stash/rebase; leave CRLFdrift. Real counts/timing/remaining failures; STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. Known230emptykey/inflight/cross-ownerdecoderlabels/OfferAPI/227residual/225-R1lostLOCALwork retained. TB185paused/defaultOFF/PendingAllocation.
```
