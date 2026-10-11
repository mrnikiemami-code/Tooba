PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-CART-RETRY-LIVE-ACCEPTANCE-001
Parent-Task: TOOBA-CART-EXPIRED-RESERVATION-RETRY-FIX
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: VERIFY_LIVE_BOUNDED
Target: main, current origin/main
Timebox: target 10-12 minutes; hard stop 15 minutes

OBJECTIVE
Independently establish whether the customer-facing "Check availability and pay again" control on /fa/cart works for a real, authorized unpaid checkout whose inventory hold has expired and whose paymentId is null. Verify the complete existing workflow delivered in commit 1ae919f2: checkout-scoped reservation retry, server-owned reacquire, same checkout identity, refreshed pending-payment capability, then legal payment continuation. This is an operational acceptance task, not authorization to implement another feature.

MANDATORY PREFLIGHT

Verify clean/safe tree and HEAD == origin/main; do not rewrite, stash or discard unrelated changes.
Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md, .cursor/skills/tooba-backend-feature/SKILL.md, docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md and applicable Shopeiva/checkout locks.
Inspect current frontend pending-payment UI/adapter, Order-owned retry endpoint, identity gate, reservation coordinator and focused tests; verify no intervening commit changed their contract.
Do not assume the local Host or frontend is running or that any prior test result proves live success.

BOUNDED PROCEDURE

Find an already-existing safe Development-only test checkout with expired hold, unpaid status, paymentId null and a legitimate authenticated customer session. Prefer a dedicated existing fixture and browser session. Do NOT invent an actor, bypass identity gates, fabricate proof or paymentId, impersonate another user, or change live/production data.
If no such checkout exists, use ONLY an existing approved Development fixture/bootstrap flow to prepare one. If preparation would require schema changes, manual database manipulation, new test endpoints, bypassing auth, or deleting/recreating a customer order, STOP with BLOCKED_TEST_PRECONDITION and precise prerequisites. Do not silently introduce test hooks.
Confirm before click from actual read-only API/server state: checkoutId, paymentId absent, pending order, expired hold, server-authorized primaryAction=retryAfterExpiry, stock/supply and remaining retry quota. Do not print secrets, cookies, proof values or customer PII in evidence.
In the real browser at /fa/cart with a valid session, click the existing CTA exactly once. Capture request method/path/status (redact credentials), server response code, and UI states. Verify POST /v1/storefront/checkout/{checkoutId}/retry-reservation is invoked for paymentId=null; there must be no silent return.
Verify server truth AFTER click: original checkoutId unchanged, authorized new hold/cycle when stock and retry policy allow, no duplicate checkout/payment/reservation, and refreshed pending-payment response transitions to an actionable pay state. Follow payment navigation as far as safely possible; do not submit real money, finalize charge or create paid orders. If gateway initiation may create a charge, stop before initiating it.
Run one negative check using existing safe fixture/unit or read-only data: unavailable supply, retry limit, or unauthorized access must fail closed with stable code and user-visible localized error. No destructive requests against third-party/customer orders.
Run focused tests appropriate to the confirmed code path; run cd src/frontend && npm run test:critical-storefront if a frontend production file is changed (frontend changes are NOT authorized by default). Report existing reds separately. Do not claim visual/browser PASS without browser evidence.

ARCHITECTURE AND SCOPE GUARDS

Verification-first. No implementation, frontend redesign, new API, new payment flow, new locale implementation, schema or seed mutations, baseline reset, AMSC waves, or unrelated cleanup in this task.
Maintain architecture as genuinely multilingual: existing translations and configured locale/fallback policy, never presume FA/EN is the complete language list.
If the live flow exposes a defect, STOP and report exact failing step, HTTP/status/errorCode, code owner and smallest proposed repair. Do not self-authorize a fix.
Critical storefront locks remain intact; do not change Home/PDP/shared visual components.

DELIVERABLE

Result must state LIVE_PASS, LIVE_FAIL, or BLOCKED_TEST_PRECONDITION. A 401 or 404 probe alone is NOT a success-path test.
Provide a compact timeline of actual browser click and server state before/after, with sanitized request/response evidence, Checkout ID consistency, cycle/hold observations, capability change and safe payment navigation result.
Report exact files read, actual commands/tests, what was and was not tested, and any remaining risk. Never call merely theoretical/unit coverage live acceptance.
Commit and push ONLY sanitized evidence documentation if useful and authorized by repo task procedure; NO production code changes. Follow canonical Bridge result envelope and lifecycle. Stop for user/Architect review after the result; no automatic follow-on task.

Stop: USER_REVIEW_AFTER_LIVE_ACCEPTANCE
END_TOOBA_TASK