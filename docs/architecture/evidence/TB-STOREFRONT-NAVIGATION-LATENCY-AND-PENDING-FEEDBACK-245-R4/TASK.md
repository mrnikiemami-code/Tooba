PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R4
Parent-Task: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R3
Channel: tooba-main
Program: Tooba Storefront — finish cart navigation responsiveness and prove bounded state flow
Mode: REMOVE_CART_HOME_CRITICAL_LOAD_AND_ADD_ACTION_LEVEL_PENDING_FEEDBACK
Expected-Base-SHA: 76d9c4a7743e8ca7f95c0b8d39bec2404be9b1da
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

Architect review and measured defect

245-R3 is ACCEPTED within its issued scope: province/city now has one projection owner, no document reload, compatible delivery choices remain provisional and are reconciled by the fresh server fingerprint, while Continue/PUT stays fail-closed. Do not reopen or weaken that path.

The remaining user-visible defect is the cart navigation path. Current source shows:

app/cart/page.tsx blocks the route on the full loadStorefrontHome() response only to obtain shell categories, search data and recommendations. The product route already uses the lightweight loadStorefrontCategories() path, but cart does not.
The cart client starts its cart, pending-payment and login reads only after the blocked RSC route arrives.
The mini-cart CTA now uses LocalizedLink, but the only immediate acknowledgment is the global one-pixel top bar. The drawer remains visually unchanged until route commit, so the user cannot tell that «تکمیل خرید / نمایش سبد» was clicked.
Header, mini-cart and cart each own bounded cart reads. No infinite state loop is proven, but request ownership and cleanup must be measured so a hidden event/effect cascade is not left behind.
Required outcome

Opening a product and opening the cart must both acknowledge the click immediately and become usable without waiting for unrelated Home merchandising. Cart navigation must have bounded renders, effects and requests with no feedback/event loop.

A — remove Home from the cart critical path
app/cart/page.tsx must not await loadStorefrontHome() before the cart route can render.
Reuse the existing lightweight loadStorefrontCategories() for the shell, as the accepted PDP path does.
The live cart content, cart loading shell and header must become usable before recommendation/merchandising data.
Preserve cart recommendations as non-blocking enhancement: load or stream them only after the primary cart is usable, with one owner, cancellation/cleanup and a quiet failure. Do not silently delete the feature and do not introduce a second Home read.
Do not add a new backend endpoint unless measurement proves no existing lightweight read can supply the needed data. No mock/static recommendations.
Header search/menu behavior must remain lawful; do not block the cart again merely to prefill optional search suggestions.
B — immediate visible feedback on the actual cart actions
Header cart icon must open the existing mini-cart immediately and retain its current loading/error state.
An ordinary primary click on the mini-cart cart CTA must show action-level feedback within 100 ms: visible localized label/spinner and aria-busy, while the existing global pending bar remains available.
Duplicate same-target ordinary clicks during the same navigation are suppressed once; modified/middle/new-tab clicks retain native browser behavior.
Do not leave the drawer looking idle while navigation is active. On successful route transition, the existing cart loading.tsx/cart page becomes the visible owner. On timeout, the accepted R2 visible localized retry banner remains intact.
Back/forward, hash/query and same-location behavior from 245-R1/R2 must not regress.
C — prove bounded state and request ownership

Inspect and measure, do not guess:

cart route mount: at most one cart read, one pending-payment read and one checkout-login decision from the cart owner;
mini-cart open: at most one initial cart read; CART_CHANGED_EVENT causes at most one refresh per mounted owner and must not emit another CART_CHANGED_EVENT itself;
header badge: one initial read and one read per real cart/auth event, with stale-result protection and listener cleanup;
recommendation enhancement: one request per cart route mount, started after primary cart usability, cancelled/ignored after unmount;
no effect dependency on state that the same effect unconditionally rewrites; no render → event → fetch → render feedback cycle; no accumulating listeners/timers across repeated product ↔ cart navigation;
if request coalescing is needed, implement it in the existing storefront cart/read boundary with a bounded in-flight lifetime and no stale cross-user cache.

Do not respond with source-text claims alone. Add deterministic counters/facts that would fail if an effect or event loop were introduced.

D — focused RED→GREEN proof

Add exact facts for:

cart page critical source no longer calls/awaits loadStorefrontHome;
lightweight categories feed the shell;
recommendations start only after the primary cart is usable and failure does not replace/hide the cart;
mini-cart CTA enters visible busy feedback before router navigation and clears on route/timeout;
duplicate ordinary click does not produce a second navigation; modified/middle click remains native;
one mount/open/event produces the bounded request counts in section C;
unmount removes listeners and late results cannot install state;
three consecutive product → mini-cart → cart → product cycles do not increase listeners, timers or per-cycle request counts;
accepted 245-R2 timeout and 245-R3 shipping destination/choice focused facts stay green.

Run only focused frontend tests, npm run typecheck and one production build. Do not run broad backend suites.

E — canonical practical acceptance

Use only the canonical runtime and actual user environment:

FE http://127.0.0.1:3000 → Host http://127.0.0.1:5088;
SingleStore / store-alpha / tooba_alpha;
actual buyer and cart 01a17b2d-e219-7000-b3b2-5bcdbe6810c5, expected Active v48, quantity 18, zero checkouts.

Measure both paths:

product card → PDP;
header cart icon → mini-cart → cart CTA → /fa/cart.

For warm and one cold-process cart navigation record:

click-to-visible-pending (target ≤100 ms);
click-to-cart-loading-shell and click-to-primary-cart-usable;
document path, beforeunload count and hard reload count;
RSC/API request ledger until primary cart usable, explicitly proving no /v1/storefront/home in the critical interval;
later recommendation request separately, if retained;
bounded render/effect/listener/request counts across three navigation cycles.

Do not mutate the cart, shipping selection, checkout, Order, payment, merchant configuration or database. No screenshots or image artifacts.

Preservation and process
Frontend only unless a measured contract defect is unavoidable.
Preserve 245 PDP optimization, native click semantics, visible timeout feedback and the accepted 245-R3 destination/selection behavior.
No new runtime, port, buyer, cart, database, seed or merchant data.
Checked-in gates remain OFF; TB185 remains paused/default-OFF/PendingAllocation.
Hard focus box: 25 minutes (source 15, focused proof/live 10); one correction round maximum; stop honestly at cutoff.
Update the evidence folder and all three recovery TOPs. One implementation commit plus evidence pin; no amend, force-push, reset or rebase.
Worker PASS is not Architect/Visual ACCEPT. End STOP_FOR_ARCHITECT; automatic next task NONE.
Worker result delivery contract

Return exactly:

Plain text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R4
Parent-Task: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R3
Channel: tooba-main
Worker-Id: <worker>
Agent-Type: cursor
Program: Tooba Storefront — finish cart navigation responsiveness and prove bounded state flow
Mode: REMOVE_CART_HOME_CRITICAL_LOAD_AND_ADD_ACTION_LEVEL_PENDING_FEEDBACK
Bridge-Task-Row: <row-id>
Status: <PASS|PARTIAL|BLOCKED>
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 76d9c4a7743e8ca7f95c0b8d39bec2404be9b1da
Base-SHA: <sha>
Code-SHA: <sha-or-NONE>
Evidence-SHA: <sha-or-NONE>
automaticNextImplementationTask: NONE
<full honest result>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK

Never send placeholder/probe content.
END_TOOBA_TASK