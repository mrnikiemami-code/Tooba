# CURRENT — 264 PARTIAL; ONLY 264-R1 ISSUED; 265 NOT ISSUED — 2026-11-12

Read docs/architecture/evidence/TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264/ARCHITECT-REVIEW.md and docs/ai/tasks/TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264-R1/TASK.md.
Concrete review gaps: zero preparation allowed forward but rejected backward/DB; selected customer window/zone missing from backward deadlines (zero-transit10–11 vs carrier17 close); silent24/7 preparation and instant-ready new-commitment fallback; missing real authoring save/publish/selection handoff.
Only264-R1 fixes these original-scope seams A–D, retaining existing timing owner/evidence/queue. No265 implementation until result reviewed.263 remains accepted within scope, user acceptance pending.
One worker/task, no delegation/full-suite/base-parity/repeated-green/screenshots. Same canonical tooba_alpha5432/FE3000/Host5088 SingleStore. Minimal labelled owning-path fixtures only, user/screenshot rows read-only, no extra DB/reset/reseed/global reclaimer. Additive correction migration only via runner+backup; old accepted plans not recomputed.
Messaging existing Toast/MessageBox; active-order hold retained through parcel cancel/return/retry/timeout, dispatch consumes once. Future approved Returns+owning line-obligation cancellation release belongs265; opening case alone not release.
Plan SHIPPING-USER-FLOW-CLOSURE-263-265.md. Preserve DataProtection/CapturedPreview;255 parked/Tipax deferred.
STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE. Prior CURRENTs below superseded.

---

# Shipping user-flow closure 263–265 — 2026-11-11
Latest user instruction: fix the defects, issue sequential tasks, review each result before the next. Smooth operator flow using existing Toast and MessageBox; no repeated repair/test loops.
262-R1 accepted ONLY its three repair seams; new user trial reopens functional acceptance.

## Sequence
1. **263 ACCEPTED within issued scope — user functional acceptance pending** — TB-SHIPPING-REPLACEMENT-RECOVERY-AND-OPERATOR-FEEDBACK-263.
   Reproduced on the read-only screenshot record: the durable decision existed (`Recorded`) but the admin projection
   dropped its `occurrenceKey`, so the resume posted a blank key and the owner answered the deterministic 409
   `fulfillment.damage.resolution_occurrence_required`; every non-ok answer was then presented as an unknown response.
   Repaired at the owning seams (projection forwards the key; ONE completed/pending/refused/unknown classification;
   existing Toast for confirmed success; existing MessageBox for a blocking refusal; all `fulfillment.damage.*`
   localized). See docs/architecture/evidence/TB-SHIPPING-REPLACEMENT-RECOVERY-AND-OPERATOR-FEEDBACK-263/RESULT.md.
   No financial or calendar feature was added.
2. **264 WORKER PASS — pending Architect review** — TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264 — readiness-aware shipping calendar and operational start deadlines. Current 256 policy assumes nowUTC is goods readiness; user explicitly rejects that assumption. Inspect actual existing per-item/offer/seller preparation inputs and operational working calendar before adding settings; don't invent duplicate lead-time fields. Model readiness (processing/packing), transfer to carrier, actual acceptance window/pickup cutoff, MAX transport working days and independent customer receiving window. If window is opening hours it is not an invented fixed pickup appointment. Preparation cannot run through closed warehouse hours unless explicit policy allows it. Before ordering, offer customer dates ONLY from first feasible handoff after readiness+transfer; max2–3=>3. At carrier close exactly => next acceptance; 16:59 can also miss today's 17:00 when preparation/transfer takes time. No double counting: existing authoritative preparation time plus only distinct missing transfer/buffer.
   For chosen customer date, derive latest feasible carrier handoff AND latest preparation start using same authority, save accepted evidence and queue from durable facts (no current-config recomputation of old orders). Customer promise stays separate. Multi-item common group readiness is constrained by all required members, not summed parallel preparation blindly. Separate parcels retain own work scope. Revalidate availability at acceptance; historical plans remain explicit legacy/unknown, not fabricated/backfilled.
   Acceptance examples: 14:00+2h feasible before17,16:00+2h misses17; weekday noon-close, closed warehouse/carrier/customer day, cross-zone, chosen later date; queue shows when preparation must start and handoff deadline.
   Worker result: the physical preparation/transfer pair is now an explicit owned authoring policy carried through the captured
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
