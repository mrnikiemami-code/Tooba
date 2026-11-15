# Planned closure — resend/replacement timing, then employee work queue — 2026-11-15

Status: 270 RESULT DELIVERED (PARTIAL — design/ownership only, zero code change); awaiting Architect review. 271 NOT ISSUED. 269 ACCEPT within presentation-only scope. No concurrent worker/new task. User requests conditional continuation: review269 result; if defective close its exact defect first, otherwise fill confirmed resend scheduling gap. 270's own finding: the durable own-plan write path needs ONE additive Shipping scheduling-inputs Contracts read first (the accepted evidence has the plan outputs but not the week shapes, and the only existing read that has them is the rate-quote lookup that needs a destination the order no longer owns). Recommended next issue: 270-R1 = that seam + its focused test, 270-R2 = Order-owned attempt plan + reviewed command + preview + queue/detail + review step.

## User intent / constraints
Employee must see WHAT to do on WHICH existing package and WHEN, not decipher general status filters. Reuse existing business facts/settings/authorities and current orders; no additional demonstration dataset, new database, repeated cleanup or invented dates. Existing user data stays intact. Internal plan persistence for a genuinely new shipment commitment is necessary operational evidence, not permission to create new demo orders or settings. Explain any required new owning fact in task; never imply already stored.
User functional feedback positive for exercised shipping/return/resend;267 improved UI,268 tabs reviewed pending visual feedback.269 fixes queue date formatting only. Do not reopen successful lifecycle/payment/quantity/money/notification architecture.

## Confirmed source
- Fulfillment.Infrastructure/Queries/AdminFulfillmentWorkUnitProjection.BuildParcelRow: any shipment.ResendOfShipmentId!=null gets plans=[] instead of original accepted-group plan (also covers linked healthy-stock replacement in262).
- Original accepted dates/plans remain owned by Order group; never borrow original deadline after goods return.
- CheckoutDirectory.CapturedAcceptance explicitly permits truly legacy submissions with no saved captured evidence, and historical evidence without plan remains readable. This is NOT a guarantee every new order uses captured path.
- New captured availability requires carrier week and feasible receiving schedule in StorefrontCapturedGroupCompositionCore; StorefrontCapturedSelectionWriter resolves planned dispatch on acceptance.
- Four initial266 legacy fixtures remain uncaptured. Don't retrofit fake accepted evidence.

## Gate0 — finish269 first
Read actual result/diff/served display evidence when available. Verify Persian DateOnly without timezone-day shift, accepted-zone deadline display, query/sort raw values unchanged. Bounded correction only if real269 defect. Then issue ONE implementation task for next stage below; result review before queue stage. No automatic chain without review; user visual check at meaningful milestone, no loops/whole-suite repeats.

