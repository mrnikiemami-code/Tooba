Persisted claim artifact — full Architect body received via Bridge
`GET /api/tasks/next?channelId=tooba-main&workerId=tooba-worker-01&agentType=cursor`
at Bridge task UUID `111cd8ec-de60-4fc9-84ea-f536a63931ab` (createdAtUtc 2026-10-16T19:38:00.0207347Z).


PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-GROUP-UX-ACCEPTANCE-154
Parent-Task: TB-SHIPPING-GROUP-ID-CONTINUITY-153
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ab3f70b203056ff464cc963d33c2825cdb63ff29
Program: TOOBA-SHIPPING-INDEPENDENT-SELLER-COMMITMENTS
Mode: BOUNDED_PRIVATE_BROWSER_CONTINUE_GATING_AND_RELOAD_ACCEPTANCE
Timebox: 45 MINUTES HARD
Status-Target: PASS_OR_PARTIAL_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE

ARCHITECT DECISION
TB-153 PASS for stable persisted group ids, with 365/365 Order tests and real private DB readback. Its scratch payment-draft returned 409 checkout.tax.unavailable due to a new unpriceable tax fixture; TB-152 previously proved payment-draft HTTP 200. Do NOT reopen group identity or blindly modify tax production code. This task closes the remaining customer-facing Shipping acceptance: disabled -> enabled Continue state for incomplete/complete group schedules; independent selections and browser reload/back restoration; continuity on repeated saves. The user authorized proceeding without repeated approvals until a usable flow, but worker still stops after each bounded task.

LOCKED SOURCES (READ FIRST)
docs/architecture/shipping/SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md
docs/planning/shipping/SHIPPING-GROUP-SCHEDULE-WIREFRAME-145.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md
docs/architecture/evidence/TB-SHIPPING-INDEPENDENT-GROUP-ACCEPTANCE-152/RESULT.md
and TB-153 evidence identity matrix.

MISSION — DELIVER REAL CUSTOMER FLOW
Use the existing TB-152 disposable authenticated browser + private Host/Postgres recipe and existing code; NO fake screenshots or fabricated carrier schedules. Configure two real seeded sellers with the SAME canonical tipax:standard method and preparation delays 2 and 3, yielding distinct group horizons. Start only task-owned private ports and scratch database, verify isolation first. The private setup must use current schemas/migrations and established development test-auth fixtures, never shared credentials.

Reproduce the real Shipping UI with TWO independent seller cards, member lines, per-group fees, separate earliest dates, independent date and time chips. Confirm real DOM markers and take actual screenshots in authorized private browser. No new wireframe or global styling changes.
Exercise Continue disabled -> enabled: create a legitimate incomplete state without tampering with backend authorization or inventing dates. An initial cart with an unchosen method or incomplete per-group selection should disable Continue; select a valid method and per-group date/window for each group in browser, assert Continue remains disabled until every active group is complete and enabled thereafter. If current UX auto-selects all windows by design, prove a legitimate incomplete state by method change/unselected line instead of introducing a deceptive deselect control. If impossible, report exact blocker; do not fake proof.
Persist via real authenticated shipping selection. Change ONLY A's window; verify B unchanged in UI, HTTP re-read, and database; navigate Shipping -> Payment preview (NO final purchase) -> Back and full reload; assert per-group dates/windows and stable ShippingGroupIds across saves/reloads. Confirm no global delivery date/time selector, no cross-method horizon intersection and no cart/order/payment mutation before explicit final click.
Read-only payment draft should respond 200 using the ALREADY PROVEN TB-152 scratch tax fixture recipe, adjusted only to actual seeded categories verified in scratch. Validate group fees exactly once, per-group snapshot continuity and total math. If tax is still 409, record it as separate fixture failure and do not obscure UI validation.
If a REAL BUG (not merely missing evidence) prevents 1-3, implement ONLY the smallest frontend/order-owner correction justified by the reproduction; write focused regression test, compile/typecheck and browser rerun. Do NOT do unrelated UI polishing, Admin finance, global tax/settlement policy or large refactor. If time does not permit safe fix and proof, STOP PARTIAL/BLOCKED with precise reproduction.

NON-NEGOTIABLE SAFETY

Maximum 45 minutes HARD; reserve final ~10 for focused evidence, cleanup, commit/push and report. Do not start a new implementation wave automatically.
Shared :3000/:5088/:5432, store-alpha/tooba_alpha, Redis, RabbitMQ and local dirty files must remain UNCHANGED. No restart, no cache purge, no production DB mutations; disposable scratch resources must be removed and port cleanup verified.
Never bypass eligibility/auth, invent customer fees, real carrier capacity, origin, payout split, or dates. No real PSP charge, final payment, SMS or real notification. No secrets/tokens/cookies in evidence.
Preserve single-group compatibility, server-owned group identity, seller-specific method+date eligibility, PendingAllocation finance invariant, unchanged storefront visual tokens and RTL.
If origin/main advanced, reconcile without modifying unrelated dirty/stashed work and DO NOT force push.

PASS CRITERIA

One real private authenticated browser run with two seller cards, same method and separate server-horizon dates; screenshots + sanitized DOM assertions.
Observed Continue disabled with legitimately incomplete group, then enabled only when both groups valid; persisted state restored after Payment->Back and reload.
Repeated save maintains both stable ShippingGroupIds (TB-153), independent date/window and line/fee membership in HTTP and DB. Rejected invalid write does not corrupt data.
Payment-draft 200 with group fees once and consistent totals, or else verdict PARTIAL with isolated fixture issue disclosed.
Zero early checkout/order/payment/PSP mutation; test results and private cleanup trace.
Commit only owned code/evidence; verify HEAD==origin/main, report PASS/PARTIAL/BLOCKED and STOP_FOR_ARCHITECT.

FOLLOW-UP (DO NOT EXECUTE)
After successful Shipping usability closure, resume Admin financial mode and per-method restrictions, real create-method API, multilingual save/write-roundtrip, EF debug log flood/scale-check, trusted transport calendar/origin authority, and approved shipping fee allocation. These remain OPEN in recovery.
END_TOOBA_TASK
