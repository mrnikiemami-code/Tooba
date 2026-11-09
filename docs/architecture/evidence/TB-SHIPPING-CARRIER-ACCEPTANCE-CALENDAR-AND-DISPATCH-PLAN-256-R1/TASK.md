PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R1
Parent-Task: TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — professional calendar tabs and reliable user-testable scheduling
Mode: REPAIR_REJECTED_TIMING_TABS_COPY_AND_BOUNDED_SCHEDULING_GAPS
Expected-Base-SHA: 639947fc5573a3ae29ec1662d117c7d75e40b856
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. User feedback is now received; execute ONLY this correction
User tested 256 and rejected the customer/carrier switch styling: it looks like mutually exclusive blue buttons, while BOTH calendars coexist. User also rejects repetitive technical explanations in the operator UI.
They authorize progressing to the next meaningful user test, not starting 257–260.
Read CURRENT recovery, closure plan and 256/ARCHITECT-REVIEW.md, then the applicable mandatory frontend/backend skills and Shipping rules. Preserve 256 delivered storage/calendar work. Do not redesign authoring again.

## 2. Exact visual correction
Use actual tabs consistent with the page's existing underlined General/Regions tabs:
- labels «تحویل به مشتری» and «پذیرش حامل»;
- a shared baseline; active text and underline; inactive neutral text;
- NO filled blue selected-button/pill treatment for this navigation;
- correct tablist/tab/tabpanel, aria-selected/controls, stable IDs, focus and arrow-key behavior consistent with the existing design system and RTL;
- changing tabs only changes presentation, NEVER disables either schedule or resets unsaved edits.
Preserve customer slot choices and the real day active/closed controls; do not indiscriminately turn all segmented controls into tabs.
Keep title and day/hour rows compact and readable, normal glyph sizes, 44px target areas, desktop/mobile no horizontal page overflow.

Copy/description hygiene:
- remove repetitive paragraphs about independent calendars, one-time copying, technical promise/UTC/source/revision/semantics from the visible normal form.
- tab labels and headings should explain the sections. At most ONE concise helpful sentence where needed.
- keep the purposeful button «کپی از ساعات تحویل مشتری».
- optional brief copy success feedback is enough; do not print long invariants beside it.
- keep validation errors, essential empty-state action, and real warnings. Do not hide a blocker merely for visual polish.
- operator-facing text never exposes tenant/Store/source codes or internal implementation.
All configured languages use existing message system; no hardcoded FA/EN-only authority, no global CSS or unrelated storefront redesign.
Do not delete provenance/errors from backend diagnostics; the request concerns operator presentation.

## 3. Close the concrete scheduling gaps before claiming full readiness
Source-confirmed at c58e87e5e:
A. StorefrontPlannedDispatchResolver returns success/null for absent carrier week; composition still presents a legacy MAX-only calendar. Historical readability does NOT authorize NEW incomplete commitments.
Make new acceptance under current planned-dispatch policy require configured carrier schedule and complete dispatch evidence; absent configuration yields one localized actionable availability blocker. Preserve old accepted orders/revisions read-only; no history rewrite or invented calendar. Retain a compatibility distinction explicitly instead of silently keeping new incomplete checkout enabled.

B. Current BuildReceivingCalendar assumes nowUTC is goods readiness. Inspect preparation/readiness source and duration semantics before changing arithmetic. Preserve user rule: MAX 2–3 => 3 transport working days AFTER first feasible carrier acceptance; same-day only within valid acceptance and actual configured preparation applied once. If no authoritative extra preparation input exists, do not invent one or claim physical readiness: document the explicit policy assumption and expose missing required readiness as unavailable where appropriate. Do not add a second preparation setting or double-add duration already covering it. Keep this repair narrow.

