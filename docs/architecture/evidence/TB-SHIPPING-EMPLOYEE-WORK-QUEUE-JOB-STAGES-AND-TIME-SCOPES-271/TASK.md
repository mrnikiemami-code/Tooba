PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271
Parent-Task: TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R4
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: c501fe9bcdb2c42a26d05e23d44af288931612ed
Mode: EMPLOYEE_JOB_QUEUE_OVER_EXISTING_AUTHORITATIVE_WORK_UNITS
Track: SHIPPING_OPERATOR_WORK_QUEUE
Title: Employee queue showing the next job, exact parcel and relevant operational time
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

USER GOAL / ONE TASK
The employee opens the shipping work queue and immediately understands WHAT to do, on WHICH physical work unit, and WHEN. Replace confusing overlapping status/time quick-filter rows with five meaningful job stages and an independent time scope.
Use CURRENT existing business facts and records. This is a read-model/query + existing queue UI task; no new lifecycle, scheduling policy, business write or demonstration dataset.
ONE worker / ONE active task. No delegation/subagents, concurrent task, historical-agent resume or automatic follow-up. One complete final Result then IDLE/STOP.
NO screenshot request/capture/generation/deliverable. No polling or automatic refresh/retry loop, broad suite/baseline/stash campaign or speculative cleanup. Run named affected checks once after final code; rerun only an affected failure after a concrete correction.

ARCHITECT CHECKPOINT — 270-R4
Decision: ACCEPT WITHIN THE ISSUED FOUR-DEFECT REPAIR SOURCE/EVIDENCE SCOPE at main c501fe9bcdb2c42a26d05e23d44af288931612ed.
Reviewed changed source: modal/attempt pre-send unresolved persistence and conservative legacy read; close/unmount cleanup epoch and exact occurrence/body compare-clear/update; direct new-plan command gates after authorized stored replay; exact source+occurrence resend and source-decision replacement resolution without only-child fallback. Read focused first-send/lifecycle evidence and source comparison from 1ff02ab8a18c20999b92791f2b51520726bfa2d1.
28 command /13 resolver /14 modal /75 panel and builds/runtime observations are WORKER evidence, not independent Architect runtime execution. No new broad test replay required.
R1 remains accepted within its captured-input seam. Earlier R2/R3 partial findings are addressed for this bounded continuation through R4; do not rewrite those historical reviews as originally accepted. Two-stage physical allocation -> own confirmed plan remains the architecture. Preserve exact first-send uncertainty, frozen request, actor/history replay and dispatch guards.
NOT claimed: user visual/functional acceptance, actual live eligible-parcel click-through, true two-writer race, whole-shipping/provider/financial/consent acceptance. No eligible current unplanned linked parcel existed at last check; do not manufacture one.
R2 pre-apply backup was absent and remains an audit deviation; its post-apply dump is not retroactive protection. No new schema needed here.
R4 began with same-scope uncommitted edits and disclosed them; do not infer a clean historical starting tree or erase provenance. Persist this checkpoint at R4/ARCHITECT-REVIEW.md with these limits.

RECOVERY / MANDATORY READS
Fetch main, inspect safe worktree and Expected-Base-SHA existence/ancestry to origin/main. Compatible docs-only advancement may be integrated and declared; unsafe code divergence/missing expected object -> RECOVERY_CONFLICT. No force/amend/reset/rebase/stash or unrelated scratch changes.
Read current AGENTS/START-HERE, BRIDGE-WAKE-V1 protocol/controller, shipping 270/271 planning note (old 269/270 pending pointers are historical), backend feature skill and frontend feature skill/FRONTEND-ARCHITECTURE-STANDARD.
Read applicable Shipping Admin/Storefront UX decision TEXT; user forbids screenshot/image-review work. Use configured DB language registry, existing RTL/LTR, design-system grid and Toast/MessageBox conventions. No module audit/recertification, deferred findings register or broad source sweep.

