# Architect review — TB235 — 2026-10-27

Reviewed actual source commit 1662e6f92a3b0e20f44f4b24b0b39d8856b85d60, its patches and complete acceptance,
payment-preview, facade and fixture files. Root did NOT execute Windows tests/runtime. Verdict:
PARTIAL / REPAIR REQUIRED. Worker PASS is not full owner-vertical or release acceptance.

## Retained within scope
One shared typed owner policy now derives Store(active exact tenant), Marketplace, or real SellerPartyId from
trusted edition/context/explicit Offer assignment. Planner/writer/final acceptance consume it. Label lookup is
now full typed owner+binding, which repairs the actual dictionary collision in source. OrderLineIds are separate
from CartLineIds; absent historical groups are not filled from current drafts. Preview shipping total derives
from accepted groups and the receipt returns exact selection revision text. Retain these changes.

## Blocking source regressions / remaining contract defect
1. CheckoutDirectory.CapturedAcceptance.ResolveGroupOwner removed the former assignment.OfferId == offerId check.
   A dictionary entry under offer A can now carry Offer B (same seller/type/responsibility) and pass final
   acceptance. Lookup key alone is not a verified response identity. Restore exact key+payload consistency before
   owner derivation. Refuse missing/unknown responsibility and missing/empty/inactive type for EVERY line.
2. The same helper uses responsibility == null as its "not initialized" sentinel and continues. On a stored
   multi-line group, first assignment responsibility null followed by valid Platform/Seller causes the first
   missing responsibility to be overwritten, not refused. Validate every line first and track initialization
   separately (or an equivalent explicit first-line value). Null must never become proof by the next line.
3. Payment snapshot override fixes ShippingAmount ONLY. ComposePreviewSnapshot still reads
   command.MinimumDeliveryDate/RequestedDeliveryDate/RequestedDeliveryTimeWindow/ShippingMethod* while accepted
   groups/revision are freshly loaded. No deterministic root replacement test was supplied. Either compare an
   explicit outer revision and refuse a changed root, or build the captured presentation from one accepted
   receipt; never pair old dates/method with new accepted groups/revision. Do not invent an aggregate interval.
   ShippingService.ProjectPaymentDraftAsync already authorizes/loads the presentation cart then discards it;
   facade adds another RequireCartAsync. Reuse the first authorized CartPage with an internal typed handoff,
   keeping Directory's real cart/version validation. This is not another goods quote, but it is an avoidable
   duplicate presentation cart load and fails the task's explicit reuse instruction.

## Evidence limits corrected
The new Marketplace public-edge positive calls SeedMarketplaceOwnerDraftAsync, which manually constructs accepted
owner evidence and inserts the draft. It DOES NOT execute read plan -> selection writer -> public preview -> commit.
Its FixedCapturedPointerLookup returns the SAME snapshot/draft/translations for any requested owner; it only
increments Calls, so 2 calls is NOT a proof of distinct owner-specific captures/labels or Seller A/B isolation.
Separate writer unit facts exercise Marketplace and Seller, but do not close that vertical gap. The policy test
of binding equality is not a rendering/translation provenance test. Existing merchant authorizer controls may be
retained as pre-existing scoped permission evidence, not a new live seller-admin proof.
The new contract mapping fake and real Order edge tests prove their stated mappings/totals, not an in-flight
root replacement; pointer change != Order selection-root replacement. No physical tenant DB isolation claim.
Worker reports a forbidden whole-Order run (564/566, baseline guards red). Do not repeat or use it as a release
certificate. Commit changes two frontend API files; no assertion is made here about actual HMR/runtime effects,
which root cannot observe. None of the three required recovery files was in this worker commit; this review
updates their TOPs, preserving history. RESULT timing text is inconsistent with later report; accept no timing
compliance claim. No new release/checkout/visual acceptance.

## Next
Issue TB235-R1 only: repair the two per-line validation defects, coherent preview handoff, and one actual
mixed-owner existing-path proof with strict owner-keyed Contracts. Preserve accepted code; no generic audit or
broad suite. Coupons + frontend connection + complete practical desktop/mobile trials remain mandatory next
delivery work, NOT final exclusions. All checked-in gates OFF, 231/232 shared NOT APPLIED, stale snapshot retained.
No live runtime/browser/business writes/shared DDL. User review window and unsaved work untouched.
