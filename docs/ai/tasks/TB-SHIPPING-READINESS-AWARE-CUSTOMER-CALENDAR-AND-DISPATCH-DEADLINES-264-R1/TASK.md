PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264-R1
Parent-Task: TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: CLOSE_ZERO_DURATION_SELECTED_SLOT_AND_PROCESSING_READINESS_GAPS_WITH_REAL_AUTHORING_ACCEPTANCE_HANDOFF
Expected-Base-SHA: bba723b1d82d3bd5e4916adc9b69ce2c0e040460
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Bounded review correction; no loop
Read 264/ARCHITECT-REVIEW.md, original264 TASK, CURRENT recovery and mandatory repository architecture/Contracts/UI rules.
264 is PARTIAL, not accepted. Preserve its owned preparation/transfer fields, versioned evidence/read bridge, existing queue/Toast/MessageBox and physical identities. Fix ONLY concrete original-scope gaps A–D below in ONE execution;265 NOT ISSUED.
One worker/task, no delegation, no full-suite/base-parity/repeated-green/screenshots/concurrency stress. Run new failing cases then changed focused checks; only repeat after actual new code/failure. No unrelated code cleanup/framework.
Expected base permits legitimate issue/docs descendants; preserve all work, no force/reset/amend/stash/rebase.

## 2. A — explicit zero must survive the ENTIRE path
Source-confirmed:
Authoring allows PreparationMinutes=0/HandoffMinutes=0; forward calendar accepts0.
ResolvePreparationDeadlines rejects via preparationStart < transferDeparture; Order domain/model/migration repeats strict ordering. Thus prep0 + handoff30 (and0/0) can be offered but acceptance fails.
Keep explicit zero valid, never pretend absence means zero.
Align authority/domain/EF model/ADDITIVE correction migration with nonnegative durations and exact arithmetic: prepStart<=departure<=handoff deadline, equality when corresponding duration0. Preserve close-exclusive acceptance and correct "before/by" semantics; equality of conceptual boundaries does NOT authorize arrival at carrier close. Do not use epsilon to fake business duration or change zero to1.
New focused public acceptance/storage cases0/0,0/30,30/0,positive pair; partial/corrupt plan still refused. Old applied migration remains immutable; new additive correction through MigrationRunner after backup if needed. No manual business SQL/backfill.

## 3. B — chosen customer WINDOW/instant constrains backward plan
Source-confirmed:
StorefrontPlannedDispatchResolver receives ONLY receivingDate; ResolvePreparationDeadlines calls latest handoff for date then chooses carrier CLOSE. Customer chosen start/end/zone never constrain deadline.
Concrete failure: UTC carrier09–17, customer tomorrow10–11, transport0, prep60+transfer30; current plan says prep15:30/carrier17:00 AFTER customer appointment. Different zones also allow date-only mismatch.
Pass authoritative accepted customer date+window+zone through the existing selected-group seam into the SAME backward scheduling authority. Derive latest feasible handoff AND prep/departure deadlines whose forward result respects that actual receiving commitment, including0-day transit and cross-zone/day rollover. The scheduled latest start cannot be later than a customer commitment it is supposed to meet. Do not invent exact-hour guarantees for N-day transit, but even zero-day must not schedule handoff after the receiving window.
Use same semantics for offered slot feasibility, selection acceptance, checkout revalidation, evidence and operational queue. No browser arithmetic or second scheduler. Policy version new semantics where required; existing accepted v1/v2 readable and NOT recomputed/relabelled/backfilled. Current settings edits cannot rewrite accepted evidence.
Focused cases tomorrow10–11 vs carrier17 close, selected late slot, zero allowance, cross-zone day mismatch, positive transit and stale selection. Verify exact selected slot reaches public selection writer and stored deadline, not direct factory-only fixture. Do not claim close-exclusive deadline itself is a valid appointment.

