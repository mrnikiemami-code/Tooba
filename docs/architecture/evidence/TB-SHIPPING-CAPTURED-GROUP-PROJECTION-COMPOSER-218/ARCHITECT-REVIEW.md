# Architect review — TB218
Source: 6b3f3d2e940641f5b735d97b9da975acd2dcc05f; base f8b77aebb8377b82ac2c5f94355f36023b0a2374.

## Decision
Retain the composition implementation, but request one bounded correctness repair before closing218. NOT Storefront/live/activation acceptance.

Independent review: exact changed files and composer/contracts/test patches; 217 real reader inspected for membership order and financial source. No root build/test/runtime execution. Worker reports 11 new and 9 retained facts plus clean builds; these are worker-executed evidence, not root reruns. Worker did NOT update the canonical recovery in its source diff; this checkpoint updates it.

The source actually calls one whole-cart goods reader, forwards each group's post-discount tax-inclusive goods amount, keeps unknown physical measurements unknown, quotes through Shipping Contracts, and uses one clock + method timezone + accepted receiving calendar. No current Storefront consumer, public route, writes or active-capture selection is added. Max delivery duration stays metadata, not a guessed booking cutoff.

## Concrete remaining defect
ValidatePlan calls ValidateCandidates separately for each group. ValidateCandidates creates a fresh seenBindings dictionary each time. Therefore group A can request owner X / service express / option std at draft D revision1 and group B can request the SAME binding at revision2; both pass plan validation and invoke financial/Shipping work. This contradicts the task's single selected captured revision per owner+binding requirement. Group-specific goods/destinations do not authorize different published revisions.
Also seenBindings is Ordinal and includes raw service casing and destination, whereas IsBoundToCandidate compares service/option OrdinalIgnoreCase. A case change (or different group destination) evades identity consistency.

Required repair218-R1: one request-wide typed owner+canonical service/option key, no destination or method-display code in publication identity; retain exact owner equality semantics (do not lowercase tenant identities). Compare exact draft+revision values. Allow the same binding and same revision in separate groups, each with its own financial facts/destination/row quote. Preserve independent owners/bindings, per-group duplicate method check, and all existing monetary/calendar behavior.
Only focused executed regressions and one OrderTests build/type compile; no broad old-battery loop.

## Limits and process
Source review is not real Shipping provider or live checkout evidence. Missing published selection/fulfillment owner/type authority and unknown physical measurements still block live consumption. No new policy is inferred. Time33>25 and two correction rounds do not pass process compliance.
