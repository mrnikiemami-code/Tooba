# 233-R1 Architect review — NOT ACCEPTED, source blockers precede missing proof

Reviewed actual main ce73562177dd92f41a15f55bbf0e4dda78446b30 on2026-10-27: CapturedAcceptance, shared facts builder, composer patch, OrderModule registrations, submit host, command model and Domain shipping sum. Root did NOT run Windows tests. Reported36/36 covers extraction/negative gates, not an actual accepted submit.

## Confirmed source blockers
1 DI CYCLE: ICheckoutDirectory -> CheckoutDirectory(capturedComposer) -> StorefrontCapturedGroupProjectionComposer(goodsFactsReader) -> ICheckoutShippingGroupGoodsFactsReader factory -> SAME ICheckoutDirectory. This affects service resolution irrespective of gates. Build-only proof does not instantiate DI. Break the constructor cycle with a single reader-free composition core holding existing validation/quote/calendar code; move, do not copy. No IServiceProvider/Lazy workaround or second directory/context.
2 GROUP IDENTITY: final facts.GroupKey uses ShippingGroupId.ToString("N"), but capturedPlans.GroupKey uses candidate.MethodCode. Therefore ValidateFacts rejects even one group; two sellers same binding also duplicate ValidatePlan's group keys. Same stable persisted group key must be used on both sides; method binding remains a separate identity.
3 FACTS VALIDATION: ComposeFromFactsAsync calls ValidateFacts(facts,facts.CartId,facts.CartVersion,plan), comparing facts against themselves, ignoring request.Command. Must compare to actual command.CartId/ExpectedCartVersion like ordinary ComposeAsync.
4 TRUST gaps: tenant is derived but never compared to evidence.Owner; assignment.OfferId/SellerPartyId are not checked; sellerByOffer=cart.Lines.ToDictionary(OfferId,...) is unused and throws for lawful same-offer multiple cart lines. Type response duplicates/foreign rows unchecked; active service/option catalog not verified.
5 ADDRESS: ResolveCurrentDestinationAsync trusts command province/city; no owned saved-address reread. SubmitCheckoutCommand currently has no SavedAddressId; stored draft does. Stored saved address must be re-read by real actor through AddressBook Contracts and its current contents compared, no circular CheckoutService dependency.
6 REVISION boundary: AcceptCapturedShippingAsync STILL runs before TransactionScope; no Order revision lock/CAS between acceptance and persistence, despite result wording "inside transaction sequence". Stored.CartVersion is not explicitly compared to current cart. Snapshot/format/owner response checks remain incomplete; options.CheckoutEnabled alone ignores preview/selection/date policy coherence.

## Retained direction, not acceptance
Own-Order capture-marker probe closes the gateOFF caller-omission shortcut in source. Common217 FactsLine builder, exact cart-line map and one218 body are appropriate; fix their integration and prove it. Current CheckoutGroup.Submit already derives total shipping from accepted group charges; don't duplicate total math.
No positive real-process/provider persistence, no tested commit/order rollback/idempotency, no actual DI resolution. Thus NOT even "wiring accepted" for final acceptance. Public Commit/PaymentDraft must stay CLOSED, gatesOFF.

233-R2 is a concrete repair+first-real-submit task, not another abstract seam or token-test task. Mandatory DI resolution and positive two-seller same-binding actual SubmitAsync with persisted evidence. Known trust/boundary cases must be fixed and tested before core acceptance can be granted; don't relabel them complete by source-only claims.

75min vs40 NONCOMPLIANT. No runtime/frontend/shared migration/user-window change. 231/232 accepted scopes retained; full checkout/VisualACCEPT/activation NOT claimed.
