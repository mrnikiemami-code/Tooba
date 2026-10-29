# ARCHITECT REVIEW — TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R1

**Verdict: PARTIAL — shipping invalidation and navigation mechanics accepted; visible timeout feedback missing.**

Accepted:
- global destination/coupon/cart-version invalidation and per-group confirmation fingerprints;
- non-routing recipient fields preserve date/hour;
- zero-slot stale ticks rejected;
- modified/middle clicks remain native;
- path/search/hash reconciliation, pop/back handling and five-second unlock;
- PDP/cart performance repair retained with focused proof.

Blocking:
- after timeout, `retryHint` is rendered only in `sr-only` content inside an `h-0 overflow-hidden` container. Sighted users see the progress bar disappear without the promised localized retry explanation.

Correction:
- `TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R2` is issued only for a visible, bounded, localized timeout message. The accepted shipping and performance scope must not be reopened.
