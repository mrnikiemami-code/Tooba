# Shipping operational closure plan — 2026-11-09

Status: APPROVED PRODUCT DIRECTION; implementation NOT complete.
Owner: Architect. One Bridge task at a time. Worker PASS is not Architect/user acceptance.
Reviewed source base: 69bb2e2ab1e81e673a5119d740c94ff3b4949489.
This document supersedes conversational guesses about missing delivery durations. Durations EXIST; the gaps are their semantics/application and the missing dispatch plan.

## Read first after chat recovery
Read this plan, the CURRENT top of the three recovery files, then ONLY the active task and its result. Do not replay old tasks or issue all stages at once. After each result independently inspect affected code/evidence, accept or issue a bounded repair, then release the next stage.
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

IDs after 256 are reservations, not execution authorization. Narrow repair tasks may use 256-R1 etc. Only issue next stage after actual result and independent review. No endless broad proof loop.

## Environment, proof and deployment boundaries
Canonical source D:\\Users\\User\\source\\repos\\SarvNewVer, FE3000 -> Host5088, SingleStore/store-alpha/tooba_alpha. PIDs are historical, recheck.
No edition switch, new final DB/port/runtime, Store->Marketplace promotion, real Tipax, user-order/cart/payment/dispatch mutations.
Necessary additive local-dev migrations authorized within the active issued task only, after private backup, target/pending migration inventory and current MigrationRunner procedure; no reset/reseed/manual business SQL.
Preserve DataProtection and local CapturedPreview ON/7 dates, checked-in OFF.
Focused tests once, relational proof for new durable invariants; no screenshots. Live authorized DOM read-only, never shell200 as acceptance. Controlled fixture writes must not hit user orders.
TB185 paused/default-OFF; retain 238 unknown22001, 227 mobile note, 225-R1 lost local edits.
