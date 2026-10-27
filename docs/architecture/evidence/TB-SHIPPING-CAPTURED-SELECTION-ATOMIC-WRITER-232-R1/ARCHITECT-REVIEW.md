# 232-R1 Architect review — data repairs accepted; one cleanup blocker remains

Source reviewed: 5c38f97619609aef20b0fcca05d64034b193289e (main, 2026-10-27).
Root read the actual adapter/writer/ports/service patch and relational test source. Worker test results are reported execution, not root execution.

## Accepted within scope
- Binding-only evidence dictionary removed. Typed commitments correlate seller, binding and exact member set one-to-one with planner groups; GroupKey remains server plan identity. No list-index/label adoption. Same-binding two-seller evidence isolation is now explicitly covered by writer and relational fixtures.
- Captured-only preflight refuses pending caller work; same-cart tracked graph detached and fresh complete AsNoTracking graph loaded; prepared replacement no longer mutates a tracked root before begin.
- Explicit expected selection revision, mixed legacy-group refusal, nonblank captured coupon refusal. No unsupported discount silently quoted against the read.
- Ordinary rollback detaches only named attempt/old graph and reloads persisted root; global unrelated-line sweep removed. Successful legacy behavior and final-payment refusal retained.

## One blocking cleanup defect
Both ReplaceCapturedSelectionAsync and CreateCapturedSelectionAsync allocate IDbContextTransaction with no disposal/finally/await using. Commit/Rollback may clear EF CurrentTransaction internally, but do not constitute disposal of the owned transaction object.
Furthermore, rollback runs before owned tracking detachment and only InvalidOperationException is caught. Another rollback failure skips detachment and masks the original exception; subsequent recovery query can also mask the primary failure. This violates the issued primary-exception/no-leak contract during an actual provider failure.

Test limitation: Caller_cancellation_leaves_no_attempt_and_the_old_selection explicitly cancels its token BEFORE calling ReplaceCapturedSelectionAsync. This proves pre-cancel safety, not cancellation after deletion/root save. Strengthen at the real boundary, do not relabel the original test.

232-R2 is limited to transaction lifetime + cleanup failure resilience and exact focused regressions. No redesign of accepted group/value/owner/coupon behavior. Overall232 selection writer remains NOT ACCEPTED pending that proof. No233; final order/payment/history work stays closed.

Reported broader285/286 run was beyond the narrowly requested new/affected classes; retain red packaging baseline disclosure, do not repeat broad batteries. R1 no history rewrite confirmed from single commit parent. No shared migration/runtime/UI/user-window change. User Visual ACCEPT not claimed.
