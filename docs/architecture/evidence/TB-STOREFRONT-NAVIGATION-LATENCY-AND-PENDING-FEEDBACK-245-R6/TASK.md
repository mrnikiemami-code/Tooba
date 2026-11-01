PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R6
Parent-Task: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R5
Channel: tooba-main
Program: Tooba Storefront — remove stale shipping revalidation errors and identify fee groups
Mode: SEPARATE_REVALIDATION_ADVISORY_FROM_OPERATION_ERRORS_AND_KEEP_SUMMARY_COHERENT
Expected-Base-SHA: a70afe3c5a352c0ad233e6ac4632f504e39dda82
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

# Architect diagnosis

The user receipt proves a contradictory state: two current captured shipping rows are selected at `125000` each and the exact whole-cart shipping total is correctly `250000`, while the red page error still says another shipping selection is invalid.

Source confirms the owner defect in `storefront-shipping.tsx`: a successful projection with `page.revalidationMessage` imperatively copies that advisory into the same long-lived `error` state used for network/save failures. A later lawful method/date/slot choice updates `capturedChoices`, but does not clear or re-derive that old projection advisory. The page therefore shows a stale error beside a valid current summary.

The two fee rows also display only the shared method label («پیشتاز»), so the buyer cannot tell which store/group owns each `125000` fee.

# Required outcome

The shipping page must have one coherent current truth:

- if all current captured choices match the confirmed preview, the stale revalidation advisory disappears and the exact total remains visible;
- if a current group is genuinely incomplete or invalid, the whole-cart total is not presented as valid and the owning group/store is named;
- network/save errors remain visible until their own lawful clear point and are never accidentally erased by selecting a method.

# A — separate error ownership

- Stop copying `page.revalidationMessage` into the generic operation/network `error` state.
- Model projection revalidation as a distinct typed/derived advisory owned by the current projection identity.
- A projection advisory may be shown only while it still describes the current unresolved captured selection.
- Recompute/suppress it from the current confirmed preview + reconciled choices. When every group has a current lawful method/date/slot, the old advisory must disappear without requiring another HTTP read or page reload.
- A new destination/coupon/cart/quote identity may create a new advisory only from its own response; stale generations cannot install or retain one.
- Do not clear unrelated network, projection-fetch, selection-save or checkout errors merely because the user clicks a method/date/slot. Keep their existing ownership and retry behavior.

# B — summary/error coherence

- Reuse the R5 exact captured summary and existing `resolveShippingContinueState`; do not add another validity engine.
- `ready` exact summary + complete current captured intent must never coexist with a stale “selection invalid” banner.
- `partial`, `hidden` or `unavailable` must never display the whole-cart shipping/payable as valid.
- Recipient/address incompleteness remains a Continue blocker only and must not become a red shipping-selection error.
- If a captured group becomes invalid after fingerprint reconciliation, identify that group with the current seller/store label and direct the buyer to reselect only that group.

# C — identify fee rows

- Extend the pure R5 projection row with stable group ownership needed for presentation (`groupKey` and authoritative `sellerPartyId`; no label matching).
- Resolve the visible seller/store label from the current cart lines by seller identity.
- Render each row as `<seller/store label> — <method label>`; two groups sharing «پیشتاز» must remain distinguishable.
- If a display label is unavailable, use a short localized group fallback; never expose raw GUIDs.
- Arithmetic stays exactly as accepted in R5: `125000 + 125000 = 250000`; carrier collect remains separate.

# D — focused RED→GREEN facts

Add exact facts for:

1. projection returns a revalidation message, then both current groups are lawfully reselected: banner disappears, rows and `250000` total remain;
2. one group remains invalid: total/payable are not shown as complete and the owning seller/group is named;
3. two equal «پیشتاز» rows render different seller/store labels;
4. recipient-name blocker can coexist with valid money but no red shipping-selection banner;
5. network/fetch/save errors are not cleared by local selection changes;
6. stale projection generation cannot restore an old advisory;
7. destination pending/failed still hides money and preserves the accepted retry state;
8. R5 exact decimal/carrier-collect tests and 244-R3/245-R3 choice-retention tests remain green.

Run only focused frontend tests, `npm run typecheck`, and one production frontend build.

# E — canonical practical acceptance

Use only canonical FE `http://127.0.0.1:3000` → Host `http://127.0.0.1:5088`, SingleStore / `store-alpha` / `tooba_alpha`, actual cart `01a17b2d-e219-7000-b3b2-5bcdbe6810c5` Active v48 quantity 18.

Reproduce without submitting:

1. reach the old red revalidation advisory;
2. select the lawful current method/date/hour for both groups;
3. prove the red advisory disappears;
4. prove fee rows show two distinct seller/store labels, each `125000`, and shipping total `250000`;
5. leave recipient name incomplete and prove only the recipient blocker remains;
6. invalidate only one group through a current lawful UI identity change and prove the total is withheld and that group is named.

No selection PUT, checkout, Order, payment, merchant configuration, database write, screenshot or image artifact.

# Preservation and process

- Frontend only.
- Preserve R5 exact monetary arithmetic, R3 destination reconciliation, R4 bounded cart navigation and configured delivery slots.
- No alternate runtime/port/buyer/cart/database. Checked-in gates stay OFF; TB185 stays paused/default-OFF/PendingAllocation.
- Hard focus box: 15 minutes; one correction round maximum; stop honestly at cutoff.
- Update evidence and all three recovery TOPs. One implementation commit plus evidence pin; no amend/force/reset/rebase.
- Worker PASS is not Architect/Visual ACCEPT. End `STOP_FOR_ARCHITECT`; automatic next task `NONE`.

# Worker result delivery contract

Return exactly:

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R6
Parent-Task: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R5
Channel: tooba-main
Worker-Id: <worker>
Agent-Type: cursor
Program: Tooba Storefront — remove stale shipping revalidation errors and identify fee groups
Mode: SEPARATE_REVALIDATION_ADVISORY_FROM_OPERATION_ERRORS_AND_KEEP_SUMMARY_COHERENT
Bridge-Task-Row: <row-id>
Status: <PASS|PARTIAL|BLOCKED>
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: a70afe3c5a352c0ad233e6ac4632f504e39dda82
Base-SHA: <sha>
Code-SHA: <sha-or-NONE>
Evidence-SHA: <sha-or-NONE>
automaticNextImplementationTask: NONE
<full honest result>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
```

Never send placeholder/probe content.
END_TOOBA_TASK
