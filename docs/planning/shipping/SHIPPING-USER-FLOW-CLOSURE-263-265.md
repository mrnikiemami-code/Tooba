# Shipping user-flow closure 263–265 — 2026-11-11
Latest user instruction: fix the defects, issue sequential tasks, review each result before the next. Smooth operator flow using existing Toast and MessageBox; no repeated repair/test loops.
262-R1 accepted ONLY its three repair seams; new user trial reopens functional acceptance.

## Sequence
1. **263 ACCEPTED within scope** — TB-SHIPPING-REPLACEMENT-RECOVERY-AND-OPERATOR-FEEDBACK-263.
   Reproduced on the read-only screenshot record: the durable decision existed (`Recorded`) but the admin projection
   dropped its `occurrenceKey`, so the resume posted a blank key and the owner answered the deterministic 409
   `fulfillment.damage.resolution_occurrence_required`; every non-ok answer was then presented as an unknown response.
   Repaired at the owning seams (projection forwards the key; ONE completed/pending/refused/unknown classification;
   existing Toast for confirmed success; existing MessageBox for a blocking refusal; all `fulfillment.damage.*`
   localized). See docs/architecture/evidence/TB-SHIPPING-REPLACEMENT-RECOVERY-AND-OPERATOR-FEEDBACK-263/RESULT.md.
   No financial or calendar feature was added.
2. **264 WORKER PASS — pending Architect review** — TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264.
   The physical preparation/transfer pair is now an explicit owned authoring policy carried through the captured
   quote; ONE forward/backward authority turns the trusted eligibility instant into feasible carrier handoffs and
   customer dates and derives latest preparation-start / transfer-departure / handoff-deadline instants; the
   policy-v2 triple persists in accepted evidence behind an additive all-or-nothing constraint (v1 stays readable);
   the existing finance-free queue gains its own preparation state/due filter and the shared operations timeline
   shows the deadlines beside the customer promise. No second queue/settings app, no money change.
   See docs/architecture/evidence/TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264/RESULT.md.
   Boundary: acceptance proven through the production resolver + real DB + actual queue query, NOT a live paid checkout.
3. **265 PLANNED / NOT ISSUED** — connect courier-origin return to EXISTING Returns flow. Inspect Returns eligibility/finance/inventory APIs (delivered-customer RMA may currently refuse never-delivered source). Reuse Contracts and owning processes, no parallel refund module or fake Delivered. Three routes: resend same healthy goods; healthy-stock replacement with distinct reservation; create linked Returns case with existing origin receipt reused. Cover usable as well as damaged quantities. One source quantity cannot fund simultaneous resend/replacement/refund branches; concurrent/retry exact authority.
 Latest reservation rule: opening a case alone doesn't release. Approved return plus owning cancellation of relevant order-line obligation releases only its unconsumed held stock once, blocks further send for that quantity; consumed original reservation isn't released or resurrected. Existing whole-order post-dispatch cancellation restriction must not be bypassed; explicitly integrate line obligation policy instead. Already-dispatched replacement needs physical recall/receipt, cannot be treated as unused hold. Refund amount/approval/execution remains Returns/Settlement/Payment-owned; damaged disposition isn't saleable restock. Referral in262 is NOT this integration. Preserve case audit links and exact replay, no double receive/refund. Scope/issue after actual code review and263/264 results.

## Communication
Existing react-toastify for transient confirmed success; existing MessageBox for blocking acknowledgement/decision; adjacent field validation; compact persistent business status. No technical diagnostics, no duplicate success/error surfaces, no unsafe abandonment of uncertain commands.

## Runtime and stop rules
One worker/task at a time, review then next. No automatic worker continuation. Same tooba_alpha5432, FE3000/Host5088 SingleStore; no new DB/reset/reseed. Minimal labelled production-path fixtures only; user rows read-only. Never global mutating reclaimer fixture test. Preserve DataProtection/CapturedPreview.255 parked, Tipax deferred.
Do not claim all complete. Issue263 result -> architect review -> next bounded task264; after265 user milestone unless concrete blocker requires decision.
