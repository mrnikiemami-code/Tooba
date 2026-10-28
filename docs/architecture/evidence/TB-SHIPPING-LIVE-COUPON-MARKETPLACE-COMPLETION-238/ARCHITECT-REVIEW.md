# Architect review — TB238 — 2026-10-28

Reviewed source/main600a96f1acf94f05dda8a69f80e7f8981b0276e5, actual payment/coordinator/page diffs,
complete current refresh/page path and worker RESULT. Also read actual shared OutboxSaveChangesInterceptor,
OutboxMessageMapping, MessagingCorrelation and CorrelationIdContext relevant to the newly reported22001.
Root did NOT run the local Windows requests/tests. Verdict: RETAIN valid-coupon SingleStore HTTP milestone;
NOT Marketplace delivery, browser coupon-completion certificate, double-submit success or mobile-payment acceptance.

## Retained
New payInFlightRef guards the actual payment page before await, releases finally. Current destination/coupon
response identity and current-only intent gating fix prior raw-projection selection weakness. Empty destination
now has a nonempty failed/retry identity. Existing pricing/owner/transaction/idempotency unchanged.
Recorded normal HTTP valid DB coupon -> same goods/read/selection/paymentpreview/final order with manualPending
and1reservation is useful scoped live evidence. Inclusive goods9485771 ->8537194 difference948577; exclusive
discount870254 differs because of Tax, not automatically an arithmetic defect. Payable8537194+125000=8662194IRR.
No partial applicability/mixed owner live proof; no PSP/paid or final release/Visual ACCEPT.
Payment stepper local wrap/shrink source is retained, but there are NO actual payment390/360 captures. New
shipping-page captures cannot certify a DIFFERENT payment page;237R1 payment viewport defect remains unverified.
Worker APIs vs browser interaction must stay distinguished: RESULT states coupon via HTTP, not full observed
coupon UI workflow. New screenshots are root/screens/ship-*; keep their real path, not fictitious evidence folder.

## Double-submit failure is not a passing concurrency proof
Two real requests reportedly BOTH failed SQLSTATE22001, no checkouts; later single submit/replay succeeded.
This proves failure, NOT concurrent idempotent acceptance. The new UI ref isn't a backend duplicate-write fix.
The exact failing column attribution is NOT yet proven by the repository/committed receipts:
- OutboxMessageMapping.CorrelationId max128.
- OutboxSaveChangesInterceptor calls MessagingCorrelation.ResolveForPublish before writing.
- MessagingCorrelation normalizes metadata Guid, else ambient CorrelationIdContext, else EventId N.
- CorrelationIdContext's public setters normalize to32 hex.
Thus a normal current correlation should fit128. Check actual PostgresException schema/table/column fields,
failing parameter LENGTHS and exact current assembly/process/input provenance.22001 alone doesn't identify a
column. Could be different128-column, malformed test intent, runtime mismatch or another direct writer;
do not invent root cause, enlarge schema, truncate audit identifiers or swallow the provider failure.
Trace only this concrete failing submit, not a generic Outbox/Inventory architecture audit.

## Remaining honesty corrections
"Invalid coupon read200 by tolerant design" is not established: actual planner composition only catches known
plan/facts/binding refusals, while Directory quote throws checkout.coupon.not_applicable. A200 may have an early
destination/assignment refusal and no quote, not a priced invalid coupon. Next live record must include full
request/current verified destination plus sanitized body/code, and distinguish early-unavailable200 from
discounted/priced success. No silent invalid-coupon acceptance is approved.
planDestinationRefresh still returns SKIP for ANY inFlight; new identity waits for the old request. This was NOT
fixed as new-key immediate cancellation/resolution. Frozen coupon input also isn't passed as an explicit request
argument: page reads mutable coupon ref after cart await. Do not claim every in-flight context race closed;
freshness gating is safer, remaining behavior is a bounded live follow-up only if it blocks required trial.
Marketplace boot target wasn't created. React-input automation difficulties in SingleStore are NOT evidence of a
Marketplace configuration/architecture blocker; next task starts actual Marketplace runtime/API proof FIRST.
Partial applicability, real merchant grant denial and post-Order publication-change/Order-only GET still open.
61min vs40 / multiple corrections NONCOMPLIANT. Reuse working read runtime, don't repeat broad tests/bootwork.
Handoff user3100/5188/profile is preserved; never enumerate/read/reload it. All3 TOPs updated after this review.

## Next
TB239 starts actual Marketplace edition/DB, scoped seller APIs and normal buyer HTTP BEFORE UI automation.
Prove real three-owner shipping/order path, coupon partial applicability and original historical Order read; then
small sameUI browser smoke/payment native captures. Classify/reproduce22001 with real native bounded intent;
fix only confirmed producer/validation/runtime cause if necessary, no schema widening or transactional bypass.
Current SingleStore coupon milestone remains available, all final owner requirements remain binding.
