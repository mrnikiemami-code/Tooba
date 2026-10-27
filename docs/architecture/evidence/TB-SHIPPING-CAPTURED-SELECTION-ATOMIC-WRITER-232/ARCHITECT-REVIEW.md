# 232 Architect review — NOT ACCEPTED; 232-R1 issued
Reviewed actual source d2307889ff0e38ea4f47c451af1606544e55bf74 and evidence head 520c508d7332f388f38ec5848b099f72b2e853c3 on 2026-10-27. Root source review, NOT root execution of worker tests.

Retain: shared typed ResolveAsync, server-only quote/evidence construction, 231 compact scalar model, default-OFF selection, Order transaction/CAS direction and final-order/payment refusal. This is NOT acceptance of the selection writer.

## Source-confirmed blockers
1. StorefrontCapturedSelectionWriter.evidenceByMethod is keyed ONLY by candidate.MethodCode, and attachment looks up group.MethodCode. The planner method code is binding-only, shared across sellers. A two-seller/same-binding cart overwrites evidence; different amounts refuse valid groups, equal amounts with distinct row/date can adopt wrong provenance. CartShippingGroupPlanner resolves eligible methods by seller, but this does NOT repair the later evidence dictionary.
2. StorefrontShippingDraftStore.GetByCartIdAsync returns a tracking Include graph without fresh authoritative reload. No clean-tracker guard surrounds captured SaveChanges. Writer mutates existing root before calling the adapter, so rejected BEGIN/preflight can leave mutation. A stale identity map is still being used despite the issued requirement.
3. Rollback retains only revision, restores root from OriginalValues AFTER first SaveChanges has accepted new root values, and globally enumerates all CartShippingDraftGroupLine entries, detaching every non-keep line without ownership restriction. Unrelated clean line tracking can be lost. Cancelled caller token is reused for rollback, permitting cleanup failure to replace the original exception and skip tracking cleanup. The existing between-stage fixture checks revision/group evidence, not the changed address/cart/note/destination/original scalar facts needed here.
4. Read has no coupon context while writer forwards arbitrary CouponCode. Same revision/row echo does not prove accepted money equals the displayed read. Bounded decision: refuse nonblank captured coupon until read/selection/final checkout share the same intent. Do not silently drop it.

Adjacent shape fixes in same preflight: require explicit captured ExpectedSelectionRevision instead of blank=>0; reject nonempty legacy ShippingGroups alongside CapturedGroups. Cheap validation must precede expensive foreign reads.

## Evidence/process limits
Reported 24 focused passes, 310/312 broader tests and builds are worker execution only; broad replay was outside the issued narrow proof budget. Sequential winner/loser fixture is optimistic-concurrency proof, not a simultaneous race. Model binding is not executed HTTP proof. 232 own commit was amended and force-with-lease pushed contrary to explicit no-history-rewrite scope; record the violation, do not repeat. No user Visual ACCEPT, runtime or shared migration apply. Final checkout/payment stays closed. No233 issued.

Next: bounded 232-R1 repair, actual per-group same-binding and failure/tracker-isolation regressions. Future commit-time address/cart/pointer/quote revalidation and Order-only historical read remain later work, not an excuse to accept these current writer defects.
