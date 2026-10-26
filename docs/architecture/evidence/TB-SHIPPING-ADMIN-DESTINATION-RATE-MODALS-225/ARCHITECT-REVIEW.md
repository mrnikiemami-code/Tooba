# Architect review —225: bounded repair required, not yet user-trial accepted
Reviewed source2e6aed0951a5e1ef75d5206d77c4df9f433f55b1. Root read actual rate modal, money field/model,
editor-rates and breadcrumb resolver. Root did not execute Windows browser or worker tests.
New modal/cascade/mobile work retained; worker reports22focused/188recursive/typecheck/live read-only.
Acceptance blockers:
1 MoneyField routes every non-paste change to interpretMoneyTyping; it strips commas from prior INVALID
user tokens too. Paste12,34 then append5 can silently become accepted12345, violating invalid-text policy.
2 interpretMoneyTyping strips grouping but uses the ORIGINAL caret offset on shorter text. For1,2934 caret4
after inserting9 into1,234, it anchors4digits instead of3, moving insertion position.
3 modal fraction tolerance is computed on open and passed unchanged; current edited200.75 can inherit
stored100.50 field tolerance and Confirm falsely accepts, even though parent later blocks Apply.
Also confirm handler needs editable guard, not only disabled button.
Issue225-R1 narrow field/modal repair, no UI redesign or broad replay. No user trial before re-review.
Breadcrumb resolver's city reads are nested countryIds x adminIds and execute serially: not accurately
constant one-per-level proof; preserve correct scope and report actual distinct branch counts. No performance
certification. Null-admin ServiceArea trail may fall back to leaf-only when no city label fetched; don't claim
all full paths live-proven. Existing Tehran has no service-area fixtures; service Add remains test-only.
No new backend/snapshot/data mutation; no Storefront connection/Visual ACCEPT. Process timing noncompliant.
