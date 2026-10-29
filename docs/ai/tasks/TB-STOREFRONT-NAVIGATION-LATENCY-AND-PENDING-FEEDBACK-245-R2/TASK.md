PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R2
Parent-Task: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Storefront — make navigation timeout feedback visible
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: VISIBLE_LOCALIZED_NAVIGATION_TIMEOUT_FEEDBACK_ONLY
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

# Accepted parent scope — do not reopen

Architect review accepts the 245-R1 shipping invalidation and navigation-state repairs within their measured scope:

- destination/coupon/cart-version changes clear all delivery choices;
- per-group server confirmation fingerprint changes clear the affected group;
- non-routing recipient fields preserve date/hour;
- zero-slot days reject stale ticks;
- native modified/middle clicks are not prevented;
- path/search/hash, pop/back and the five-second unlock are implemented;
- PDP/cart latency improvements remain measured.

Do not rewrite, widen or re-test those implementations except the focused regression needed to prove this visual correction.

# Exact remaining defect

At timeout, `retryHint` is populated and the duplicate lock is released, but `StorefrontNavigationPendingIndicator` renders the text only inside `<span className="sr-only">`. The container simultaneously has `h-0 overflow-hidden`. A sighted customer therefore sees the progress bar disappear with no explanation, contrary to the 245-R1 requirement to expose a short localized retry message and the user's report that clicks give no understandable feedback.

# Required repair

Use the existing pending provider and design system. Do not add another navigation coordinator, toast framework or route.

1. While pending, keep the existing immediate top progress indicator and localized live-region message.
2. On the five-second timeout:
   - clear pending and the duplicate-click lock exactly as R1 does;
   - render a compact, visible, non-blocking FA/EN message near the top of the viewport;
   - Persian: a natural equivalent of «باز شدن صفحه طول کشید؛ دوباره تلاش کنید.»;
   - English: a natural equivalent of “The page is taking too long to open. Try again.”;
   - preserve `aria-live`/status semantics;
   - do not cover navigation controls or intercept clicks.
3. The visible timeout message must not remain forever. Clear it on the next accepted ordinary navigation and auto-dismiss it after a short bounded interval (4–8 seconds). A modified/native click must not clear or create application pending state.
4. A later ordinary click must work immediately after timeout. No automatic navigation retry.
5. Keep FA/EN direction and the existing semantic color/design tokens. No hardcoded locale outside the existing locale helper.

# Focused proof

Add RED→GREEN facts proving:

- pending state shows the progress indicator and pending live text;
- timeout removes the progress indicator, unlocks navigation and renders visible FA text;
- EN locale renders visible EN text;
- timeout feedback auto-dismisses within the bounded interval;
- a new ordinary click clears the feedback and starts pending;
- modified/middle clicks neither create pending nor suppress native behavior;
- no second provider/coordinator is introduced.

Run only the focused navigation tests, the adjacent DOM/guard test, `npm run typecheck`, and one production build. One warm product click on canonical FE3000→Host5088 is enough to confirm that the progress indicator still appears promptly; do not force a real five-second network failure on the user's session if the source/DOM timer fact is exact.

# Preservation and timebox

- Frontend only. No backend, database, migration, merchant configuration, shipping state, cart mutation, checkout, Order, payment, PSP, new port, new buyer or new runtime.
- Canonical cart `01a17b2d-e219-7000-b3b2-5bcdbe6810c5` must remain Active v48 with zero checkouts.
- No screenshots or image artifacts.
- Hard focus box: 12 minutes, source 7 + proof 5. One correction round maximum. Stop honestly at cutoff.
- Update the task evidence folder and the three recovery TOPs. One implementation commit plus evidence pin; no amend/force/reset/rebase.
- Worker PASS is not Architect/Visual ACCEPT. End at STOP_FOR_ARCHITECT and create no automatic next task.

# Worker result delivery contract

Return exactly:

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R2
Parent-Task: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Storefront — make navigation timeout feedback visible
Mode: VISIBLE_LOCALIZED_NAVIGATION_TIMEOUT_FEEDBACK_ONLY
Status: PASS | PARTIAL | BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: <resolved issue commit>
Base-SHA: <sha>
Code-SHA: <sha-or-NONE>
Evidence-SHA: <sha-or-NONE>
automaticNextImplementationTask: NONE
<full honest result>
END_TOOBA_WORKER_RESULT
```

Append the task-end marker separately. Never send placeholder/probe content.
END_TOOBA_TASK
