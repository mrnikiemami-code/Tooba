# CURRENT — 256-R2 PASS (worker): the calendar Save now writes; next is the five-item ledger — 2026-11-10

Read docs/architecture/evidence/TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R2/{RESULT.md,RECEIPTS.md,dom/*}.
256-R2 repairs the only user-reported blocker on the accepted two-tab form: «ذخیره نمی کنه». Measured root cause — the frontend session's serializable-patch predicate `isDirty()` omitted the TB-256 `carrierSchedule`, so a carrier-only patch was classified `no-changes` and the click sent ZERO PUT while Save looked enabled; a reload restored the old calendar. The repair counts the carrier calendar in that predicate (matching `intentHasEdits`); nothing else changed.
Proven on canonical FE3000 → Host5088 / SingleStore / store-alpha / `tooba_alpha` with FOUR real Save presses and reloads on the task-owned draft `01a1beed-…` (revisions 3→8): carrier-only, customer-only retaining the carrier week, one-time copy + independent carrier edit, and the final restore. User draft `01a17d0d-…` stayed read-only at revision 2. New save suite 4/4, session 23/23, carrier 10/10, tabs 3/3, mount 6/6, typecheck 0, build 0, FE restarted on BUILD_ID `uXwhPh0ZnBqFWcpxujHPd`; no migration, no backend change.
USER DIRECTION: tabs accepted; sequential continuation authorized — close the FIVE remaining issues stage by stage with one Architect review per result; no separate appearance approval after every correction, and no worker self-start.
NEXT (only after Architect reviews this result): the five-item ledger — tracking-generator uniqueness, correct remaining quantity, non-delivery/return/resend, shared-group common-or-separate parcels, due workqueue/shared operational order view. Planned 257 (first two), 258, 259, 260 remain NOT ISSUED. 255 parked; Tipax deferred.
HARD RULE: one database only — `tooba_alpha` on the canonical FE3000/Host5088 instance. No disposable/test/parallel database, no port 15433, no Docker PostgreSQL, in this task or any later one.

# SUPERSEDED — 256-R2 issued (completed by the worker result above) — 2026-11-09

User accepted timing-tab appearance but reports SAVE FAILS. Active issued task: TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R2; read its TASK.md. Expected code/evidence base 9543b149e; newer issue/docs descendants are legitimate. Issued is not claimed/completed.
LATEST USER OVERRIDE: continue one task at a time, wait for actual worker result and Architect review, then issue the next; do not pause for user after every defect. Stop for meaningful user handoff after the FIVE remaining defects, following save prerequisite.
Ledger: save prerequisite 256-R2 IN PROGRESS/ISSUED (not fixed); 1 tracking uniqueness PENDING; 2 remaining quantity PENDING (both planned257); 3 non-delivery/return/resend PENDING258; 4 common-or-independent parcels in shared group PENDING259; 5 due queue/shared operational view PENDING260. 257–260 NOT ISSUED yet. Worker must STOP after its single received task.
Only tooba_alpha, FE3000/Host5088; NO extra DB including disposable tests. Real UI SAVE proof on existing task-owned draft 01a1beed-d7df-7000-bfc8-0185ea61afe3 authorized, no user's service save/publication or order mutation. Preserve approved tabs and concise copy. 255 parked/Tipax deferred.
The latest instruction supersedes older user-test-every-fix pauses below; do not lose this through chat recovery. Never claim save works until actual PUT + fresh GET + reload prove it.

# SUPERSEDED — 256-R1 PASS (worker); user trial then Architect review — 2026-11-09

Read docs/architecture/evidence/TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R1/{RESULT.md,RECEIPTS.md,dom/*}.
256-R1 closes the four open items from the 256 Architect PARTIAL: real underlined tabs with ARIA + RTL roving keyboard and presentation-only switching; concise copy; new commitments fail closed without a configured carrier acceptance calendar while stored legacy null-plan evidence stays readable; latest handoff accepted when its conservative arrival is on or before the chosen receiving date; and «موعد ارسال» rendered as its own read-only operational row separated from «تحویل مشتری».
Runtime unchanged (FE3000 BUILD_ID `pz_oynTTa4nZUOPrjSPOm` → Host5088 SingleStore / store-alpha / `tooba_alpha`), no new migration, user draft revision 2 untouched, task draft revision 3.
NEXT: user trial on `/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit`, then Architect review. Cutoff examples are code/fixture-proven only (that method has no configured carrier week) — do not shift the clock. 257–260 remain NOT ISSUED; no automatic next task; 255 parked; Tipax deferred.
HARD RULE: one database only — `tooba_alpha` on the canonical FE3000/Host5088 instance. No disposable/test/parallel database, no port 15433, no Docker PostgreSQL, in this task or any later one.

# SUPERSEDED — 256 USER TRIAL; Architect PARTIAL; no next task — 2026-11-09

Read docs/architecture/evidence/TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256/ARCHITECT-REVIEW.md first, then the closure plan.
256 code/evidence c58e87e5e is deployed per worker and authoring UI is ready for user trial, NOT full scheduling acceptance. Open: absent carrier calendar still allows new null-plan acceptance; nowUTC is not proof of preparation readiness; exact-arrival-only backward rule needs review; no visible operational dispatch row/canonical plan.
User requires a TEST OPPORTUNITY after EACH correction. Await USER FEEDBACK before issuing even a repair task. 257–260 remain NOT ISSUED; no automatic continuation from worker PASS.
User prohibits extra/disposable databases. Worker reported a 15433/order_tb231 relational run contrary to latest direction; do not repeat, cleanup not verified. Canonical tooba_alpha and FE3000/Host5088 only. No automatic user-order mutation.
Review existing draft /admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit (carrier unconfigured); task-owned draft 01a1beed-d7df-7000-bfc8-0185ea61afe3. Trial starts with tabs/copy/independent edit; old published service does not gain cutoff until valid configured publication. Preserve prior history/255 parked/Tipax deferred.

# Shipping operational closure plan — 2026-11-09

Status: APPROVED PRODUCT DIRECTION; implementation NOT complete.
Owner: Architect. One Bridge task at a time. Worker PASS is not Architect/user acceptance.
Reviewed source base: 69bb2e2ab1e81e673a5119d740c94ff3b4949489.
This document supersedes conversational guesses about missing delivery durations. Durations EXIST; the gaps are their semantics/application and the missing dispatch plan.

## Read first after chat recovery
Read this plan, the CURRENT top of the three recovery files, then ONLY the active task and its result. Do not replay old tasks or issue all stages at once. After each result independently inspect affected code/evidence, hand the visible correction to the user, and WAIT for user feedback before issuing a repair or next stage.
255 is BLOCKED and now PARKED by user. No edition/database switch was authorized. Do not spend the next task trying to create the Marketplace test order.

## Existing work — preserve, do not rebuild
- Accepted shipping groups retain method/service/option, owner, receiving date/window/timezone and money.
- Existing order operations: ready -> processing -> packing -> parcel creation -> tracking -> dispatch -> delivery. Partial quantities supported.
- 254-R1 operational cards/dialog and one display/action owner per parcel exist. Bounded technical proof is not user visual acceptance.
- 253 Marketplace operational grouping authority and durable occurrence/replay safeguards exist. Common group workflow currently forces a common consolidated package.
- Shipping authoring already stores MIN/MAX delivery days per region and customer receiving weekly hours.
- ShippingRatePolicy has separate PreparationDays/LeadDays; OperationalQuote accepts RequestedPickupAtUtc to evaluate carrier operating hours. These are NOT proof of an accepted per-order dispatch deadline.
- Product weight whole grams and dimensions cm exist. Never sum dimensions to invent parcel dimensions.
- Preserve 249/253 complete immutable request envelope, prebound occurrence, no blind retry/remint, exact parcel ownership and actor authority.
- Provider connection/live Tipax verification remains explicitly deferred.

## Defect/evidence ledger
| Item | Evidence/status | Required correction |
|---|---|---|
| Calendar uses MinDeliveryDays | independently source-verified in CapturedGroupCompositionCore.BuildReceiving | user now requires MAX: 2–3 -> 3 working days |
| Cutoff and planned dispatch | calculator explicitly omits cutoff; receiving selection stored without planned dispatch | separate carrier acceptance schedule and persist a feasible dispatch plan |
| Automatic tracking repeats | user report; root cause NOT yet investigated | server-side atomic uniqueness, no repeat, distinguish internal ref from real provider tracking |
| Remaining packed over-reported | source/worker diagnosis: subtracts Created allocations but misses shipped | one quantity authority subtracts shipped + open allocation, consistent projection/modal/command |
| Post-dispatch failure/return absent | source verified cancel only Created; dispatched only exposes deliver | non-delivery -> return journey -> confirmed return -> controlled resend; preserve dispatch history |
| Shared vs separate packages | source verified workflow always CreateConsolidatedPackage; UI hides independent create for multi-member | one operational group supports common or independent physical parcels |
| Work queue is fulfillment/seller summary | source verified row keyed FulfillmentId/SellerOrderId, CreatedAt/UpdatedAt, status filters | operational due queues reuse existing order operations, no duplicate lifecycle |
| Marketplace real checkout test | 255 BLOCKED; current runtime SingleStore | pending; no claim of real acceptance, no owner relabel/environment switch |

## Locked approved product rules
1. Three different concepts: customer receiving window; carrier package acceptance hours; actual dispatch/delivery timestamps.
2. Add carrier acceptance weekly schedule on the SAME owner-scoped shipping authoring document. Existing customer schedule remains independent.
3. UI: schedule tabs «تحویل به مشتری» and «پذیرش حامل» (or equally clear short labels). Button «کپی از ساعات تحویل مشتری» performs a ONE-TIME copy into editable carrier hours, no hidden perpetual link.
4. Carrier may accept Wednesday 09:00–12:00 while customer receiving hours stay 09:00–17:00.
5. Use maximum configured duration for new receiving availability (2–3 -> 3). Determine first feasible carrier handoff from readiness + carrier schedule. At closing time or later no same-day handoff; before opening can target that day's opening; closed day moves to next valid acceptance.
6. Count transport working days AFTER feasible handoff day (same-day allowed only for explicit zero-day duration with feasible customer slot). Never count an already-missed acceptance day. Keep transport-day and customer-slot calendars explicit; reuse an existing authoritative transport calendar if present, otherwise record and implement the bounded calendar rule explicitly rather than silently treating customer receiving hours as carrier acceptance.
7. Derive latest feasible handoff for chosen customer date using the SAME forward feasibility authority, bounded search; do not naively subtract calendar days. Retain timezone and schedule/duration provenance. Do not promise an hour the data cannot guarantee.
8. Audit preparation semantics: captured duration had been documented as including preparation; DO NOT add legacy preparation again. New carrier-transport semantics must be explicit/versioned. Missing readiness/calendar evidence -> honest unavailable/needs-configuration, never inferred SLA.
9. Existing accepted order commitments/money/history immutable; new policy applies to new acceptance. Old orders without a dispatch plan remain unspecified, not silently backfilled.
10. Responsibility is NOT physical origin. Cross-seller grouping only Marketplace + same proven origin/destination/method/service/option/customer day/hour/timezone and compatible transport. Store/Seller stay separate.
11. Operational delivery group is NOT a physical package. Same group can have one common package or multiple independently tracked parcels; accepted financial groups/fees stay unchanged.
12. Failed delivery is NOT customer return after delivery, order cancellation or automatic refund/restock. Physical return confirmation precedes release of resend quantity.
13. Dispatch correction when never physically left is a separate audited case; never erase history or permit universal cancel-after-dispatch.
14. Fulfillments list should open the SAME operational order section without irrelevant financial panels, focused on exact group/parcel. Operational collect-on-delivery money remains where needed. No parallel forms/state machine.
15. Languages from configured registry; preserve RTL, wrapped titles, 44px targets with ordinary glyph sizes; no technical clutter or duplicate parcel action owner.

## Sequential task roadmap (only first issued now)
| Stage | Planned task | Scope | Exit requirement |
|---|---|---|---|
| 1 | TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256 | separate acceptance schedule, MAX calendar, cutoff, durable new dispatch plan | focused model/API/persistence/mounted UI + real authorized read-only canonical proof/deploy |
| 2 | TB-SHIPPING-TRACKING-UNIQUENESS-AND-QUANTITY-AUTHORITY-257 (NOT ISSUED) | diagnose tracking generator/unique scope; fix quantity/readiness everywhere | collision/concurrency/retry + mixed partial quantities; no empty actionable modal |
| 3 | TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258 (NOT ISSUED) | reasons, return state/receipt, quantity-safe resend, audited dispatch correction if supported | real lifecycle/persistence proof; no fake delivered/cancel/refund |
| 4 | TB-SHIPPING-OPERATIONAL-GROUP-PACKING-CHOICE-259 (NOT ISSUED) | optional consolidation vs independent parcels within existing compatible group | positive Marketplace controlled fixture both paths, retry/concurrency; preserve envelope identity |
| 5 | TB-SHIPPING-DUE-WORKQUEUE-AND-OPERATIONAL-ORDER-VIEW-260 (NOT ISSUED) | due today/overdue/status queue, exact locator, shared operational view | accurate filters/time boundaries + no duplicate actions/UI; user handoff |

Stage 1 status (2026-11-09): `TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256` executed — worker **PASS within scope** (base `de4924373`, code `fe5eb2f9b`), NOT Architect/Visual ACCEPT. Carrier acceptance week is independent from the customer week, receiving feasibility uses MAX with a half-open carrier cutoff, and new accepted groups persist a durable planned dispatch (legacy rows stay explicit null). Evidence: `docs/architecture/evidence/TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256/{RESULT.md,RECEIPTS.md}`. Stages 2–5 remain NOT ISSUED; 255 parked.

IDs after 256 are reservations, not execution authorization. Narrow repair tasks may use 256-R1 etc. Only issue next stage after actual result and independent review. No endless broad proof loop.

## Environment, proof and deployment boundaries
Canonical source D:\\Users\\User\\source\\repos\\SarvNewVer, FE3000 -> Host5088, SingleStore/store-alpha/tooba_alpha. PIDs are historical, recheck.
No edition switch, new final DB/port/runtime, Store->Marketplace promotion, real Tipax, user-order/cart/payment/dispatch mutations.
Necessary additive local-dev migrations authorized within the active issued task only, after private backup, target/pending migration inventory and current MigrationRunner procedure; no reset/reseed/manual business SQL.
Preserve DataProtection and local CapturedPreview ON/7 dates, checked-in OFF.
Focused tests once, relational proof for new durable invariants; no screenshots. Live authorized DOM read-only, never shell200 as acceptance. Controlled fixture writes must not hit user orders.
TB185 paused/default-OFF; retain 238 unknown22001, 227 mobile note, 225-R1 lost local edits.
