PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256
Parent-Task: TB-SHIPPING-MARKETPLACE-MULTISELLER-TEST-ORDER-255
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — separate carrier acceptance from customer delivery and plan dispatch
Mode: IMPLEMENT_CARRIER_ACCEPTANCE_HOURS_MAX_DELIVERY_AVAILABILITY_AND_DURABLE_DISPATCH_PLAN
Expected-Base-SHA: e758169bde872c6706ebde010b64f363e00fb9b6
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Recovery and scope
Read the CURRENT TOPs and docs/planning/shipping/SHIPPING-OPERATIONAL-CLOSURE-PLAN-256-260.md.
Architect has already committed the plan and updated all three recovery TOPs. Pull normally from a safe tree; disclose newer descendants, never reset unrelated work.
255 is PARKED by the user. Do not revisit edition/database switching or create a Marketplace test order.
This task is stage 1 ONLY. Do not implement planned 257–260 (tracking/quantity, return/resend, packing choice, workqueue redesign).

Before code read applicable backend/frontend feature skills, architecture standards and Shipping admin/customer UX rules required by AGENTS.md. Reuse existing module boundaries, Contracts, Result/errors, shared owner-scoped document/publication and design-system UI. Do not create a second scheduling authority.

## 2. Product meaning — explicit correction
We already HAVE:
- authoring per-region min/max delivery days and customer receiving weekly hours;
- legacy PreparationDays/LeadDays and an operational RequestedPickupAtUtc evaluator.
But captured storefront BuildReceiving currently passes MinDeliveryDays into the calendar and ignores carrier closing-time cutoff. Its selected receiving window is NOT a carrier handoff schedule or dispatch deadline.

User NOW requires:
- min/max 2–3 => first customer dates based on MAX 3 working days;
- service carrier acceptance schedule independent from customer receiving schedule;
- for carrier hours 09:00–17:00, order at/after 17:00 cannot use today's handoff;
- Wednesday carrier closing at 12:00 must work independently from customer 09:00–17:00;
- same-day handoff feasibility must be real; no automatic assumption already physically sent;
- derive planned handoff/dispatch for the chosen customer date, so operations can later show due/overdue.

No second delivery-duration field duplicating min/max. Do not add PreparationDays twice: audit current meaning and isolate the new policy/version explicitly.

## 3. Authoring, storage and publication
Extend SAME shipping method authoring:
- timing tabs «تحویل به مشتری» and «پذیرش حامل» (short clear equivalent acceptable);
- customer controls retained: per-day hours + existing full-window/hourly slot semantics;
- carrier controls: seven days active/closed, per-day opening/closing and trusted timezone; no customer-slot duration imposed on handoff;
- button «کپی از ساعات تحویل مشتری»: ONE-TIME copy into independently editable carrier weekly hours, no implicit link;
- copying changes local draft only until existing Save; Cancel/discard/reconcile semantics preserved.
Reuse existing suitable operating schedule domain primitives after mapping ownership/document/revision semantics. A global service-only schedule must not leak between owners or publication versions.

Wire create/update/editor-document/query/validation/captured revision/publication/readback as required. Valid draft edit/save must roundtrip both distinct schedules through real storage. No browser-only cutoff.
Existing documents/revisions with absent carrier hours must decode and remain readable. Absence is explicit needs-configuration for NEW timing feasibility, never silent copy/fabricated hours or rewrite of old snapshots.
Read-only published views show useful schedule facts, no raw keys/technical prose.

## 4. One feasibility calculation, not frontend arithmetic
Implement a pure bounded domain/application scheduling authority exposed through proper owning Contracts:
- trusted nowUTC -> configured timezone;
- true readiness from current trusted preparation/order path, not fake packed-now;
- first feasible carrier handoff inside a valid acceptance window;
- half-open hours [open,close): exactly closing time is too late; before opening can target opening; closed day advances;
- transport maximum working-day duration; skip nonworking transport days using the existing authoritative calendar if present;
- customer receiving slots from their SEPARATE schedule.
The existing setup supplies weekly hours, not an arbitrary holiday service. Do not add an external calendar project. Explicitly record which configured calendar defines counted transport working days. If no separate transport calendar exists, use the configured carrier working DAYS as the bounded policy for this version, and filter receiving dates/slots separately by customer hours. Do not guess dates from browser locale or count a nonworking customer day as a valid receiving date.
Count N positive transport days AFTER the feasible handoff date; zero-day must be explicit and leave a physically feasible customer slot after readiness/handoff, never offer elapsed slots.
At after-close registration the first handoff moves forward; do not separately add another arbitrary one-day delay.
MAX must be present/valid; never fall back silently to MIN when MAX is missing. Existing validation min<=max remains.
Explicitly audit whether the existing durations include preparation. Keep accepted history semantics unchanged; new policy applies to new acceptance. Use existing trustworthy preparation where required exactly once; do not introduce a hidden preparation default.