## Planned270 — own a new dispatch plan for returned-source resend / healthy-stock replacement
Goal: new parcel has its OWN durable preparation/handoff/customer commitment context and queue deadlines.
Design before code within same bounded task, through existing owner/Contracts seams:
1 Treat healthy same-goods resend and replacement as distinct funding/readiness sources. Resend readiness cannot precede actual physical origin receipt/usable goods and required processing; replacement readiness cannot precede lawful new healthy stock availability/reservation and preparation. Do not unconditionally equate order original now or original dispatch time to new readiness.
2 Reuse ShippingDispatchSchedulingAuthority (current policy3) for forward feasible dates + backward deadlines, existing preparation/transfer allowance and processing/carrier/customer weeks. No frontend arithmetic, new scheduler/calendar service or duplicated policy. Read relevant original captured method/scope and current validity through owner Contracts; decide explicit snapshot binding for new review, record it. Do not silently borrow another seller/method or automatically use changed current settings as historical truth.
3 Review new shipping commitment through SAME existing resend/replacement dialog/owner. Original selected receiving window may only be reused if feasible for the NEW readiness. If elapsed/unreachable, offer a newly feasible date/window for deliberate confirmation; do not rewrite old accepted customer date or assert customer agreed without evidence. New schedule is a new shipment attempt commitment, not backfill of original order. No need to add a setting if existing policy inputs suffice.
4 Durable new-plan ownership: Order owns original/new customer delivery obligation commitments, Fulfillment owns physical source/attempt/parcel and lineage, Shipping owns pure schedule policy. Persist immutable binding/readiness/calculation inputs/outputs + accepted date/window/timezones + preparation/departure/handoff deadlines via narrow owning Contracts, with exact occurrence intent. Determine narrow existing persistence seam rather than invent broad scheduling framework; minimal additive schema only if required.
5 Availability no longer feasible between review and submit -> exact localized refusal retaining reviewed envelope, no silent clamping/recompute-and-save. Same request after lost response answers same parcel+plan; changed date/method/source under same key conflicts. Replacement no-stock pending can retain decision, but MUST NOT persist an actionable dispatch promise based on nonexistent healthy stock; resume readiness/plan review coherently when stock exists, preserve same hold/decision laws.
6 Existing active resend parcels without plan remain honest: no automatic guessed backfill, no borrowing original first-dispatch plan. Provide explicit authorized planning/confirmation for those existing parcels if needed. Completed history stays unchanged. Missing legacy capture is explicit blocker, not bypass or fabricated commitment.
7 Queue scoped plan reader chooses new parcel's own plan for resend/replacement; first dispatch keeps original group plan; exact physical unit row, no sibling plan lending/count inflation. Future plan cannot make an already completed parcel actionable.
8 Preserve source quantities, one decision/case branch per unit, held stock until lawful obligation closure, original consumed reservation untouched, replacement hold retained on parcel cancellation, payment authorization and package locks. No financial change/provider call.
9 Focused proof on same minimal existing data and controlled test sources where necessary: carrier cutoff, processing closed hours, original delivery date elapsed, stock pending, partial/mixed source, exact replay/uncertainty, queue reads stored new plan unchanged. No extra live test-order manufacture. Read-only current records until the specific safe task-owned mutation proof is issued; disclose permission scope precisely. No fake provider waybill or real money.
Handoff user sees: on an actual existing returned/replacement package a deliberate new schedule, next preparation/exit/handoff times, clear customer delivery promise. User doesn't need to populate a new dataset.

## Planned271 — actionable employee queue over existing facts
After270 reviewed (and only then):
Replace confusing overlapping primary filter rows with these job stages, based on existing server-owned next executable action:
- آماده‌سازی و بسته‌بندی
- آمادهٔ ارسال (parcel/tracking completion)
- تحویل به حامل (dispatch-ready handoff)
- پیگیری تحویل
- برگشتی‌ها (receipt/resend/replacement/Returns decision)
Time scope: Today default (includes overdue first), Future, All. Don't put "needs action"/"problematic" as competing job-stage filters; blockers are precise row reasons. Keep one row per physical unit and exact locator.
Each row explicitly presents next verb, parcel/order label, relevant due/start/deadline and blocker. Distinguish preparation START from departure deadline and carrier last handoff; never rename a preparation start as packing finish. No invented separate pick/pack duration when current policy has only total preparation allowance. Returning/delivery follow-up without SLA cannot acquire arbitrary hourly deadlines.
Current null-plan legacy rows remain honest exception work, not all hidden/treated completed. New resend plan makes due ordering meaningful. Same full-set counts/search/paging authority, one action owner, same shared detail surface. Five filters proposal is a design direction, refine exact mapping from source capabilities rather than infer execution permission from status or labels.
No new settings/dataset/business state engine. Existing Toast/MessageBox, RTL/mobile/readability retained.

## Recovery / stop
Current active269 only.270/271 NOT ISSUED; numbers reserved for planning, no worker launched. When269 result arrives independently inspect and either issue exact repair OR270, one at a time. No automatic tasks/messages/provider calls. One canonical tooba_alpha5432 SingleStore/store-alpha FE3000->Host5088, all keys/flags preserved. No new cleanup/restore/reset/stock write, no user method edit. Preserve prior cleanup audit corrections and unknowns, don't certify historic manual reset.
