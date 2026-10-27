# 233 Architect review — PARTIAL, outcome NOT ACCEPTED

Source reviewed1c42a43f75f82e57914eb91b987ceb26b9d65f1a on2026-10-27.
Root read actual CapturedAcceptance partial, ProcessManager/host/service patches and existing217/218 composition code. No root execution of worker tests.

Retain additive defaultOFF CheckoutEnabled, exact-text token transport direction, strict parser, real pre-reservation hook and threading its returned plans into materialization. These are scaffolding, NOT completed final acceptance or persisted captured checkout.

Source confirmed: AcceptCapturedShippingAsync discards sellerOrders/now and unconditionally throws for every captured passage with gateON. It never invokes strict parser, current address/Offer/type/pointer/216/213 verification, or final-line goods facts. No positive submit/provider proof exists;28 parser/legacy/materializer facts do not prove ON-path acceptance or HTTP token forwarding.

## Additional bypass to repair
The early (!checkoutEnabled && !callerSuppliedEvidence) return trusts caller OMISSION as proof of legacy, without reading the actual stored captured draft. Direct SubmitAsync callers can omit evidence and bypass stored captured selection detection. Existing Storefront public guards do not make CheckoutDirectory's direct boundary safe.
The original zero-extra-OWN-DB-read legacy preference is superseded for this check: a fresh own-Order capture marker read is authorized; zero foreign calls/genuine legacy behavior retained.

The hook currently runs outside TransactionScope, so it alone cannot retain root selection revision through Order persistence. Final acceptance must cover an Order-owned check/lock in the established boundary before reservation; no nested cross-module transaction/distributed pointer-lock claim.

233-R1 is narrowed to core actual submit acceptance and REAL persisted two-group evidence using this submit's already-quoted SellerOrder lines. Public Storefront Commit/PaymentDraft remain CLOSED in that slice; public activation/hookup is a later explicit review, not falsely claimed complete here. No new placeholder-only finish or broad test reruns.
Source-guided seams:217 existing exact line-map/fact assembly can be shared;218 already has one ValidatePlan/ValidateFacts/candidate/calendar body and can gain an internal typed facts entry without recursive preview or duplicate engines.

Process:70min exceeded45, outcome not delivered, not processPASS. 231/232 accepted scopes unchanged. No live/runtime/UI/shared migration/user-window changes; no VisualACCEPT. All known recorded limits remain.