Forward availability and latest permissible handoff for the selected receiving date must use SAME authority/calendars. A bounded backward candidate search validated by the forward evaluator is acceptable; naive calendar subtraction is not.
Daily-duration evidence must not claim exact transit-hour guarantees. The chosen date/window is the receiving commitment; distinguish date feasibility from unknown precise transit timing honestly.
No feasible schedule -> localized owning blocker and unavailable calendar; checkout cannot accept an impossible new commitment.

## 5. Durable new dispatch plan
For NEW accepted shipping groups store the accepted planned handoff/dispatch window/deadline and enough provenance to reproduce it:
schedule/revision identity, effective MAX duration, timezone, calculation policy version/readiness reference as appropriate.
Use an existing seam if suitable, no speculative framework. Keep Order accepted evidence vs Fulfillment actual events ownership clean.
Return these facts through existing order/group projections so later due-queue work consumes authoritative data. In existing operational order UI show only a concise planned «موعد ارسال» distinctly from «تحویل مشتری» if this is needed to review the implemented path.
Actual DispatchedAt/DeliveredAt remain actual events, not set by scheduling.
Historical accepted orders: no recompute/backfill from mutable current settings, no changed receiving commitments/money. Missing dispatch plan stays explicit unspecified; do not mark historical items overdue from a guessed date.
Checkout revalidation must catch changed schedule/revision and expired same-day acceptance before new acceptance; no stale quote silently accepted.
Preserve shipping selection/provisional choices/revalidation and group fingerprint guards; no unrelated cart/navigation change.

## 6. Focused proof — executable boundaries
Run meaningful affected tests, typecheck/build and required boundary checks once; broaden only for a new failure.
Minimum scheduling cases:
A. 2–3 uses 3, never 2.
B. before opening, just before close, exactly close, after close.
C. Wednesday 12:00 close with customer receiving to17:00; Friday closed; rollover next working acceptance.
D. timezone midnight, days skipped, no active carrier days, missing schedule, invalid max, zero-day expired slots.
E. later chosen receiving date -> valid latest handoff; forward/backward agree.
F. readiness/preparation counted once; no double offset.
G. old snapshot decodes unchanged and accepted history not rewritten.
H. new accepted evidence/projection roundtrip, revision/cutoff revalidation; owner isolation.
I. mounted tabs/copy/edit/cancel/save payload: carrier edit never mutates customer hours, copy is one-time, single submit, readable RTL/time input/44px targets.
New schema/persistence invariants need real relational roundtrip through production adapters in task-owned disposable proof if required; controlled fixtures only, never user-order mutations. Do not replace required storage proof with InMemory. No full unrelated Shipping/Order suite loop.

## 7. Canonical deployment and actual UI handoff
Original source D:\Users\User\source\repos\SarvNewVer.
FE3000 -> Host5088, SingleStore/store-alpha/tooba_alpha.
No edition/connection switch/new final DB/port/runtime. Local development additive migrations necessary for THIS scope are authorized after private backup, exact target+pending migration inventory and repository MigrationRunner procedure. Unexpected unrelated pending migrations -> disclose/stop applying them, not blanket permission.
No manual business SQL/reset/reseed; no write to user accepted orders/cart/pack/dispatch/payment.
Preserve DataProtection and CapturedPreview local ON with 7 dates, checked-in OFF.
Build/publish/restart required changed backend/FE from canonical source. Verify runtime identity + served build, not merely compiled source.
Authorized read-only browser render of existing authoring route and order timing facts at desktop/mobile; task-owned session/profile, no screenshots/user tab manipulation.
Real schedule save/readback may be proven with a task-owned draft via supported API (explicitly authorized); do not modify user's existing draft/published service to manufacture proof. Identify the test draft clearly.
A new real user checkout/payment is NOT authorized here; use controlled production-path integration fixtures for new acceptance.
If authorized DOM/storage/deploy is blocked, PARTIAL with exact limit; no full-ready claim.
Review authoring URL:
http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit
Keep user's draft values untouched. Provide any task-owned draft review link if needed.

## 8. Evidence / recovery / result
Persist exact TASK.md at docs/ai/tasks/TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256/TASK.md and task evidence directory with RESULT/RECEIPTS.
Update the closure plan status row and all three TOPs:
AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md
Record exact counted-day semantics, cutoff boundary, schedule provenance, migration/deployment state, historical compatibility and limits. Pending 257–260 remain NOT ISSUED; 255 parked.
Commit/push normally, verify local==origin/main, no force/amend/reset/stash/rebase. Include Base/Code/Evidence SHA and measured elapsed.
Return complete BEGIN_TOOBA_WORKER_RESULT envelope with PASS/PARTIAL/BLOCKED, proofs, deployed identity, migrations, review URL, remaining list. Worker PASS is not Architect/Visual ACCEPT.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
