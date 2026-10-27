# Architect review — TB-230-R1 — 2026-10-27

Reviewed main 399660434b5b3e4888201f33d052a73697bfcfd0, compare against6d0a506b2a10c95868daf23241a4d57ce8ac0127.
Verdict: ACCEPTED FOR READ-ONLY USER RETEST of the demonstrated populated-destination failure/retry,
IRR all-zero fractional display and interval direction. NOT user Visual ACCEPT, checkout or final safety
certification. Architect read source/state/test diff and VIEWED committed390 failed/retry-success PNGs.
Root did not run Windows tests/browser; worker22focused+typecheck remains worker execution.

Confirmed source: requested/confirmed/failed destination identity separated, current generation gates
projection and failures, no finally clears loading to reveal stale quote, synchronous retry gate, exact
IRR integer display only when fraction allzero, bdiLTR for stored interval. Source+screenshots agree:
failed state hides money/dates, retry image shows correct120000/5000, actual group/product lines and
09:00–17:00 in correct direction, second unassigned group retained after success.
During failure/loading product cards are hidden (not deleted) and return on success; disclosed, no data-loss
claim. This is a bounded presentation limitation, not a reason to redesign again before user visual feedback.

Important remaining boundary, not silently included in acceptance:
snapshot uses empty string for BOTH no-destination key and no-failure sentinel. A failure at key"" cannot
satisfy canRetry(failedKey!==""). R1 only proves the populated-key case; empty/no-destination failure retry
is not accepted. Also an in-flight request currently suppresses new-key planning until it settles. Record
these for parity production hardening; do not claim every arbitrary-failure state is solved.
Earlier cross-owner label map, decoder id coercion/date-shape limits and Offer API proof gaps remain.
SingleStore visual trial does not certify Marketplace. Existing servernot_ready guard MUST remain.

User read trial is now reviewable on /fa/shipping using the already authenticated TASK profile/account/cart,
local previewON/count7. Credentials remain local/private.231 may open a separate visible TASK review window
for the user, never enumerate user tabs. Once handed off, that window belongs to the user's unsaved work
and must not be reloaded by automation.

Next implementation is compact evidence attached to EXISTING CartShippingDraftGroup/OrderShippingGroup and
materializer, not another quote engine or full authoring JSON snapshot per order. No live checkout enablement
until later selected-revision/cart/price/schedule revalidation and payment-payer correctness prove parity.
