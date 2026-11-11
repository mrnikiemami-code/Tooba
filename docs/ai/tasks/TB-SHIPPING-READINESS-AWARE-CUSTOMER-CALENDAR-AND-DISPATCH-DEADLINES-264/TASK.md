PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264
Parent-Task: TB-SHIPPING-REPLACEMENT-RECOVERY-AND-OPERATOR-FEEDBACK-263
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: PREPARATION_AND_CARRIER_TRANSFER_READINESS_TO_FEASIBLE_CUSTOMER_DATES_AND_DURABLE_OPERATIONAL_DEADLINES
Expected-Base-SHA: 04c8099bbf7c26d781fc097c96a51ab3fe99b3f5
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. One complete, bounded user outcome
User orders near carrier closing time; current scheduling treats nowUTC as goods readiness, offering an impossible handoff and customer date. Deliver the WHOLE correction: editable owned timing inputs -> save/reload/publication -> actual storefront calendar -> acceptance/capture -> durable preparation/handoff deadlines -> existing operational queue/view. Do not finish with pure arithmetic/fixtures only or add a second queue.265 Returns integration remains PLANNED/NOT ISSUED.
Read CURRENT TOPs/recovery, SHIPPING-USER-FLOW-CLOSURE-263-265.md, mandatory repository architecture/Contracts/feature/design-system rules. One worker, one task, no delegation. Expected base permits legitimate issue/docs descendants; preserve ancestry and unrelated work; no force/reset/amend/stash/rebase.
263 source/evidence accepted within scope, user acceptance pending. Preserve occurrence projection, typed pending/refused/unknown, existing Toast/MessageBox, reservation and quantity rules.

## 2. Inspect and choose ONE owning timing model (within this implementation, not another discovery task)
Confirmed at issue:
- ShippingDispatchSchedulingAuthority.ResolveFirstHandoff accepts readinessUtc; its callers still assume now is readiness.
- ShippingRatePolicy/ShippingQuote ALREADY have PreparationDays separate from LeadDays. Do not add a second setting for the SAME duration.
- ShippingAuthoringMethodSettings and EditorSettingsWrite currently have only payment/calculation modes; authoring customer/carrier calendars exist. There is no preparation/transfer input in these inspected contracts. This does NOT prove no such source exists elsewhere.
Trace the ACTUAL canonical authoring/published captured storefront path and any per-offer/seller/product preparation/ready-at source + any processing work calendar. Record authority/precedence and units briefly in ownership-design.md. Reuse actual governing inputs via Contracts; don't treat an unused legacy field as a working authority. No cross-module DbContext/entity coupling.
Where missing, add the MINIMUM explicitly editable owned preparation/handoff policy to the existing authoring draft/editor/revision/publication flow. A practical hours/minutes allowance must represent physical preparation and travel to carrier; zero only when explicitly configured, never "missing means instantly ready". Existing preparation days, if applicable, count once, plus only distinct missing handling/transfer allowance. State whether time is elapsed or working time; do NOT silently run preparation through closed processing hours.
Use existing actual processing calendar if present. If none exists and working-time configuration is needed, a small explicit processing week belongs to the same owning policy, with deliberate one-time copy from another week allowed; never silently equate customer receiving hours/carrier acceptance hours to warehouse operating hours. Avoid a separate settings app/framework or confusing exclusive buttons. Document chosen business semantics so operator labels match them.
Use judgement to implement the minimal coherent policy without repeated permission/discovery pauses. If genuinely incompatible ownership/product rules prevent a correct choice, report precise blocker rather than fabricate defaults.

## 3. ONE forward/backward scheduling authority
Forward:
trusted order eligibility instant + authoritative preparation -> physically ready at origin -> transfer/handoff allowance -> first POSSIBLE carrier acceptance -> MAX transport days -> separate customer working dates/slots.
All using explicit trusted nowUTC and configured zones/calendars; frontend never computes business dates.
- 2..3 transit days means MAX3, never MIN2; preparation/transfer counted once, not transport double-added.
- Carrier window remains [open,close). Ready/arrives AT close misses it. Before open may wait until opening. Near close may miss even if the order itself is before close.
- Acceptance hours are availability, NOT an invented daily exact pickup appointment. If actual configured pickup/lead deadline exists respect it; don't label entire9–17 window as a17:00 appointment.
- Closed processing/carrier/customer days are independent; same-weekend assumptions forbidden. Transport working-day policy from256 remains explicit unless actual separate source exists; no fabricated holiday service.
- Zero-day transit still filters elapsed/impossible slots; cross-zone comparisons use instants.
- Multi-item group waits until ALL required members ready (max readiness for parallel work, no naive sum); separate work units never borrow sibling plan.
- Acceptance/revalidation uses fresh authoritative readiness and refuses expired/impossible selections. Start preparation clock from actual established eligibility policy, not browser timezone or stale cart render; disclose payment/confirmation semantics, don't change payment workflow.

Backward for customer's selected feasible receiving date:
derive latest feasible handoff window/instant AND latest time preparation must start, validated against the SAME forward rules. A close-exclusive deadline must not be claimed feasible AT close; store/display deadline semantics truthfully (e.g. "پیش از"). The operational carrier window alone is not sufficient: record latest dispatch/transfer departure if distinct.
Persist accepted readiness inputs and preparation/handoff deadlines with policy version/provenance via Order's existing accepted evidence seam; local business dates/times/zones/instants lossless. PolicyVersion change where semantics change, old versions readable.
Earliest ready time and latest preparation start are different facts; don't display earliest ready as latest start. New plans survive method/calendar edits without recomputation. Historical orders retain old evidence/no new prep plan; never retroactively relabel as preparation complete.
No actual Dispatched/Delivered timestamps invented, no historical promise/money rewrite.

