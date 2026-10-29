# ARCHITECT REVIEW — TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R3

Status: **ACCEPTED WITHIN ISSUED SCOPE** on 2026-10-29. This is not user Visual ACCEPT.

Independent source review confirms:

- the province handler is state-only and no longer calls `refreshProjection` directly;
- the destination coordinator is the sole province/city projection owner;
- coupon/cart-version changes hard-clear, while destination changes keep provisional choices until authoritative per-group fingerprint reconciliation;
- pending/failed destination state keeps cards visible but disables interaction, hides confirmed money and prevents selection intent/Continue;
- canonical evidence records no document reload or root remount and one request for province plus one for city.

The user-reported cart navigation uncertainty is a separate measured defect: the cart route still blocks on full Home merchandising and the mini-cart CTA lacks action-level visible pending feedback. It is assigned to 245-R4 and does not reopen this accepted shipping correction.
