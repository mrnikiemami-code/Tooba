# Architect review — TB235-R1 — 2026-10-27

Reviewed main 9b765e81b49d089b423266420f0588eb60862257, implementation cb3d054bb5e0d78ad5685d12578d653dc7b8a106,
actual patches and complete StorefrontFullOwnerVerticalTests. Root did not execute the worker's Windows tests.
Verdict: ACCEPT WITHIN THE RECORDED OWNER-VERTICAL SCOPE; two narrow residual guards/preview presentation fixes
carry into the required coupon integration. Not full release, activation, payment or Visual ACCEPT.

## Accepted mechanical progress
ResolveGroupOwner restores assignment payload OfferId vs lookup-key equality, explicit per-line defined
responsibility, initialized flag and group consistency. Typed owner policy remains shared. Authorized presentation
CartPage is passed from ShippingService into facade without the former second presentation load; Directory still
does its own cart/version validation. Preview uses one typed accepted-groups receipt for total/method/window.
The new positive truly calls Shipping.SaveSelectionAsync -> planner -> real232 writer/store, public payment
preview, public commit and persisted Order. THREE partitions have distinct owner-specific snapshots/drafts/labels/
rows/dates/100ns ticks: Marketplace Platform + SellerB + SellerC, same post/standard, own125/73/94. Controlled
pointer/quote callbacks select by owner/draft; swapped seller responses refuse. Unlike235's manually seeded
positive, this fixture obtains accepted owner evidence from the real writer. This closes that bounded integration
gap. Existing permissions remain a separate accepted route/authorizer boundary, not new live seller-admin proof.
No physical tenant DB isolation or actual Marketplace network/provider proof follows from a real Order DB.

## Residual source facts — do not repeat the worker's broader claims
1. ComposePreviewSnapshot uses shippingOverride?.RequestedDeliveryDate ?? command.RequestedDeliveryDate.
   Derive intentionally returns null when accepted groups disagree. Null coalescing therefore restores an OLD
   aggregate date instead of honoring the accepted explicit absence. MinimumDeliveryDate uses the same pattern.
   Select by override OBJECT PRESENCE, not by nullable field value. No aggregate time/date promise is invented.
2. ResolveGroupOwner checks lineTypeId == Guid.Empty || lineTypeId != evidence.ShippingTypeId, but both are
   nullable. NULL assignment type plus NULL optional evidence type passes; later active-type check is skipped.
   Require lineTypeId is a nonempty Guid and active. Current real writer normally supplies a type; this is a
   final-boundary completeness guard, not a claim that ordinary valid writes currently omit types.
3. Payment_preview_describes_the_newly_accepted_root_not_the_stale_outer_read calls MoveSelectionToSecondRevisionAsync
   BEFORE calling public ProjectPaymentDraftAsync. There is no outer facade/draft read before that advance.
   It proves an ordinary preview AFTER replacement, not replacement BETWEEN outer and inner reads. Dates also
   stay unchanged in the move; only money changes. Do not call this the deterministic inter-read proof.
4. Missing-responsibility/payload negative fixtures use Seller responsibility under SingleStore and a manually
   seeded Store owner, so baseline already has other reasons to refuse. Green proves the new owning early codes,
   not measured RED unsafe acceptance. Worker correctly disclosed RED was not run; do not invent it.

## Process/recovery
Source started1662e6f92 before corrected issue a5baee32b; later docs merge does not change actual Base-SHA. Accept
the disclosed reconciliation as history, not start-at-issue compliance. Broad Checkout|Storefront412/410/2 was
outside the exact-class instruction; do not replay it or present it as release confidence. Worker reports
6/6 new facts and affected builds; all execution remains worker evidence. Two known red guards retained.
Current TOPs incorrectly describe nullable type as always non-null and inter-read proof as executed; this review
and new TOP correct those claims without rewriting worker history.

## Next required implementation, no audit loop
TB236: SAME coupon intent/context through existing shipping read -> selection -> payment preview -> final quote.
Use Promotion's existing Checkout port and actual Order quote/facts builder, never own discount allocation.
Persist only minimum draft coupon/group goods confirmation scalars if needed (no full authored/quote JSON or
per-line shipping copies). Preserve mixed owners, exact evidence, idempotency and atomicity. Fix the two tiny
residual guards above in those same paths and prove explicit absence + real boundary transition. Frontend and
complete practical desktop/mobile/HTTP trial remain the next required delivery work; all gates stay checked-in OFF,
runtime/user window untouched, 231/232 shared NOT APPLIED and stale snapshot gap retained.