## 4. Operator and customer experience
Existing method authoring: concise labels "زمان آماده‌سازی" and distinct transfer allowance if needed; practical hours/minutes, units clear. Reuse existing days source if applicable. Copy is one-time, fields remain independent.
Test carrier/prep-only changes through actual page save button -> production API -> readback/reload. Wire every dirty predicate, editor session, payload, decode, reset, conflict retention and publish/captured read path (256-R2 failure must not recur).
Retain true underlined tabs and current styling. Toast once for confirmed save/publish; field validation at field; existing MessageBox for blocking error. No persistent technical/success banners, no raw enums/policy/source ids.
Storefront shows only feasible customer receiving dates; no admin-only deadlines leaked to buyer. No money/fee change.
Shared finance-free operations view displays concise separate facts: "شروع آماده‌سازی تا ..." / "تحویل به حامل ..." / "تحویل مشتری ...". Reuse existing component and exact work-unit locator.
Existing queue surfaces preparation due BEFORE handoff due, preserving existing dispatch filters/count semantics or adding explicitly separate prep filters/columns if needed. Do not silently redefine dispatch_due to mean preparation. Counts full filtered set, stable physical row identities, own-group plans, no child duplicates in common package. Readiness statuses stay truthful; a tracked Created parcel is physical work. No bulk dispatch or auto-mutation.
User must be able to configure and see this result on canonical UI after deployment, not only read a test report.

## 5. Focused acceptance, complete connected paths
Use explicit injected clocks; never change system clock.
A Same-day processing week9–17; carrier9–17; TOTAL required preparation+transfer2h: eligible14:00 -> ready/arrival16:00 -> today's carrier feasible;16:00 ->18:00 unavailable -> next accepting date.16:59 also misses. Exactly17:00 misses. Carrier Wednesday closes12 while customer receiving works to17; processing/carrier closures and an after-hours order roll correctly.
B MAX3 after a feasible Monday handoff offers Thursday earliest under Mon–Fri transport policy; missed Monday handoff moves earliest accordingly. Customer chosen later feasible date has latest prep start/handoff whose forward arrival<=chosen date; next later candidate violates it. Zero transit and cross-zone case.
C Two-member common group constrained by slower member; separate units own their plan; existing legacy null/old plan still readable unchanged.
D REAL authoring page save/publish/readback on task-owned method ONLY (no user method write), including a timing-only patch and reload. Actual storefront calendar endpoint/composition and actual public selection/acceptance writer persist new evidence, owning read bridge returns same values; change method afterward -> accepted order unchanged. Do not inject invented plan directly into database and call that checkout proof.
E Minimal labelled accepted fixture through real owning production paths on SAME database may be created to prove Order plan read -> actual queue query -> real shared operations route. No real customer payment/provider; if whole checkout requires payment, use existing approved internal fixture acceptance path and disclose exact boundary; never claim real paid checkout. Positive new plan must be visible for user trial with URL/IDs.
F Mounted relevant UI + targeted authority/roundtrip tests; retain pertinent263 and physical-work queue regressions. No full-module/base parity/green repeats/stress/screenshots. Checks once unless actual new failure/change justifies rerun. FE typecheck/build + affected backend build/publish.
Necessary additive migrations allowed: private canonical backup, repository MigrationRunner only; old rows remain readable, no backfill/reseed/manualDDL. No schema change merely for test convenience.

## 6. Environment and invariants
ONLY canonical tooba_alpha @127.0.0.1:5432, FE3000 -> Host5088, SingleStore/store-alpha. No extra/disposable/test/Docker DB/15433, no edition/connection/port switch. Task-labelled minimal production-path fixtures authorized in SAME DB; user and screenshot data read-only. No global expiry/reclamation job against shared DB, no raw business SQL/reset/reseed/delete.
Preserve DataProtection/CapturedPreview localON7dates/checked-inOFF,255 parked, Tipax deferred. No provider calls/fake waybills.
No financial/Returns implementation. Active-order unconsumed replacement holds retained through parcel cancel/return/retry/timeout; dispatch consumes once. Future approved Returns+owning line obligation cancellation release belongs265 only; case creation alone isn't release.

## 7. Handoff and stop
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264/TASK.md; evidence TASK/RESULT/RECEIPTS/result-contract/ownership-design + concise case receipts under docs/architecture/evidence/TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264/.
Update AGENTS/START-HERE/recovery/263–265 plan:263 accepted within scope;264 worker result pending Architect review;265 NOT ISSUED. Never claim all complete.
Result exact Base/Code/Evidence/Merge SHA and HEAD==origin/main; owning source/units/semantics, forward/backward examples, actual save/publish/calendar/acceptance/queue proof boundaries; minimal fixture IDs; migrations/runtime/build; concrete review URLs and short user trial. Clearly label any unproven live checkout boundary.
One task/result then STOP_FOR_ARCHITECT. No automated next task, no proposal for extra repair without concrete defect.
automaticNextImplementationTask: NONE
END_TOOBA_TASK
