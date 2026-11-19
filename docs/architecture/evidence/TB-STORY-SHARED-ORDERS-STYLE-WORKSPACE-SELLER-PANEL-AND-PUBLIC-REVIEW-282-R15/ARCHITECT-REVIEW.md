# ARCHITECT REVIEW — TB-282-R15

Date: 2026-11-20 (Asia/Tehran)
Code: 877080f043de029d6cc9ffb0261702f7975d64b2
Worker evidence/head: 277dfd046491998e4d5ae75288a0b6a402e493f6
Expected base: 8c6fe36325f181aba213fb655d735cc48c7384c7 (GitHub compare confirms ahead).
Decision: ACCEPT_REJECTED_TRANSPORT_CLOSURE_FOR_LOCAL_OWNER_TEST
This closes the R14 connection-loss blocker. Not production release approval or owner acceptance.

## Reviewed behavior
The production delta is limited to Story executor transport catches, uncertain-result classification and R15 tests. JSON and FormData fetch rejections now return story.transport.rejected/status 0 with the same envelope; no synthetic success or clearing is introduced. The existing batch uncertainty branch records unknown, preserving prior successful/skipped results and allowing the existing result Dialog to appear. Existing epoch/scope checks remain authoritative before later dispatch.
The catch is at the fetch boundary; its network classifier currently has no behavioral effect and all caught rejections become uncertain. This does not invent success; route validation remains outside that catch. No broader executor redesign requested.

## Evidence and exact limits
- Worker receipt reports pre-fix SAME mounted rejected-fetch fact failed because result Dialog was not reached (~630ms), then R15 2/2 + R14 4/4 and tsc 0. The receipt is a summarized worker observation, not an architect rerun or a preserved full test log.
- Read the actual R15 mounted test: real AppDataGrid selects two eligible + one skipped, second fetch throws TypeError, result panel lists names/outcomes, no unhandled rejection observed, A cleared and B pending. The test closes the result Dialog before visible recovery; replay body bytes match and successful A is not resent.
- The test does not explicitly assert B's pending removal after bound replay, nor capture replay URL separately. Matching-envelope clearing/route identity rely on retained executor behavior, not a new R15 assertion. Do not describe those as additional mounted assertions.
- The second R15 fact is a direct batch-helper test, not mounted unmount/browser proof: deferred rejection plus epoch/actor change yields first unknown, second not_started, secondHits=0.
- FormData catch is source-reviewed; R15 does not independently prove real upload recovery.
- Architect read remote commit/source delta and committed tests/receipts; did not rerun Windows tests, serving or browser geometry.

## Owner handoff
At /fa/admin/stories, test single/bulk approve, reject with reason and intentional test-story deletion. Check confirmation, ineligible-row reasons and per-row results. Recheck no-end beside end label with aligned dates. Only test records deliberately chosen by owner should be deleted.
At /fa/vendor-panel/stories, retain seller's own-shop workflow and submit for review. Public /fa verification follows lawful approval and the publication window; no live end-to-end walkthrough is claimed by this review.

## State and retained limits
State: AWAITING_LOCAL_OWNER_TEST
activeTask: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_USER_REVIEW
No R16 issued.
Host5088 skip-bootstrap=1 / Next3000 next-dev are worker-reported local serving facts. Production Html404 prerender build blocker remains unresolved; no production BUILD_ID or release readiness claimed. critical-storefront bg-white limitation and R6 bootstrap effects UNKNOWN remain preserved.
ZERO architect live Story/Media writes. No backend/schema/grant/seed changes in this delivery. Preserve VendorShell, language authority, Returns, payout backlog and user data.
