# STAGE 3 DECISION GATES — business policy NOT to be invented

Per TASK: shipping policy ambiguities are resolved only from existing approved rules; otherwise log a precise gate
and keep the test `BLOCKED_ON_DECISION`. Do not silently alter Checkout/Cart/Shipping ownership.

- DG-01 Multi-offer group with no common method: is the group **ineligible** (fail-closed) or auto-split into
  per-seller subgroups? (Current code plans groups; policy for "no common method across offers in one group" not
  explicitly approved.) Proposed safe default: fail-closed ineligible. Needs architect/owner confirm.
- DG-02 Warehouse vs seller service source: when `Responsibility=Platform`, is the service selected from the
  store/warehouse Shipping publication or a platform-wide publication? (Both exist in scope today.)
- DG-03 Fee/threshold policy: free-shipping threshold, per-group vs per-order, and who pays. Only implement if an
  approved shipping policy document exists; otherwise BLOCKED_ON_DECISION.
- DG-04 Destination granularity: city vs province vs country-level eligibility; what the "destination set" of a
  method means for the checkout address.
- DG-05 Time window semantics: cutoff time, preparation days (per-seller), and same-day vs next-day eligibility
  boundary; how window is evaluated at checkout time.
- DG-06 Per-shop assignment cardinality: one service per offer, or an ordered preference list per (offer, shop,
  destination, window)? Determines the Offer DTO shape (single id vs list).
- DG-07 Rate group selection: when the SAME method has multiple rate rows, which row is chosen per destination/window.

These gates must be answered before Stage-3 product code; tests DG-dependent stay BLOCKED_ON_DECISION.