CURRENT SOURCE / REUSE
1 Fulfillment.Application/WorkQueue/Models/AdminFulfillmentWorkQueueModels.cs:
AdminFulfillmentWorkQueueRow already carries WorkUnitKey/Kind/State, exact group/parcel/package locators, scope quantities, AvailableActionCodes, NextStepCode/Label/Blocker, preparation/departure/handoff instants and accepted zone. Preserve physical identity.
2 Fulfillment.Infrastructure/Queries/AdminFulfillmentWorkUnitProjection.cs:
authoritative physical rows: pending accepted-group work, one existing parcel, one active parent package with locked children suppressed, or history-only fulfillment. OwnPlanByShipmentId and binding rules protect linked attempts; no original-plan/sibling lending.
3 Fulfillment.Application/WorkQueue/Models/AdminFulfillmentDispatchTiming.cs:
existing scheduling/timing facts and AdminFulfillmentNextStep.Resolve. Progression currently lists status/operation helpers and tracking correction; a correction is not necessarily the next employee job. plan_shipment_dispatch must be represented when it is the actual outstanding server-offered job, without authorizing it from UI.
4 Fulfillment.Infrastructure/Queries/AdminFulfillmentWorkQueueQueryEngine.cs:
QueryAsync/CountAsync/ClassifyAsync currently load complete filtered units, build rows, apply timing filter/sort/page. Legacy status/queue filters can prefilter seller-level units; do NOT implement new job stages by that shortcut because sibling physical rows differ.
5 Frontend app/admin/admin-fulfillment-work-queue-screen.tsx currently renders TWO competing quick-filter rows from FULFILLMENT_DISPATCH_FILTERS and FULFILLMENT_QUEUE_FILTERS, one queueFilter state/URL filter, columns and AppDataGrid rowHeight64.
6 Frontend app/fulfillment/fulfillment-api.ts and fulfillment-work-queue-dispatch.ts map rows/counts/filter tokens and existing Persian/date format. Route /fa/admin/fulfillments and /admin/fulfillments share this screen. Existing operational detail remains the single action owner.

DESIGN / IMPLEMENT A — ONE JOB STAGE PER ACTIVE PHYSICAL ROW
Add ONE small pure SERVER read-model classifier over the existing physical row + relevant owning capability/source facts. No second lifecycle/status engine, Shipping calculation, frontend date arithmetic or DB state mutation.
Record exact mapping in work-stage-mapping.md BEFORE code, using actual source action semantics. A status label or a Persian string is never execution authority. Reuse existing server-owned capability/quantity/binding rules and actual command guards.
Five primary job categories (stable tokens + localized labels):

آماده‌سازی و بسته‌بندی: existing processing/packing work on exact remaining group scope.
آمادهٔ ارسال: allocating a parcel from packed work, confirming linked parcel's own plan, recording required tracking or completing existing parcel preparation. Name must not imply dispatch is already authorized.
تحویل به حامل: all owning prerequisites present and actual server dispatch/handoff operation is offered, including parent package action.
پیگیری تحویل: outstanding dispatched/in-transit delivery follow-up through existing operations; tracking correction alone must not outrank the real job.
برگشتی‌ها: actual return transit/receipt and remaining source-owned resend/damaged/Returns decision work. A historical returned source already fully allocated/settled is NOT active merely because its status is Returned.
Precedence follows actual NEXT work: an active newly Created resend/replacement is ready-for-send/planning work, not forever a return row because of lineage. Its source return may separately own distinct remaining received quantities; do not duplicate the same unit/action.
One stage per ACTIVE work-unit row, not five overlapping status lists. Provide All stages plus these five. Completed/cancelled/no-obligation history is not employee active work; keep it reachable via a quiet explicit history view using existing semantics, outside the five competing work stages.
A blocked active unit remains in its honest applicable job stage with a precise server reason, even when no executable action currently exists (legacy missing capture, no own plan, payment/source/hold/package prerequisite). Do not hide it or invent permission. Terminal history is distinct from blocked active work.
Reuse NextStepCode/Label/Blocker where correct. Extend their projection narrowly so plan_shipment_dispatch and package tracking/dispatch are meaningful next jobs; do not use a generic correction action as primary progress. Existing detailed action permissions/handlers are unchanged.

B — INDEPENDENT TIME SCOPE, STAGE-RELEVANT TIMING
Primary time scope is ONLY امروز (default), آینده, همه. Today includes overdue work first. Do not keep "needs action", "problem", "preparation due", "dispatch today" etc as extra competing primary filters.
Use injected SERVER now, existing accepted plan and existing timezone authorities. No local browser today, hardcoded Tehran/FA-EN assumptions, new calendar engine or per-read current-settings schedule recomputation.
Time attaches to the CURRENT next job:

Preparation: existing PreparationStartUtc is latest time to START, not a scheduled packing-finish time. Display "آخرین مهلت شروع آماده‌سازی" where that is the fact.
Ready-for-send and handoff: use only relevant persisted preparation/departure/handoff fact with an explicitly named meaning, preserving distinctions and actual physical state. No invented duration for pick/pack or "finish" inferred from a start.
Carrier handoff: distinguish departure deadline from acceptance close; close is half-open (at cutoff already late), not a booked carrier appointment.
Customer receiving promise is separate, never used as warehouse task due time.
Return transit and delivery follow-up without a stored SLA get NO invented hourly deadline. Label as ongoing follow-up/without a fixed deadline; don't borrow original expired handoff or use UpdatedAt to fabricate urgency.
Unknown/absent plan active work MUST remain visible in default Today through an explicitly labelled undated/exception group or note ("موعد مشخص نشده") with its own truthful count, not silently treated overdue or completed. Ongoing no-SLA follow-up may appear as current undated work, clearly labelled, never a fabricated due-today promise.
Future contains only active work whose RELEVANT known job time is a future day in its authoritative zone; tomorrow's handoff may still require preparation today. Test this exact distinction.
All spans current+future+undated active work; history only through explicit separate history view. Invalid/missing zones keep truthful unavailable markers; never browser-zone fallback.
Sort default overdue first, then relevant today due/start values, then explicit undated current work in deterministic order; future ordered by meaningful relevant stored time. Add stable WorkUnitKey tie-break. Keep explicit user sort supported and truthful.

