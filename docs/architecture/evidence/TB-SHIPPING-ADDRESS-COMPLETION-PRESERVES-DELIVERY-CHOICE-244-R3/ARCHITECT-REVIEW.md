# ARCHITECT REVIEW — TB-SHIPPING-ADDRESS-COMPLETION-PRESERVES-DELIVERY-CHOICE-244-R3

**Verdict: PARTIAL — user-reported non-routing reset is repaired, but the invalidation boundary is too broad.**

Accepted within scope:

- first/last name, contact mobile, postal address, postal code and notes do not issue a projection and preserve both
  groups' selected date/hour;
- incomplete-field Continue blockers do not mutate choices;
- the canonical actual cart reached the payment-ready form without checkout/Order/payment;
- an invalid slot is pruned per group in the focused facts.

Blocking source findings:

1. `reconcileCapturedChoicesAgainstPreview` validates only groupKey, method, date and slot ticks. It ignores the
   exact routing/pricing fingerprint that caused `capturedSelectionContextKey` to change. Therefore a coupon,
   cart-version, snapshot/source revision, area/rate row, goods amount or quote-price change can retain the old user
   confirmation whenever the same date/slot still exists. This contradicts task acceptance item 7
   (“coupon/cart/group/quote/calendar change still invalidates”) and can silently carry a choice into a changed price.
2. When the refreshed day has zero `slots`, the function retains a previous choice even if it carries non-null slot
   ticks. The receipt describes “no slots ⇒ slot ticks null/ok”, but the implementation does not enforce that.
3. The live proof covers non-routing edits and one destination refresh with the same offered slot. It does not prove
   coupon/cart/quote/rate/snapshot changes clear the prior confirmation.
4. RECEIPTS abbreviates the buyer id as `01a0996c-b8d9-7000-8f5e-…`, inconsistent with the previously corrected
   owner id `01a0996c-b8d9-7000-9854-…`; this should be corrected append-only.

Required later correction: preserve choices only when the exact server confirmation/price/calendar identity for that
group is semantically unchanged; non-routing form edits remain outside the identity. A genuine coupon/cart/quote/
snapshot/area/rate/goods change must require explicit reconfirmation. A no-slot day may retain only a lawful null-slot
choice.

No new task is issued by this review. Task 245 remains withdrawn; if a stray worker result arrives, review it
separately before any follow-up.