## 4. C — physical readiness cannot silently count closed processing hours
264 original task required preparation not run through closed processing hours unless explicitly permitted.
Current source is now.AddMinutes(prep+transfer) with only customer/carrier weeks; documentation saying "elapsed" does not make a warehouse operate overnight. Example processing09–17, eligible23:00, prep120, carrier09–17 -> current arithmetic permits09:00 acceptance although goods cannot be ready until11:00 before transfer.
Implement minimal explicit owned processing-time semantics within the existing authoring/revision/captured timing policy, as original264 already authorized:
- real processing availability comes from governing existing source if any; prior review found none, so add the minimal independently editable processing week if needed, reuse existing weekly editor pattern and deliberate one-time copy; never silently use customer/carrier calendar as warehouse hours;
- count preparation working minutes inside that configured week; backward subtraction through SAME calendar; transfer allowance remains distinct and its elapsed/working semantics explicit, not double-counted;
- elapsed24/7 operation is allowed ONLY if explicitly configured and clearly labelled by the operator, not an undocumented default for every seller;
- all closed/no feasible readiness => precise localized unavailable, no infinite search. Bound search appropriately for long duration/sparse weeks.
- group readiness constrained by ALL required members, independent parcels keep own scope.
Absent new prep policy must NOT give a NEW commitment an instant-ready v1 fallback. Separate legacy stored-evidence compatibility from new selections: read/revalidate existing old evidence using its recorded policy; new incomplete physical timing/calendar -> clear configure/unavailable reason. Do not recompute existing promises or delete services/data.
Preserve existing currencies, geographic/language ownership, rates and money. Don't bring back old PreparationDays as a second preparation charge without actual governing-path evidence.
Focused closed-day/night case, copy independence/save, forward/backward consistency and explicit24/7 mode if supported.

## 5. D — missing connected user proof, not more pure-model tests
264 supplied only authoring-model test4/4, no actual save/reload/publish on a specific task draft, no exact method id; trial says "task method" without locator. Resolver+factory+DB fixture is useful but is NOT the issued actual storefront calendar/selection acceptance proof.
Deliver ONE concrete task-owned method on SAME canonical installation (minimal new task draft allowed, or reuse labelled task draft without user data):
- set/edit timing through actual authoring screen Save, real API PUT, GET/reload retains values; timing-only change recognized by all session dirty predicates;
- publish through owning API/UI then read published captured quote/calendar from actual storefront path, verify preparation and carrier settings affect offered dates;
- real selection/acceptance writer uses the actual selected window and persists plan. Minimal task-owned cart/group fixture through owning production entry allowed, no real payment/customer transaction/provider. If fully public checkout requires payment, use approved fixture acceptance seam and state EXACT boundary; don't call factory-injected plan a storefront checkout;
- query existing queue through actual API for the same accepted fixture, exact operation URL resolves its own deadline, legacy sibling remains unchanged;
- record method/draft/revision/cart/group/order fixture IDs, response status/body shape, runtime/build, exact review URLs and short operator steps. Read-only browser/DOM inspection sufficient, no screenshot deliverable.
Use existing Toast once on confirmed save/publish, field validation inline, existing MessageBox for blocking error; no technical persistent banners. Keep existing true tabs and one operational action owner.
If a real external obstacle prevents this connected proof, report precise blocker rather than label all PASS; don't bypass permissions/payment or fabricate user evidence.

## 6. Canonical environment/invariants
ONLY tooba_alpha @127.0.0.1:5432, FE3000 -> Host5088 SingleStore/store-alpha. No extra/disposable/test/Docker/15433 DB or edition/connection/port change. Minimal task-labelled production-path fixtures authorized; user/screenshot/published user methods read-only. No raw business SQL/manualDDL/reset/reseed/backfill/delete/global reclaimer. Additive migration only through repository MigrationRunner with private backup.
Preserve DataProtection/CapturedPreview localON7dates/checked-inOFF,255 parked/Tipax deferred. No live provider/fake labels. No financial/Returns integration or active-order reservation release; parcel cancel/return/retry/timeout retains stock, dispatch consumes once. Future265 approved Returns+owning line-obligation cancellation release remains NOT IMPLEMENTED.

## 7. Handoff
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264-R1/TASK.md, evidence TASK/RESULT/RECEIPTS/result-contract plus concise connected proof under docs/architecture/evidence/TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264-R1/.
Update AGENTS/START-HERE/recovery/263–265 plan:264 PARTIAL until reviewed264-R1,265 NOT ISSUED. Result exact Base/Code/Evidence/Merge SHA and HEAD==origin/main, changed paths, proof for A–D, actual authoring/selection/queue URLs, deployment and limits. No claim all complete/user acceptance.
One result then STOP_FOR_ARCHITECT; no further task proposed/auto execution.
automaticNextImplementationTask: NONE
END_TOOBA_TASK