C — FILTER / COUNT / PAGING AUTHORITY
Add narrow stage/time query tokens and additive read fields/metadata to existing query route, following grid validation/Result conventions. Server computes rows/filtering/counts from ONE consistent set of physical work units BEFORE paging. Frontend only renders supplied fields/counters.
Do not filter a current page then tally it, or filter whole seller status before projecting exact parcels/groups/packages. Preserve search, column filters and advanced-filter semantics; maintain actor/seller scope and existing exact plan binding.
Define badge semantics explicitly: stage counts cover whole active set within selected time scope after search/column constraints, before selected stage; time counts cover selected stage after the same constraints, before selected time. Overall total and undated count are explicit. History excluded from active totals. Show zero badges honestly.
Compute count/page from same classified rows/server now per query response where practical, reusing existing query composition. No duplicated independent timing calculations, per-row API/DB fetches, page-local fallback or second query authority. If existing endpoint separately calls CountAsync, refactor only the narrow reuse/snapshot seam necessary rather than inventing an engine.
One package row owns locked members; group remaining work and existing parcel rows retain disjoint physical scopes; no sibling/count inflation. Search must not make same-package member appear again as another action owner.
Missing metadata/API error is unknown/error, not six zeros with a successful-looking empty queue. Use existing bounded retry UI; no mock fallback or automatic polling.
Old URL filter tokens must not silently combine with hidden new stage/time filters. Normalize supported legacy links deliberately to a documented meaningful new selection or preserve an explicitly labelled legacy/history view; unknown tokens get safe clear state. Do not keep two competing authorities. Preserve back-to-queue exact state.

D — EXISTING QUEUE UI, CLEAR DAILY WORK
Keep route and shared AppDataGrid. Replace the two old overlapping quick-tab rows with one job-stage strip (All+five) and a clearly secondary independent Today/Future/All time control. Today default on a clean entry; restore valid explicit URL state and browser back/forward consistently.
Each visible row prominently shows:
1 exact human work-unit/order identity (parcel/group/parent package, stable whole-group ordinal where available; no raw GUID as primary label);
2 next VERB or honest blocker;
3 relevant job time with its precise label, zone and Persian/Jalali formatting;
4 secondary customer promise when stored and useful;
5 ONE existing "عملیات ارسال" link targeting exact group/parcel/package, carrying current queue stage/time/search state for return.
Do not add quick mutation buttons that bypass existing review/retained envelope/authorization; the queue remains a read/locate surface for actions already owned by the shared detail. Existing compatible bulk actions remain protected and use their existing guards.
Prefer a concise useful default column set in the existing grid, preserving optional existing columns/filter/sort settings where possible. Do not create another board/table framework or redesign detail page.
Keep 64px row/readability, no microtext as the only source of next action or deadline, >=44px new touch controls, cell/title consistency and scoped grid horizontal scroll at390. No document overflow. Non-actionable reason must remain discoverable via readable cell/tooltip, not color only.
Use existing operational date helpers: DateOnly day stable, instants in accepted zone, LTR clock windows and one human zone label per meaningful block. Don't reintroduce raw ISO/IANA ids, prices on the finance-free shipping view or hardcoded locale registry.
Refresh existing read models on explicit user activity only; no timers, continuous re-fetch or clearing retained planning attempts when stage/time changes. Preserve all 268 tabs/lineage and 270-R4 uncertainty/confirmation workflow.

PROTECTED SCOPE / NO DATA WORK
No change to payment, hold retention, allocation/consumption, original accepted evidence, resend/replacement creation, plan confirmation/replay, carrier/provider, Returns/refund/quantity rules, schedule settings or policy. Stage/time is a read projection, never new authorization.
No schema/migration/DDL/manual business SQL, backup/restore/reset/reseed/delete/cleanup or new live demo order/parcel/plan/hold/settings. Current user records and shipping draft read-only. Completed trial history not reset to obtain an active queue.
ONE canonical tooba_alpha5432 SingleStore/store-alpha FE3000->Host5088; no new/disposable DB/Docker/15432/15433, edition/port/connection/appsettings/key/CapturedPreview change. 255 parked, Tipax deferred.
No new endpoint family unless existing grid seam truly needs a narrow additive metadata response; no generic reporting platform or infrastructure task.