C. ResolveLatestHandoffForReceivingDate currently requires forward arrival EXACTLY equal to chosen date. A package able to arrive BEFORE the customer's selected receiving date must not be rejected merely because carrier acceptance is closed on that receiving day. Select the latest valid handoff whose conservative forward arrival is ON OR BEFORE the selected receiving commitment and which remains feasible from readiness. Do not invent early customer delivery: actual commitment remains customer's selected date/window. Do not delay already-in-flight parcels or change accepted money/history.
Forward/reverse agree, including carrier-closed/customer-open dates. Preserve bounded search without overflow/unhandled exceptions for supported duration/day bounds. Test sparse carrier week and maximum supported duration; do not simply widen constants until green.
Check zero-day different-timezone comparison through actual instants; never compare carrier clock values directly to customer clock values. No elapsed customer slots.

D. Expose the EXISTING accepted PlannedDispatch facts in the same operational order/group read model/view as a concise «موعد ارسال» distinct from «تحویل مشتری» if missing. Historical null plan is explicitly unspecified, never recomputed. No new queue, no finance/layout rewrite.
Do not fabricate a real checkout just to populate this row.

## 4. Tests and proof — one existing database ONLY
LATEST USER OVERRIDE: NO extra database at all, including disposable/test/parallel DB, no Docker PostgreSQL container, no 15433 server. tooba_alpha and canonical ports only.
Do not rerun TB231/TB256 disposable relational commands. Prior violation is recorded; do not conceal or repeat. Do not delete an existing DB/container during this task.
No new schema expected; if truly necessary, disclose exact reason and use existing safe additive local migration procedure with private backup only; no reset/reseed/manual DDL/business SQL.

Focused checks only:
- mounted accessible true tabs, retained edits across switches, copy independence, concise copy/no duplicate description;
- new unconfigured method gives owning blocker, legacy accepted group still readable;
- MAX/cutoff before-open/exact-close/after-close;
- reachable customer-open/carrier-closed receiving date;
- readiness/preparation once, zero-day timezone boundary, supported sparse-week bounds;
- existing Order evidence/projection read facts, no money/history regression.
Use pure tests/mounted fixtures for deterministic time cases. Do not write test orders, dispatch, deliver, cancel, pay or change user's published service.
No full-suite/repeated proof loop, no screenshot. Report unrelated pre-existing reds without fixing/rebaselining.

## 5. Deploy then stop for a meaningful visible test
Same source D:\Users\User\source\repos\SarvNewVer.
Same FE3000 -> Host5088 / SingleStore / store-alpha / tooba_alpha.
No edition switch, new final runtime/port/DB; 255 remains parked; live Tipax remains deferred.
Rebuild/publish only affected components; preserve DataProtection, local CapturedPreview ON/7 dates, checked-in OFF.
Authorized task-owned browser read-only DOM on actual route: correct tabs, concise copy, preserved edits after tab switching, no overflow at desktop/mobile. No user tab/profile manipulation; no screenshot artifacts.
Existing user draft stays unchanged unless USER saves it. No task-owned merchant writes required for this correction.
Review:
http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit
Operational order read-only:
http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf

Provide a SHORT user checklist:
1. real tabs, no exclusive-mode appearance;
2. customer/carrier edit independence + one-time copy;
3. real saved configuration needed for new availability; unconfigured gives clear blocker.
List exact simulated cutoff expected dates separately, not instructions to alter computer clock.
If a published configured method is absent, say cutoff is code/fixture-proven and not yet testable through that user's live method. Do not claim end-to-end acceptance from shell200 or a nonpublished draft.
Stop here for user trial. No task 257 or auto next implementation.

## 6. Recovery/result
Persist verbatim TASK and RESULT/RECEIPTS under this task ID in docs/ai/tasks/ and docs/architecture/evidence/.
Update closure plan and all three recovery TOPs with:
- implemented vs still-open details;
- same-database override (applies to every future task);
- final served/runtime identity and next user test;
- user feedback required before further stage; no false Architect/Visual acceptance.
Commit/push normally, verify local==origin/main, preserve unrelated work; no force/amend/reset/stash/rebase.
Return complete BEGIN_TOOBA_WORKER_RESULT with exact Base/Code/Evidence SHA, focused proofs, actual deploy/authorized DOM limits, review URL and remaining issues.
Worker PASS is not Architect/user ACCEPT.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
