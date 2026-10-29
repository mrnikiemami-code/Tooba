# TB-STOREFRONT-NAVIGATION-AND-SHIPPING-CONTEXT-CLOSURE-245-R1

Parent: `TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245`  
Also closes the Architect blockers recorded for `TB-SHIPPING-ADDRESS-COMPLETION-PRESERVES-DELIVERY-CHOICE-244-R3`.  
Mode: `CLOSE_SELECTION_INVALIDATION_AND_NAVIGATION_PENDING_WITHOUT_NEW_RUNTIME`  
Expected base: `ISSUE_COMMIT_CONTAINING_THIS_TASK`  
Stop state: `STOP_FOR_ARCHITECT`  
Automatic next task: `NONE`

## Outcome

Finish the two user-visible defects on the existing canonical storefront:

1. Recipient/contact/postal/notes edits keep the selected delivery day/hour, while a real routing, cart, coupon, quote, price or calendar change invalidates exactly the stale confirmation.
2. Product/cart navigation acknowledges an ordinary click immediately, never breaks native modified-click behavior, never leaves a permanent pending state, and retains the measured PDP/cart latency improvement from 245.

This is one frontend closure task. Do not add a backend endpoint, migration, database write, seed, second runtime, new page, parallel navigation framework or new shipping engine.

## A — correct shipping-choice invalidation

The reviewed R3 implementation is only partial. `reconcileCapturedChoicesAgainstPreview(previous, preview)` currently checks group/method/date/slot only. It can retain a previous confirmation after coupon, cart version, snapshot, source revision, selection version, area, rate row, goods amount or quote changes when the same slot still exists. It also keeps non-null ticks when a refreshed day has zero slots.

Repair the existing selection state with explicit compatibility data:

- Recipient first/last name, contact mobile, postal address, postal code and notes are not routing/pricing identity and must not clear choices or issue a projection.
- Store the previous confirmed context/fingerprint next to the choices; do not infer the reason for a change from the new preview alone.
- A changed destination identity, coupon intent or cart version invalidates all captured choices after the fresh response arrives.
- Within an otherwise identical global context, a group keeps its choice only when its full server confirmation fingerprint is identical: group key, service/option, snapshot id, source revision, selection version, area group id, rate row id, exact goods amount, selected date and the authored slot ticks. A changed group fingerprint clears that group only.
- A technical re-fetch with the exact same global and group fingerprints preserves the same choices.
- If the refreshed day exposes slots, the chosen ticks must match one slot exactly. If it exposes zero slots, retention is lawful only when both prior slot ticks are absent; non-null prior ticks must be cleared.
- Never display or submit a stale card while a routing response is pending. Never silently rewrite the user's date/hour.
- Keep the server as authority; do not manufacture price, date, method, slot or confirmation tokens in the client.

Required focused RED→GREEN facts:

1. first/last/mobile/address/postal/notes preserve two groups and cause zero projection calls;
2. semantically identical re-fetch preserves both choices;
3. destination, coupon and cart-version changes clear all choices;
4. snapshot/source/selection/area/row/goods changes clear only the affected group;
5. date or slot removal clears only the affected group;
6. zero-slot day rejects a choice carrying ticks and accepts only a tickless legacy choice;
7. Continue blockers for incomplete recipient fields do not mutate choices.

Run one bounded canonical acceptance on FE `3000` → Host `5088`, SingleStore/store-alpha/tooba_alpha, using the existing actual buyer/cart only. Prove ordinary recipient completion preserves the chosen hours and one controlled pricing-context change requires reconfirmation. Do not submit checkout, Order or payment. Do not mutate merchant data merely to manufacture this proof; use a reversible request context such as coupon intent when available, otherwise rely on the exact focused source fact and disclose that live half.

Append an evidence correction for the R3 receipt buyer abbreviation: the corrected actual user id begins `01a0996c-b8d9-7000-9854-…`, not `...-8f5e-…`. Do not rewrite old evidence silently.

## B — close 245 navigation correctness

The reviewed 245 code has two concrete regressions:

1. `navPending.begin()` returns `false` for both modified/native clicks and duplicate ordinary clicks; `LocalizedLink` then calls `preventDefault()` for both. Ctrl/Cmd/Shift/Alt and middle-click are therefore blocked despite the stated requirement.
2. pending reconciliation observes only `usePathname`; same-path query/hash navigation or a failed/no-op navigation can leave the indicator and duplicate lock active until another route change.

Repair the existing pending provider/LocalizedLink instead of adding another coordinator:

- Use an explicit result (`native`, `started`, `duplicate`, `same-location`, or equivalent) so only a duplicate ordinary same-target activation is prevented. Modified clicks and middle-click must remain native, with no pending state and no `preventDefault`.
- Do not start pending for the already-current full target.
- Reconcile against pathname + search + hash. Cover `popstate` and `hashchange` without breaking App Router navigation.
- Add a finite failure/no-op timeout that clears the duplicate lock and pending bar, exposes a short localized FA/EN retry message, and allows the next click. No infinite spinner and no automatic navigation retry.
- All pending/timeout accessibility text must follow the active locale; remove the hardcoded Persian-only live-region text.
- Keep the 245 performance changes: PDP must not call full `/v1/storefront/home`, metadata/detail stays deduplicated, mini-cart remains a soft link, header inputs stay stable, and abortable fetch cleanup remains.

Required focused RED→GREEN facts:

1. ordinary click starts pending in at most 100 ms;
2. a second ordinary exact-target click while pending is the only click prevented;
3. Ctrl/Cmd/Shift/Alt and middle-click are not prevented and do not start pending;
4. same-current-target does not start pending;
5. pathname, query and hash completion each clear the matching pending target;
6. pop/back clears pending;
7. failed/no-op navigation times out, shows localized retry feedback and unlocks a later click;
8. FA and EN live-region strings are both asserted;
9. warm product and cart remain usable within 1 second on production `next start`; cold PDP/cart remain within 5 seconds; product click performs zero `/v1/storefront/home` calls.

## C — execution and preservation

- Work on current `main` only after fast-forwarding to the issue commit. One implementation commit plus evidence pin; no amend, force-push, reset, stash or history rewrite.
- Hard focus box: 30 minutes (source 18, proof 12). One correction round maximum. At cutoff, report exact completed facts and stop; do not widen scope.
- Run only the focused navigation/shipping-choice tests, `npm run typecheck`, production frontend build, and the two bounded warm/cold navigation measurements. Do not run broad backend suites.
- Use the canonical project/runtime already on ports 3000/5088. Do not create another FE/Host port, database, buyer or cart. Do not restart the backend unless it is actually unavailable; record any restart.
- Preserve the actual cart lines, quantities, merchant configuration, shipping draft, checked-in gates, TB185 and all user tabs/unsaved work. No checkout, Order, payment or PSP call.
- No screenshots or image artifacts. DOM/network/timing receipts only.
- Update `AGENTS.md`, `docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md`, `docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md`, and create `docs/architecture/evidence/TB-STOREFRONT-NAVIGATION-AND-SHIPPING-CONTEXT-CLOSURE-245-R1/{TASK.md,RESULT.md,RECEIPTS.md}`.

## Delivery report

Report exact base/code/evidence SHAs, tests and timings, modified paths, canonical runtime identity, cart preservation, before/after behavior for both defects, disclosed limits and process compliance. Worker PASS is not Architect or Visual ACCEPT. End with `STOP_FOR_ARCHITECT`; do not create an automatic next task.