FOCUSED PROOF ONCE
Use a compact named new classifier/query test suite plus directly affected API mapping/mounted queue tests. No whole Order/Fulfillment/Shipping/Composition suite or old disposable-Postgres matrix.
Backend cases:

five actual stage mappings; tracked vs untracked/planned vs unplanned Created; healthy resend/replacement/source remaining return; package owns all members; completed returned source no active obligation -> history;
planning required overrides misleading tracking-correction progress, while capabilities remain identical;
preparation today + handoff tomorrow -> Today for preparation; overdue ordering; cutoff equality; independent zones/date boundary; ongoing delivery/return with NO SLA stays undated; unknown capture visible Today; future known timing only;
mixed seller group with one terminal sibling/one active parcel: exact rows/stages/counts; package no duplicated children; no plan/sibling lending;
whole-set counts correct beyond page1 with search/column/advanced filters, stage/time dimensions, stable pagination and explicit sorting; history/legacy URL handling consistent.
Frontend cases:
two independent controls serialize correct tokens, Today default, deep-link/back-to-queue/browser history restores state without stale hidden legacy filter;
row renders exact next verb/meaningful deadline/blocker and exact locator; counts from metadata, not current rows; unknown counts don't masquerade as zeros;
request ordering fence prevents an older stage/time response from overwriting new rows/counters; no background loop or mutation on filter changes;
current read-only history stays navigable, planning retained envelopes/tab state are not cleared by queue controls.
Use controlled owning inputs/tests, not new live fixture data. Existing database read-only for query proof; any truly required relational writes must be task-owned and rolled back without changing user rows. If existing data lacks categories, report live coverage limitation, don't manufacture them.
Typecheck/FE production build and affected backend compile/build once after final code plus required targeted feature checks. No repeated historical successful behavior suites. Rerun only after a concrete affected correction.
Publish/restart changed services once after checks, stop Host before overwriting its locked publish path. Verify health/ready and actual served FE BUILD_ID/chunk once. Read-only /fa/admin/fulfillments AND /admin/fulfillments at390/1440 with DOM/text/network (no screenshots); use existing categories/empty states honestly.

DELIVERY / EVIDENCE / RECOVERY
Persist exact received TASK docs/ai/tasks/TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271/TASK.md.
Evidence docs/architecture/evidence/TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271/: TASK.md, RESULT.md, RECEIPTS.md, result-contract.txt, work-stage-mapping.md, timing-counts-and-query-proof.md, user-review.md.
Record actual mappings/precedence, terminal-vs-blocked rule, relevant time/undated semantics, count dimension definitions, old URL mapping, affected seams/paths, named commands/counts, deployed BUILD_ID/services, actual read-only data coverage and limits. Compact evidence, no screenshot/credential/huge logs.
Persist the R4 acceptance checkpoint above at docs/architecture/evidence/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R4/ARCHITECT-REVIEW.md. Preserve original R2/R3 PARTIAL reviews and backup/concurrency limits; no historical rewriting.
Update top AGENTS/START-HERE and shipping plan pointer: R4 latest architect-accepted repair within scope; 271 actual implementation result awaiting Architect; no automatic next task. Recovery identifies latest accepted, last implementation, active/none and remaining user trial/limits. No giant documentation cleanup.
One review URL to actual queue. Explain what current existing records actually show, including zero/undated/history categories. No fake actionable scenario.
Normal commit/push/fetch; exact issued/actual base, code/evidence/final SHA and HEAD==origin/main. No force/amend/reset/rebase/stash/unrelated scripts/secrets. Compatible incoming docs can normal merge with declared facts; unsafe code divergence -> RECOVERY_CONFLICT.

RESULT CONTRACT / SUCCESS / STOP
PASS requires actual backend stage/time/count query wiring AND existing queue UI, not labels on old overlapping filters. No lifecycle permission or business data changes. Worker PASS != Architect ACCEPT or user acceptance.
ONE final canonical Result, no placeholder/probe/progress Result:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Actual Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode and Bridge-Task-Row.
Status: actual PASS / PARTIAL / BLOCKED / RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: c501fe9bcdb2c42a26d05e23d44af288931612ed
Base-SHA: actual verified work base
Code-SHA: actual implementation commit or NONE
Evidence-SHA: actual evidence commit or NONE
automaticNextImplementationTask: NONE
Body: source root, mapping/time/count semantics, implemented wire/UI behavior, focused proof/deploy, existing-data coverage/unchanged protected rules, changed paths/elapsed/final parity, one review URL and limitations.
Use real values/no unresolved placeholders. END_TOOBA_WORKER_RESULT then END_TOOBA_TASK, nothing after.
No user visual/functional/universal shipping/provider/financial/consent acceptance claimed.
After final Result delivery and lifecycle completion IDLE and STOP. No next implementation/repair, polling or waiting loop.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK