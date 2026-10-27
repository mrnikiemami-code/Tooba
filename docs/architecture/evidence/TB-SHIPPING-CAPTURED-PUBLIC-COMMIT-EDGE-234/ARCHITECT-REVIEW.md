# 234 Architect review — public service integration scoped accepted; response contract fixes required

Source reviewed3ed9f4279f53436180b92285282fff20d0c014da on2026-10-27. Root read actual public service/facade/payment draft/core changes and edge tests.5/5 and200/200 reported by worker, NOT root execution.

## Retain/accept within mechanical SingleStore/noncoupon scope
Captured commit delegates to actual Directory submit without legacy selection replay/second preview; public facade replay reaches authorized existing Order before current cart/address/pointer quote, real persisted-group response preferred. Controlled StartPayment composer stops honestly at HoldPreparationFailed after real Order write; no Payment initiation proof. Strict textual large-token transfer is model/composer proof, not endpoint delegate/listener proof.
Read-only payment draft uses same acceptance on actual one-pass SellerOrder quote, no Submit/reservation/write,217goods helper unchanged. Coherent gate configuration remainsOFF; no runtime activation. Full-owner/coupon release requirement remains binding.

## Source-confirmed response blockers (fix IN235 alongside owner support)
1 PaymentDraft maps through StubCartPage(snapshot), which always has Version0 and empty Lines. Thus active cart7 is emitted as CartVersion0 and real product/seller labels become generic. Existing ProjectPaymentDraftAsync already reads the real presentation cart but discards it. Preserve actual authorized cart/version/title metadata for this read without another goods quote; committed/historical responses must still avoid post-commit foreign reads.
2 AcceptCapturedShippingCoreAsync(read-only) reloads current root, but ComposePreviewSnapshot still uses caller command's old ShippingAmount/date metadata. A selection replacement between facade load and core read can validate new groups yet emit an OLD aggregate shipping/total. Return a typed acceptance receipt with current revision/cart metadata and derive preview money from accepted groups, not stale command. Expose exact current selection revision string for next commit; no fabricated0.
3 MapCommittedGroup supplies group.OrderLineIds into StorefrontShippingGroupView.CartLineIds. These are different identities. Add a clearly named optional OrderLineIds field for persisted groups; don't relabel them as cart lines. Preserve genuine cart IDs in draft/read-only groups.
4 MapCapturedPage fallback to storedPlans when historical Snapshot.ShippingGroups absent can invent current groups on a prior legacy Order. Historical response must use persisted facts only; missing groups remain absent, not current draft fallback.

No source claim that final core is broken; these are public mapping/read-consistency blockers for actual frontend usage. Do not present 234 as complete customer/HTTP edge acceptance until fixed. Legacy successful end-to-end not tested; one legacy refusal is a branch control only.
Worker rebased unpublished work onto75ebd5708 despite task no-rebase rule; no shared force/history rewrite reported.28min wall-clock but split unmeasured/5corrections, not processPASS.200test rerun not a new whole-product certificate.

NEXT235: implement required Store/Marketplace/Seller owner matrix on existing read->selection->preview->commit, close known cross-owner label map and these adjacent response issues. Coupon and frontend/practical complete trials remain REQUIRED subsequent work, not out-of-scope final handoff. No new payment policy, no generic audit.
