PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R5
Parent-Task: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R4
Channel: tooba-main
Program: Tooba Storefront — show selected captured shipping cost immediately and exactly
Mode: REPAIR_CAPTURED_SHIPPING_SUMMARY_WITHOUT_WEAKENING_CHECKOUT_GUARDS
Expected-Base-SHA: caf49ef762a04c8bb83a25a86a1294e7b81b0c57
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

# Architect diagnosis

The attached user receipt shows a real regression on the canonical shipping page: the buyer selected the configured delivery method/day/hour, but «هزینه ارسال» and «قابل پرداخت» remain `—`. The recipient name is still incomplete, and that correctly blocks Continue, but it must not suppress the already-authoritative selected shipping price.

Source confirms the cause in `app/storefront/storefront-shipping.tsx`: when `capturedPreviewActive` is true, the summary hard-codes both shipping and payable to `—` and renders a note saying the final amount is calculated only after completion. This ignores the exact server amounts already present on the current captured preview (`finalGoodsAmountText` and `payableAtCheckoutAmountText`).

# Required outcome

On the existing captured flow, selecting a server method must update the order summary immediately from the current confirmed server response. Incomplete recipient/address fields may keep Continue disabled with their owning message, but must not blank a lawful selected shipping amount.

# A — one exact captured-summary projection

- Add one pure, typed summary projector for the captured path; do not scatter arithmetic across JSX.
- Input must be only the **current confirmed** captured preview and `capturedChoices`.
- Match each choice to exactly one group and exactly one method by the existing canonical service/option method key. Never match by label, array position or seller display name.
- Use the exact decimal wire strings already decoded by `storefront-shipping-api.ts`:
  - group goods = `finalGoodsAmountText`;
  - shipping payable now = selected method `payableAtCheckoutAmountText`;
  - carrier collect remains separate and must not be added to checkout payable;
  - currency must be present and coherent for every included group/method.
- Implement exact decimal addition without passing monetary values through JavaScript floating point. Preserve arbitrary lawful scale and long values; normalize only for display. No `Number(...)`, `parseFloat`, exponent acceptance or rounding invention.
- A missing/invalid/mismatched amount or currency must fail closed for the affected total; never display zero or a stale prior amount.

# B — honest progressive summary behavior

- As soon as a group has a current authoritative method choice, show that group’s selected shipping fee in the summary.
- When every current captured group has a lawful selected method, show:
  - total shipping = sum of all selected `payableAtCheckoutAmountText` values;
  - payable = sum of all current group `finalGoodsAmountText` values + total shipping;
  - free shipping as «رایگان» only when the exact total is zero.
- Recipient name, mobile, postal code, street address and notes must not participate in captured price visibility.
- Missing day/hour may still block Continue, but method price remains visible because the server method already owns the fee.
- If only some groups are selected, show their individual selected fee rows and a short localized “complete all delivery methods for the total” state; do not present a partial amount as the whole-cart total.
- While destination/quote revalidation is pending or failed, retain the accepted 245-R3 behavior: cards may remain visible, but confirmed money must be hidden/marked as recalculating until the fresh fingerprint is current. Never reuse stale summary money.
- Replace the incorrect existing note that says price is calculated only after final completion. The note must explain only the actual missing condition, if any.

# C — keep checkout validity separate

- `resolveShippingContinueState` remains the sole authority for enabling Continue and explaining blockers.
- Do not make Continue valid merely because prices are visible.
- The user receipt’s expected state is: exact shipping/payable visible, Continue disabled, and blocker «نام و نام خانوادگی گیرنده را کامل کنید.»
- Do not auto-save selection, navigate, mutate cart, or create checkout/payment merely to update the summary.

# D — focused RED→GREEN facts

Add exact frontend facts for:

1. one captured group: selected method immediately exposes its exact shipping and payable while recipient is incomplete;
2. two groups: each selected row remains independent and whole-cart totals appear only after both methods are selected;
3. incomplete recipient/contact/postal fields do not erase the selected amounts and still block Continue with the same owning reason;
4. missing date or slot blocks Continue but does not hide the selected method fee;
5. pending/failed destination refresh never shows stale money;
6. changed snapshot/rate/goods fingerprint removes only the invalid affected group amount until reselected/current;
7. exact decimal addition covers long tokens, different lawful decimal scales, zero, and rejects signs/exponents/blank/invalid currency;
8. carrier collect is displayed separately if already supported, and is never added to checkout payable;
9. legacy non-captured summary behavior is unchanged;
10. accepted 244-R3/245-R3 selection retention and 245-R4 navigation focused guards stay green.

Run the focused summary/selection/continue-state tests, `npm run typecheck`, and one production frontend build. No broad backend suite.

# E — canonical practical acceptance

Use only the canonical environment:

- FE `http://127.0.0.1:3000` → Host `http://127.0.0.1:5088`;
- SingleStore / `store-alpha` / `tooba_alpha`;
- actual buyer/cart `01a17b2d-e219-7000-b3b2-5bcdbe6810c5`, expected Active v48, quantity 18, zero checkouts.

Reproduce the user sequence without submitting:

1. leave first/last name incomplete;
2. choose the lawful server method, date and configured hour for every current group;
3. record DOM/text facts proving selected group fee rows, shipping total and payable update immediately;
4. prove Continue remains disabled and the recipient-name blocker remains visible;
5. complete the recipient fields and prove the amounts do not change or disappear and Continue becomes eligible only when every existing requirement is complete.

No selection PUT, checkout, Order, payment, merchant configuration, migration, seed, database write, screenshot or image artifact.

# Preservation and process

- Frontend only unless a measured contract defect makes that impossible.
- Preserve 245-R1/R2 navigation semantics, 245-R3 destination reconciliation, 245-R4 bounded cart reads, hourly slots and exact selection echo tokens.
- No alternate runtime, port, buyer, cart or database.
- Checked-in gates stay OFF; TB185 remains paused/default-OFF/PendingAllocation.
- Hard focus box: 20 minutes (source 12, proof/live 8); one correction round maximum; stop honestly at cutoff.
- Update the evidence folder and all three recovery TOPs. One implementation commit plus evidence pin; no amend, force-push, reset or rebase.
- Worker PASS is not Architect/Visual ACCEPT. End `STOP_FOR_ARCHITECT`; automatic next task `NONE`.

# Worker result delivery contract

Return exactly:

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R5
Parent-Task: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R4
Channel: tooba-main
Worker-Id: <worker>
Agent-Type: cursor
Program: Tooba Storefront — show selected captured shipping cost immediately and exactly
Mode: REPAIR_CAPTURED_SHIPPING_SUMMARY_WITHOUT_WEAKENING_CHECKOUT_GUARDS
Bridge-Task-Row: <row-id>
Status: <PASS|PARTIAL|BLOCKED>
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: caf49ef762a04c8bb83a25a86a1294e7b81b0c57
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
