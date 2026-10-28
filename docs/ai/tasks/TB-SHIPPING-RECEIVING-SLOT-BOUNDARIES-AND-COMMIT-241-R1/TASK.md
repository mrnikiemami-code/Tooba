PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-RECEIVING-SLOT-BOUNDARIES-AND-COMMIT-241-R1
Parent-Task: TB-SHIPPING-RECEIVING-SLOTS-AND-PRIMARY-RUNTIME-241
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — close two real acceptance blockers on the existing primary
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: FIX_BOUNDED_SLOT_ARITHMETIC_AND_EXPLAIN_REPAIR_PUBLIC_COMMIT_500
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

USER AUTHORITY / SCOPE
Continue the user's authorized repair: configured 9–10 / 10–11 receiving choices, exact chosen hours in the order,
ONE primary 3000/5088, recovery current. NO screenshots/images/visual-review/viewport work. No new port/edition/DB,
no repeated commissioning, no new scheduling/checkout engine. Do not redo 241's successful 60/120-minute paths.
241 is PARTIAL on Architect review, not accepted as complete. Original delivery deadline remains MISSED.
This is a bounded repair focusbox of 35 minutes, NOT a replacement completion promise. At 10 minutes report
the bounded-arithmetic control and the actual commit exception owning code. Stop/report if a new blocker changes scope.

START / CURRENT PRIMARY
Reviewed source 0bcfa52aa4ede4838b45dceb60ec2931219a6f7f; evidence/main at review
64e3c1ce53df6a4d35c2c9b70e0a1de20f264def. Integrate this issue normally; no history rewrite.
Primary FE3000 -> Host5088, Marketplace / Tenant NULL / DB tooba_marketplace_tb239.
Handoff profile D:\temp\tb241-chrome CDP9350 buyer 01a18247-54d3-7000-9f2f-219ac7de9e6d,
cart 01a1846b-aede-7000-850f-de073a511739 is USER review work: do not enumerate/automate/reload/close that window.
Use normal APIs and a separately task-owned session if browser interaction is necessary. No new application server port.
Prepare immutable build BEFORE planned replacement; stop a verified watcher before editing watched source to avoid HMR.
Preserve unsaved user form input, browser profiles, DBs and previous SingleStore configuration. No global Docker restart.
Only a bounded primary binary/FE replacement needed for these fixes is authorized; return it healthy with same edition/DB.

1 — FIX THE REAL CALENDAR LOOP IN PLACE
File: Order.Application/Storefront/Services/StorefrontShippingCalculator.cs
Current loop uses TimeOnly.Add(length) BEFORE checking closes > day.ClosesAt. TimeOnly wraps at midnight:
* length 1440 returns the SAME opening every iteration => endless loop (1440 is currently accepted by range validation);
* a 09:00–23:00 day with 120-minute slots reaches 23:00, then wraps to 01:00, which is not > 23:00:
  invalid reversed/out-of-window slots and nontermination are possible.
Do NOT execute an unbounded red case in the main test process or primary Host. A subprocess with explicit timeout
is lawful for a red control if necessary; do not leave a hung worker/server.

Replace only the existing canonical slot iteration with bounded INTEGER TICK arithmetic:
lengthTicks = exact minutes * ticksPerMinute; derive nonnegative remaining day window from closeTicks-openTicks;
number of complete slots = floor(windowTicks/lengthTicks). Iterate that bounded count and construct TimeOnly
only from already-bounded start/end ticks. No wraparound, rounding, shorter tail, overnight/day inference,
duplicate interval, floating-point or front-end arithmetic. Preserve 100ns authored offsets.
NULL duration remains ONE exact full window. AvailableDateCount counts distinct eligible dates.
The SAME canonical calendar must serve read, selection, payment preview and final acceptance; no parallel builder.

2 — ENFORCE THE ALREADY-ISSUED NO-FULL-SLOT CONFIGURATION RULE
241's task required every active day to hold at least one complete slot, but the landed domain only validates
duration range, and the calendar currently counts days that yield no slots.
Validate chosen duration AGAINST every working day in the EXISTING authoring preflight BEFORE mutation/publication.
Use stable module-owned fault/resources. 09–17 duration 1440 (and 09–10 duration 120) must refuse without any draft
revision/UpdatedAt/week mutation. Closed days have no slot requirement. NULL preserves exact old behaviour.
A remainder is unavailable, never a shorter slot; show the short admin hint promised in 241 for discarded tail.
Protect the immutable-payload reader/canonical calendar against contradictory existing/corrupt duration/week too:
refuse deterministically, no silent empty horizon, day skip that invents later availability, or loop.
Do not restrict all valid durations to the UI's dropdown list. 1..1440 is the numeric contract; the actual same-day
window determines feasibility. No unsupported overnight window added to make 1440 fit.

