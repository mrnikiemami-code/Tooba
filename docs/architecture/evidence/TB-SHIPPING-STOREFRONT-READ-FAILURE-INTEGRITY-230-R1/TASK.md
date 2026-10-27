PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-STOREFRONT-READ-FAILURE-INTEGRITY-230-R1
Parent-Task: TB-SHIPPING-STOREFRONT-DESTINATION-CONFIGURED-TRIAL-230
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Mode: ONE_FAILED_DESTINATION_REFRESH_STATE_REPAIR_PLUS_TWO_DISPLAY_FIXES
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
HARD20min / source12 / proof+recovery8. One correction only.
230 POSITIVE CONFIGURED READ ACCEPTED. Do not repeat auth/cart/draft/publish/setup or backend/calculator audit.

READ REVIEW230/TOP FIRST
Root viewed actual1440/390 priced cards. Existing test account/cart/profile, DEV method and runtime are ready.
User post/NULL draft, DEV draft/rates/selected pointer, assignment, cart and auth stay untouched.
No new migration/seed/Host rebuild or restart; frontend source changes only. Reuse task-owned browser/driver,
not user tabs. Retain checked-in defaultOFF/local read-previewON/count7/TB185paused/PendingAllocation.
No selection/save/commit/order/payment; server not_ready stays.

ONE REAL DEFECT — CURRENT FAILURE MUST NOT REVEAL AN OLD DESTINATION'S PRICE
Current refreshProjection:
- leaves old successful projection in state;
- hides it only via capturedDestinationPending;
- finally clears pending on BOTH success and exception;
- outer catch just sets error;
- appliedDestinationKey advanced BEFORE success, so same-key retry is skipped.
Therefore new destination request failure resurrects old cards/price as current and cannot retry normally.

Repair this in the EXISTING refresh path/coordinator, no general fetch framework:
A successful projection must remain associated with the exact verified destination identity it describes.
When current destination differs or its refresh failed, old money/dates MUST NOT reappear as current.
Show a compact existing-style unavailable/retry state; keep product lines/typed recipient/address intact.
Pending, succeeded, failed/current identity must not be inferred solely from a loading boolean.
Provide ONE explicit user Retry for the SAME failed key; no auto retry, render/request loop, no duplicate click.
Current success clears the correct failed latch. Stale success OR stale error cannot mutate the new request's
projection/error/pending state. Legacy OFF ordinary behavior remains unchanged.
Do not set applied/confirmed key to success merely because a request was started. Requested and confirmed
identities may be tracked separately in the smallest existing mechanism.

TWO PRESENTATION FIXES ONLY
1 In captured exact money formatter: IRR whole amounts with an all-zero fraction (120000.00) DISPLAY120,000
   with exact grouping/local digits; keep raw token untouched, explicit0 valid. Nonzero fractions must NEVER
   be rounded/truncated; preserve honest exact display for unexpected stored fractional values. Other
   currencies retain existing decimal text. No Number/parseFloat/FX/business mutation.
2 In existing captured card: isolate interval text in bdi/dir=ltr so09:00–17:00 visually reads in correct
   order in RTL. No reversal of stored hours, no new slot/calendar design.

FOCUSED VERIFICATION
Meaningful current code-path state tests using controlled promises/state (not source-regex):
old destination priced ->new request fails ->old money stays hidden; same failed key explicit retry ONCE ->
success shows correct new quote; stale failure after new success ignored; stale success ignored; unrelated
render no request; typed form values retained. Exercise the same coordinator/state decisions the page uses,
not a parallel toy implementation.
Formatter facts: IRR120000.00 display integer with raw unchanged, IRR0.00 zero, nonzero fraction preserved,
non-IRR decimals unchanged. ONE final frontend typecheck + affected focused tests only, no backend/full suite.
Reuse existing task browser/account/cart/normal logged-in session; fresh task target allowed in task-owned
profile only. No new account or driver.
Observe current positive card and RTL interval at390. If possible use actual Chrome OFFLINE network emulation
on the TASK profile (no fulfilled/mocked responses) after a prior successful quote, change province using
already-loaded options, verify failed request does NOT restore old price; restore network and click Retry,
verify one current successful real request. No user tabs or shared network manipulation.
If failure state is test-proven only, say so; do not invent mounted failure proof.
Screenshot native390 with actual card/interval IN viewport, not hero crop. No need repeat all widths/data.

FINISH
Evidence folder docs/architecture/evidence/TB-SHIPPING-STOREFRONT-READ-FAILURE-INTEGRITY-230-R1/.
Update three TOP recovery files (AGENTS, START-HERE-IF-CHATGPT-IS-LOST, canonical shipping recovery).
Preserve230 accepted positive read and actual DEV data; record failed-refresh gap corrected or remaining.
Do not relabel full Storefront as delivered. Next only after review: compact immutable order-group evidence
+server-selected revision revalidation across selection/commit. No full JSON per order/historical repricing.
Known cross-owner label-map/decoder limits,227 mobile residual and225-R1 unrecovered user-tab loss retained.
Exact-path commit/push/fetch parity, no add-A/history rewrite/Bridge/post-result script.
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. User Visual ACCEPT pending.
