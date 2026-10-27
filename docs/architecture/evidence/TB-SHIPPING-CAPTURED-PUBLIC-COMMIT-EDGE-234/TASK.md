PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-CAPTURED-PUBLIC-COMMIT-EDGE-234
Parent-Task: TB-SHIPPING-CAPTURED-ADDRESS-BINDING-AND-PROOF-233-R3
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: CONNECT_EXISTING_PUBLIC_COMMIT_AND_PAYMENT_PREVIEW_TO_ACCEPTED_CORE
HARD35 / source22 / proof+recovery13. One correction. Real hard stop; no unreported extension.
Read233-R3/ARCHITECT-REVIEW.md and applicable backend instructions. Core231/232/233 ACCEPTED WITHIN RECORDED SCOPE. Do NOT reopen core quote/calendar/atomicity/proof work. No Bridge/enqueue/wake/post-result script, no235envelope.

ONE EDGE, NOT ANOTHER ENGINE
Connect the EXISTING StorefrontShippingService.CommitAsync, ProjectPaymentDraftAsync and actual
StorefrontStartPaymentComposer entry to the accepted CheckoutDirectory/CheckoutProcessManager captured flow.
Keep existing coherent CheckoutEnabled+SelectionEnabled+Enabled/date policy, all checked-in OFF. No runtime activation.
SingleStore Store / Manual / no coupon only; nonblank coupon remains explicitly unavailable. No Marketplace/Free/CarrierApi/weight guessing. No frontend or Payment/Settlement/provider policy work.

A CAPTURED PUBLIC COMMIT — NO LEGACY REPLAY
Current CommitAsync unconditionally calls EnsureCapturedPreviewIsNotCheckoutReady, then rejects stored capture,
then replays legacy SaveSelectionAsync. Add a captured branch using fresh server-owned stored root/groups, actual
request ExpectedCartVersion + exact ExpectedSelectionRevision TEXT + idempotency key; route directly into accepted
ICheckoutDirectory.SubmitAsync. NEVER route captured groups through legacy SaveSelectionAsync, static windows/
owned legacy pricing, or a second217/218preview. Existing LEGACY path stays unchanged.
No browser owner/money/evidence authority; group plans/address are derived from fresh stored selection. Real actor/
cart access at existing boundary, final authoritative acceptance stays inside Directory before reservation.
GateOFF/incoherent or missing service refuses not_ready; stored capture never falls into legacy if flagsOFF.
SelectionEnabled alone NEVER authorizes final commit.

IMPORTANT REPLAY: existing StorefrontCheckoutService.SubmitAsync calls PrepareShippingAsync and current-cart
version checks BEFORE Directory.SubmitAsync can return an idempotent existing Order. Do not break accepted replay
by blindly using that preamble for captured commits: a converted cart or moved/deleted address/pointer must not
prevent returning an already-authorized committed Order with the SAME submission key. A narrow captured member on
EXISTING checkout facade is allowed, building a server command from stored selection + actor and delegating to
Directory's idempotency/access-first authority (no second coordinator, invented live-cart snapshot, or public bypass).
If the submission is NEW, Directory revalidates current cart/address/Offer/type/pointer/quote/date through233.
Map result from the persisted Order snapshot; do not perform post-commit catalog/Shipping/address reads that could
turn a durable success into an external failure. Keep original per-group ids/accepted money/date/ticks.

B PAYMENT DRAFT READ — VALIDATE ONCE, WRITE NOTHING
When coherent final gateON, ProjectPaymentDraftAsync for stored captured selection should call a real read-only
quote/validation path using SAME core and SAME current selected evidence. No legacy repricing or static windows.
Reuse actual preview SellerOrder quote result/exact line map and233 acceptance; if a private quote-result helper
must be extracted, MOVE existing logic once. One whole-cart Pricing/Promotion/Tax pass, zero recursive217preview.
Use Order-owned read transaction/root check if needed; never call SubmitAsync to simulate a preview. No order/
reservation/payment/cart conversion/SaveChanges. Return actual validated group payable/collect/date/interval metadata
through existing projection shape (additive typed fields only if needed).
GatesOFF/stale/unsupported stays not_ready/conflict. Date availability != CanCheckout/activation.
Keep217 goods reader pure: it calls private QuotePreviewSnapshotAsync with shipping0; do not accidentally add
captured final validation to that goods-only helper and recurse or block ordinary captured READ after selection.

