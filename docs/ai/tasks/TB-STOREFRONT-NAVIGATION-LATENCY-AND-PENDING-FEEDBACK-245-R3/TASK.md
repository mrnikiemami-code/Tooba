PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R3
Parent-Task: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Storefront — preserve delivery choice across province/city completion without duplicate refresh
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: REMOVE_DUPLICATE_DESTINATION_REFRESH_AND_SERVER_REVALIDATE_EXISTING_CHOICE
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

User-reported regression

On the canonical shipping page the user can choose a delivery day/hour first. When province is selected afterward, the page appears to reload and both choices disappear.

This is reproduced conceptually by the current source and is not closed by 244-R3/245-R1:

onProvinceChange updates address state and directly calls refreshProjection(provinceName, methodCode).
The destination coordinator effect observes the same state change and calls refreshProjection again with the tracked destination identity.
The current routing identity treats every destination-key change as an unconditional clear-all, so the chosen day/hour is erased before the new authoritative response can prove whether it is still valid.
Pending projection hides/empties cards, which makes the client re-render look like a full browser refresh.
Required outcome

The user may fill recipient/address fields and choose delivery in either order. Selecting province/city must not cause a document reload, duplicate projection, full-page flicker or unconditional loss of a still-valid server choice.

Safety remains fail-closed: no stale choice may be submitted while destination validation is pending, failed or incompatible.

A — one destination refresh owner
Remove the direct legacy refreshProjection call from the province change handler. Province selection updates address state only.
The existing destination-refresh coordinator is the single owner of projection refresh for province/city/destination identity.
Province and city transitions must each produce at most one projection request for their final identity. Superseded answers cannot install state.
Do not add debounce timers, a second coordinator, router refresh, location reload or form submission.
Prove the browser document/navigation identity does not change: no beforeunload, hard navigation, pathname change or remount of the shipping-page root.
B — preserve provisionally, revalidate authoritatively

Separate these concepts:

hard-invalidating context: coupon intent and cart version; a change clears all choices;
destination revalidation: province/city/destination identity; retain the user's choices provisionally while the fresh server response is pending;
group compatibility: after the fresh response, keep a group only when its full confirmation fingerprint still matches the authoritative response (group, service/option, snapshot, source revision, selection version, area, rate row, exact goods amount, selected date and authored slot ticks).

Behavior:

Province/city selection must not immediately discard choices or fingerprints.
While the destination response is pending or failed, keep the cards and chosen values visible but mark them unconfirmed/disabled. Continue and selection PUT remain blocked. Never show stale money as confirmed.
If province is selected while city is still empty, keep the provisional choices through that intermediate incomplete destination. Do not erase them merely because the temporary response has no groups.
When city/destination becomes complete and the fresh response arrives:
exact compatible group fingerprint ⇒ retain that group's day/hour;
changed/missing method, area, rate, goods, date or slot ⇒ clear only that group and show its existing localized reconfirmation blocker;
changed coupon or cart version ⇒ clear all groups.
A failed destination refresh keeps the provisional UI choice visible but unusable and exposes the existing explicit retry. Successful retry performs the same fingerprint reconciliation.
Recipient name, mobile, postal address, postal code and notes remain projection-free and preserve choices.
Do not manufacture rates, dates or slots. Server response remains the only authority.
C — focused RED→GREEN proof

Add exact facts for:

province handler changes state and makes zero direct projection calls;
coordinator makes exactly one projection for the province identity and exactly one for the later city identity;
no hard navigation, pathname change or shipping-root remount occurs;
two selected groups stay visibly selected but unconfirmed during destination pending;
province-only intermediate state does not erase provisional choices;
completed destination with identical fingerprints restores/keeps both choices;
changed rate/area/goods/slot clears only the affected group;
coupon or cart-version change clears all;
pending/failed destination cannot build selection intent or enable Continue;
explicit retry succeeds without duplicate request and then reconciles;
non-routing recipient fields still make zero projection calls and preserve choices.
D — canonical practical acceptance

Use only the existing canonical FE 3000 → Host 5088, SingleStore/store-alpha/tooba_alpha and the actual buyer/cart:

cart 01a17b2d-e219-7000-b3b2-5bcdbe6810c5, expected Active v48, quantity 18, zero checkouts;
choose a lawful day/hour for both groups before province/city completion;
select province, then city;
record document/navigation identity, shipping-root mount count and projection request count;
prove the page does not hard reload, each identity issues one request, and compatible choices survive;
prove Continue remains blocked during pending and becomes enabled only after authoritative compatibility;
stop at payment-ready/selection at most. Do not submit checkout, Order or payment.

If the actual current server response changes a fingerprint, do not force retention or mutate merchant data. Record the lawful affected-group clear and use the focused identical-response fact for preservation.

Preservation, bounds and process
Frontend only unless a measured server contract defect is unavoidable; do not change backend/data/migrations for convenience.
No new runtime, port, buyer, cart, database, seed or merchant configuration.
No screenshots or image artifacts.
Preserve the accepted 245 navigation performance, native clicks and visible timeout banner.
Checked-in gates, TB185, cart lines/quantities and user tabs/unsaved work remain untouched.
Hard focus box: 25 minutes (source 15, proof/live 10), one correction round maximum. Stop honestly at cutoff.
Focused tests + typecheck + production build only; no broad backend suites.
Update the evidence folder and three recovery TOPs. One implementation commit plus evidence pin; no amend/force/reset/rebase.
Worker PASS is not Architect/Visual ACCEPT. End STOP_FOR_ARCHITECT; automatic next task NONE.
Worker result delivery contract

Return exactly:

Plain text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R3
Parent-Task: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Storefront — preserve delivery choice across province/city completion without duplicate refresh
Mode: REMOVE_DUPLICATE_DESTINATION_REFRESH_AND_SERVER_REVALIDATE_EXISTING_CHOICE
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