Focused cases required:
- 09–17 /60 and /120 exact unchanged;
- 09–23 /120 terminates at 21–23, no 23–01;
- 09–12 /100 one 09–10:40, no tail;
- 1440 and overlong duration on a working same-day window refuse promptly BEFORE any mutation;
- exact fractional 100ns opening preserved through slot endpoints;
- closed days none, legacy format1/NULL one exact full window, distinct-date horizon unaffected.

3 — CLOSE THE DISCLOSED PUBLIC COMMIT 500, NO GUESSING
241 RECEIPTS E: POST /v1/storefront/shipping/commit -> 500 platform.unexpected /
ContractOperationException, traceId 66fcc1116ad89db2014e4a4f6f6fb073.
Read that task Host log/private receipt and identify the ACTUAL operation, stable exception reason and fresh/converted
cart/request state. StartPayment success does not close a public commit regression.
Reproduce on a fresh task-owned lawful selection through normal HTTP with exact revision TEXT and saved slot.
If valid commit is broken, repair its owning EXISTING facade/handler/adapter. If the old request was invalid,
prove the exact expected stable 4xx refusal, correct the error translation at its owner if it currently becomes500,
and prove a valid public commit200. Do not catch all ContractOperationException as success/conflict/not_ready.
No quote repetition, fallback legacy path, removed slot/pointer/address/revision validation, or invented token.
Prove valid public commit persists the selected subinterval, then same-key replay returns original order after a
task-only publication duration change. Existing StartPayment must still work and create no duplicate order/payment.
No PSP/paid/provider-exactly-once certification; stop at real manual Pending.

4 — MIGRATION / DATA / TEST LIMITS
Existing 241 Shipping/Order migrations are ALREADY APPLIED. NEVER edit them in place or prior history.
The loop/preflight/translation repair should need no schema change. If one appears necessary, report concrete reason
before broadening. No seed/reset/reseed/business SQL/cross-DB copy, no store-alpha writes, no auth weakening.
Keep capture formats1/2, nested slots, exact ticks, typed owner/group partition, coupon context and compact evidence.
Tests must exercise the actual changed authority. Run exact focused tests + relevant build/typecheck once, not a
full Order/Shipping/Host/solution/guard sweep. Report exact commands/counts/errors; do not say only "coverage landed."
No fake success, test-only production hooks, altered baseline guards, full Snapshot regeneration or new engines.

5 — HANDOFF / RECOVERY / STATUS
Keep ONLY primary3000/5088 user runtime. No need to re-open old 310x/518x/5190 servers.
Return same visible already-authenticated user window without reading/reloading/closing it; a safe build replacement
may require user refresh, report that plainly. Preserve cart and its unsubmitted state; do not submit user's review cart.
If a separate task review session is used, label it explicitly, never replace user's account by inference.
Update all three TOPs (AGENTS.md, START-HERE-IF-CHATGPT-IS-LOST, SHIPPING-ARCHITECT-RECOVERY) + 241 append-only
correction + this RESULT/RECEIPTS. Keep 241's successful primary/slot history; demote its unqualified PASS.
Report primary source/binary, edition/tenant/DB, actual endpoints/statuses/order chosen ticks and exact unresolved gaps.
No screenshots. Original deadline MISSED, no automatic242, no new completion-time promise.
Single normal exact-path commit/push; no force/amend/reset/stash/rebase/mass add.

RESULT CONTRACT (fill full SHAs or NONE; no placeholder/probe POST)
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-RECEIVING-SLOT-BOUNDARIES-AND-COMMIT-241-R1
Parent-Task: TB-SHIPPING-RECEIVING-SLOTS-AND-PRIMARY-RUNTIME-241
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — bounded slot and public commit repair
Mode: FIX_BOUNDED_SLOT_ARITHMETIC_AND_EXPLAIN_REPAIR_PUBLIC_COMMIT_500
Status: PASS|PARTIAL|BLOCKED|RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: <issue commit>
Base-SHA: <actual source start>
Code-SHA: <full SHA or NONE>
Evidence-SHA: <full SHA>
automaticNextImplementationTask: NONE
<primary URL + exact usable stage; measured arithmetic/preflight cases; actual commit-500 cause and owning code;
valid commit/replay/StartPayment receipts; exact focused commands/results; user-state preservation; time/slips/gaps>
END_TOOBA_WORKER_RESULT
Append the task-end marker on a separate final line in the transmitted result.

END_TOOBA_TASK