C ACTUAL PUBLIC STARTPAYMENT FORWARDING
Real path is StorefrontStartPaymentBody -> Request -> StorefrontStartPaymentComposer -> Shipping.CommitAsync ->
accepted Directory.SubmitAsync -> existing hold preparation / Payment facade. Prove exact revision token reaches
core through REAL existing endpoint/body/handler/composer delegation, not just constructing a Domain command.
Keep JSON new token a STRING; preserve4294967296 exactly; malformed/missing token for captured choice refuses with
stable code before reservation. Legacy requests remain compatible.
No new auth bypass. Endpoint uses existing buyer/cart authorization, no new permission catalogue.
Do not add Payment provider behavior: controlled hold-preparation/payment Contracts can stop or record after the
REAL Order commit; name the resulting outcome honestly (e.g. HoldPreparationFailed is not payment initiated).
No external PSP/network/payment write in tests or runtime.

FOCUSED PROOF (one new edge class + narrow existing edge controls ONCE)
Existing isolated231target15433/order_tb231/tb231, identity beforeDDL, normal migrations, one disposable existing
postgres17-alpine removed/portclosed. REAL ShippingService + checkout facade + Directory/ProcessManager +
OrderDbContext, controlled foreign Contracts/harness REUSE. No fake successful CheckoutDirectory/committed Order.
1 Public Commit positive two captured groups -> actual persisted Order evidence, own fee/date/ticks; no legacy
   save/price/window calls and no217 re-quote.
2 Same public key replay after pointer change, address removed and cart converted -> original authorized Order,
   no new quote/reserve/write; foreign actor denied. This is public facade replay, not only directDirectory replay.
3 Actual PaymentDraft captured preview -> current validated groups with zero SaveChanges/reserve/order/convert;
   stale pointer/address/revision and flagsOFF explicit refusals.
4 Actual StartPayment request with exact large revision TEXT reaches real accepted commit; controlled downstream
   hold/payment outcome recorded. Numeric JSON token, missing/whitespace/stale token, nonblankcoupon/gateOFF refuse
   before reserve; exact owning errors/statuses. No need to operate a real provider.
5 Existing true legacyOFF route/control behavior unchanged. No broad suite.
Build affectedOrder endpoints/tests only. Do not run25/68/124/36/28/31/50/119/188/194/285/310/312/fullOrder/
Shipping/Host/solution/guard replay. Runtime Host is not rebuilt/restarted.

PRESERVE / FINISH
Backend edge+focusedtests/docs only, NO frontend/HMR/browser/live cart/address/assignment/draft/pointer/payment write,
shared migration apply/schema/baseline change. USER VISIBLE reviewwindow is USER unsavedwork: never enumerate/read/
automate/reload/close; HostFE/localpreviewONcount7 unchanged. 231/232 migrations sharedNOTAPPLIED, EFsnapshotgap.
No coupon parity expansion/history endpoint/activation/fullcheckout/VisualACCEPT claim. Known230emptykey/inflight/
crossownerlabeldecoder/OfferAPI/227residual/225-R1lostLOCALwork retained. TB185paused/defaultOFF/PendingAllocation.
Task verbatim+RESULT, ALL3TOPs precise capabilities and remaining frontend/live trial. Start from ISSUE commit before
source. Exactpathscommit/push/fetch, no amend/force/reset/stash/rebase, CRLFdrift untouched. Real counts/time/corrections;
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
