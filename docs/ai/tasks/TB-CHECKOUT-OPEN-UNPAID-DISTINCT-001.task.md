PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-CHECKOUT-OPEN-UNPAID-DISTINCT-001
Parent-Task: TB-CART-OPEN-UNPAID-COUNT-REVIEW
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR
Target: main, current origin/main
Timebox: target 10-12 minutes; hard stop 15 minutes

OBJECTIVE
Repair a verified risk in Order-owned CheckoutAbuseGate: open unpaid CHECKOUT limit may count seller-order rows rather than distinct checkouts. One multi-seller checkout must consume exactly one MaxOpenUnpaidOrdersPerCustomer slot. Keep the new reservation-cycle unpayability exclusion intact.

PREFLIGHT / AUTHORITY

Verify clean/safe tree and HEAD == origin/main. Never reset, stash, overwrite, or force-push unrelated work. If unsafe, STOP RECOVERY_CONFLICT.
Read AGENTS.md and .cursor/skills/tooba-backend-feature/SKILL.md; inspect applicable Order contracts, CheckoutAbuseGate, existing tests and the checkout limit/business-policy authority. Do NOT read the architecture findings register unless expressly requested (it is opt-in only).
Verify current source; known candidate is src/backend/Modules/Order/Tooba.Order.Infrastructure/Checkout/Abuse/CheckoutAbuseGate.cs, CountOpenUnpaidAsync: distinctCheckoutIds used for projections, but final Count operates on original openUnpaidCheckoutIds. Confirm real defect before editing. Do not infer unverified behavior from this task text.

BOUNDED REPAIR

Count DISTINCT CheckoutIds for the same customer across unpaid seller orders, excluding a checkout only if canonical ReservationCyclePayabilityPolicy.HasReachedRetryLimit says it is unpayable. Preserve zero-cycle/null-projection behavior, multi-seller state semantics, tenant/access isolation, existing cap values and error codes. No duplicated payability policy.
Minimal change in the Order owner. Do not adjust other statuses, retry policies, reservation coordinator, DB schema, query contracts, frontend, baseline, or unrelated code.
Add a focused behavioral regression exercising REAL count/limit behavior through existing seam/fixture as feasible: one checkout with two unpaid seller orders counts as ONE, two distinct checkouts count as TWO, retry-exhausted checkout excluded, active/retryable checkout included. Ensure a test fails without the fix. If fixture cannot exercise the real query within timebox, STOP or explicitly report unproven behavior; do not substitute only source-text assertions and call them behavioral coverage.
Run focused Order/Host tests plus an appropriate build or architecture guard, report actual command, failures and baseline distinctions. No automatic full-suite or AMSC waves.
Review git diff for exact intended scope, no new dependency edges or tests weakened. Commit and push only after all focused gates pass; verify HEAD == origin/main and clean relevant tree. Otherwise STOP with smallest blocker and no speculative workaround.

DELIVERABLE
Return canonical BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT via Bridge: root cause, precise files/diff, behavioral test setup/results, test commands, final SHA, baseline, any remaining risk. Worker PASS is not Architect ACCEPT. Stop: USER_REVIEW_AFTER_BOUNDED_REPAIR. No next task automatically.
END_TOOBA_TASK