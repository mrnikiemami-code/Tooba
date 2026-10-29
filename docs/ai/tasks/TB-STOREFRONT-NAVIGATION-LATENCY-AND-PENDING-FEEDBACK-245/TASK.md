PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245
Parent-Task: TB-SHIPPING-ADDRESS-COMPLETION-PRESERVES-DELIVERY-CHOICE-244-R3
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Storefront — eliminate product/cart navigation stalls and add immediate pending feedback
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: MEASURE_AND_REPAIR_CANONICAL_STOREFRONT_NAVIGATION
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

# Sequencing prerequisite

Do not execute this task concurrently with 244-R3. Start only after 244-R3 has produced its worker result and its
source has been integrated into main. If the parent is still active or main lacks its final source, return
`BLOCKED_PARENT_NOT_FINISHED` without touching source/runtime. This task is prepared now so the global defect is
not lost; it is the next separate slice.

# User-reported defect

On the canonical storefront, clicking a product or “view cart” can take minutes before navigation completes. During
that time the UI gives no reliable indication that the click was accepted, so the user cannot distinguish working,
stalled or repeated clicks.

Both parts must be fixed:

1. identify and remove the actual latency/loop/retry/duplicate-request cause;
2. provide immediate, accessible navigation-pending feedback without hiding the root cause.

A spinner alone is not a repair. A faster test fixture or alternate port is not acceptance.

# Canonical scope

Use only:

- `D:\Users\User\source\repos\SarvNewVer`;
- FE `http://127.0.0.1:3000` → Host `http://127.0.0.1:5088`;
- SingleStore / store-alpha / tooba_alpha;
- the existing authenticated user and actual catalog/cart;
- production-style canonical frontend build/runtime, not dev first-compile timing and not D:\temp source.

Primary journeys:

- product card/link from home, listing/category and merchandising rails → real PDP;
- header/cart icon and “نمایش سبد خرید” / mini-cart action → real cart;
- browser Back from PDP/cart to the originating storefront surface.

# Measurement before source change

Without screenshots, capture a sanitized timing ledger for at least one product click and one cart click:

- click timestamp;
- first visual/ARIA pending-state timestamp;
- router/navigation start;
- document/RSC/API requests, start/end/status/initiator and duplicates;
- server endpoint duration and traceId for slow calls;
- first route shell render and usable content render;
- React effect/render/request counts relevant to the route;
- warm repeat timing and one cold canonical-process timing.

Classify the cause from evidence:

- duplicate click handler or nested Link/button navigation;
- redirect/auth/cart loop;
- repeated React effect/fetch caused by unstable dependencies;
- App Router RSC/prefetch/cache misuse;
- frontend BFF retry/timeout;
- backend endpoint/database query latency or N+1;
- stale service worker/build/runtime mismatch;
- production build missing route loading boundary;
- another measured cause.

Do not call a slow request a loop without request-count evidence. Do not blame compilation when the canonical
runtime is a production build.

# Repair requirements

## Root cause

Repair the owning layer. Examples:

- stabilize effect/fetch dependencies and abort superseded requests;
- remove duplicate navigation/request wiring while preserving normal Link semantics;
- fix an exact backend query/N+1/timeout only if the timing ledger proves it;
- correct cache/prefetch behavior without serving stale user/cart data;
- eliminate redirect cycles without weakening authentication or ownership.

No new storefront client, global state framework, endpoint, data shortcut or auth bypass.

## Immediate feedback

Every ordinary primary-button/product-card/cart navigation must acknowledge the click within **100 ms**:

- use the existing design system;
- show a localized pending label/progress affordance and `aria-busy`/live status;
- prevent accidental repeated same-target activation while pending;
- preserve Ctrl/Cmd-click, middle-click, open-in-new-tab and normal anchor semantics;
- clear pending state on success, failure, cancellation and browser Back;
- expose a clear retry/error only when navigation genuinely fails;
- do not freeze unrelated page controls globally.

Use an App Router `loading.tsx`, scoped transition state or existing shell facility only when it matches the
measured route lifecycle. Do not add fake fixed delays.

# Performance acceptance

On the canonical local production-style runtime after warm-up:

- pending feedback: ≤100 ms after click;
- exactly one logical navigation per click;
- no repeating document/RSC/cart/PDP request loop;
- warm product and cart navigation usable within **2 seconds** each;
- one cold canonical-process navigation usable within **5 seconds**, excluding an explicitly measured external
  dependency outage;
- no route remains silently pending beyond 5 seconds; it must complete or present a typed actionable failure.

If a required external dependency makes those limits impossible, report the exact endpoint/dependency and measured
duration; do not mark PASS.

# Focused proof

Add focused facts for:

1. product-card click produces one navigation and immediate pending state;
2. cart-header/mini-cart action produces one navigation and immediate pending state;
3. same-tick double click does not duplicate navigation/request;
4. modified/middle click remains native;
5. success clears pending;
6. route error/cancel clears pending and permits retry;
7. browser Back does not leave a stuck overlay/state;
8. no effect-driven request repeats from unstable object/function identity;
9. auth/ownership behavior remains unchanged;
10. warm canonical product/cart timing meets the budgets with actual HTTP.

Run frontend typecheck/build and directly affected tests once. Run backend build/tests only if backend source changes.
Do not loop broad suites or rebaseline unrelated guards.

# Real acceptance and handoff

On the actual canonical runtime:

- click a real product from the current storefront and reach its PDP;
- go Back;
- click the real cart control and reach the current cart;
- prove pending feedback appears immediately for both;
- record request counts and timings;
- preserve current cart lines, shipping configuration, recipient selections and user session;
- create no checkout/Order/reservation/payment;
- leave the normal primary storefront usable for the user.

No screenshots/images, new ports, alternate account/cart/DB, temp runtime, reset/reseed, PSP or Visual ACCEPT.
Checked-in gates remain unchanged; TB185 remains paused/default-OFF.

# Evidence and result contract

Write:

- `docs/architecture/evidence/TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245/TASK.md` verbatim;
- `RESULT.md`, `RECEIPTS.md`;
- `docs/ai/tasks/TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245/TASK.md`;
- all three recovery TOPs.

Lead the result with the measured root cause, before/after timing/request-count table, source repair, immediate
feedback behavior, actual canonical HTTP proof, exact final URL/runtime and SHAs.

Return exactly:

BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245
Parent-Task: TB-SHIPPING-ADDRESS-COMPLETION-PRESERVES-DELIVERY-CHOICE-244-R3
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Storefront — eliminate product/cart navigation stalls and add immediate pending feedback
Mode: MEASURE_AND_REPAIR_CANONICAL_STOREFRONT_NAVIGATION
Status: PASS | PARTIAL | BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: <resolved issue commit>
Base-SHA: <sha>
Code-SHA: <sha-or-NONE>
Evidence-SHA: <sha-or-NONE>
automaticNextImplementationTask: NONE
<full honest result>
END_TOOBA_WORKER_RESULT

Append the task-end marker separately. Never send placeholder/probe content.
END_TOOBA_TASK
