# Architect review — 240-R2

Reviewed main77ed71dfb709df7806018875d833edd9540121db/source769f43ff5c3339ae78c74dacb4929168bf8801a6 and actual planner/writer/OrderDbContext, original20261016 group migrations, PaymentDirectory and AccessControl prelude. Root did not execute Windows tests/runtime/browser.

Accept reported reached milestone of real seller commissioning/captures and THREE PRICED groups, within scope only. Full delivery remains BLOCKED, not accepted. Main index failure is source-confirmed in BOTH cart and final Order tables. Architect decision: ONE new forward migration changing BOTH unique seller/method indexes to legacy-only WHERE accepted_snapshot_id IS NULL, preserving complete-capture checks/PK/line uniqueness. Existing group id+membership+accepted owner is sufficient; no redundant owner discriminator column. Do not alter applied migrations or old orders. Normal runner apply only verified taskMarketplaceDB authorized.

R2 planner correction is incomplete: ResolveMethod returns first sameObligationKey row without seller/binding checks, falls back when key absent in eligible set, and globally relaxes legacy duplicate-seller/method invariant. Must require exact-one full keyed match, no captured fallback, and preserve legacy no-key duplicate rule. Source regression is a blocking review finding, not accepted from live positive read.

Payment winner-on-unique-conflict improves manual replay response, but source invokes gateway.InitiateAsync BEFORE SaveChanges, so simultaneous provider effects can already have occurred twice. One persisted row/attempt cannot certify externalprovider exactly-once. Current conflict matching is substring-only and loser lacks normal actor-access recheck; tighten exact index/table/schema and recheck access before winner return. No paid/PSP certification. CamelCase observation was withdrawn by actual listener proof, no serializer edit needed. checkoutReplayed false remains disclosed.

R2 parity claim231/232 sharedNOTAPPLIED is stale: earlier237-R1 applied normal store-alpha and239 taskMarketplace chain. Inspect actual target history; do not echo false state or rewrite it.

Timing:216 of240 reported focused minutes consumed,24 left before other actual focus. Architect's original completion estimate was optimistic; completion cannot be guaranteed in remainder, scope not waived. Continue existing fixes/runtime with explicit migration authority and report actual unmet delivery at deadline, never resetclock/partial-as-final. No userVisualACCEPT.
