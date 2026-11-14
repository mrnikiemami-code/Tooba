# TB-265 audit correction — fixture construction vs. paid-path proof

Appended by `TB-SHIPPING-PAYMENT-GATED-DISPATCH-READABLE-QUEUE-AND-CLEAN-TRIAL-DATA-266` (2026-11-14).
This note does not delete or rewrite any earlier 265/265-R2 claim; it corrects what those claims did **not** prove.

## What was previously implied

The 265 and 265-R2 result contracts stated that no raw business SQL and no non-production construction was used to stand up the courier-origin return scenarios, and that the resulting relational proofs exercised the real paid path. The user's trial rejection on 2026-11-14 exposed that reading as too strong.

## What is actually true (source re-read at task start)

1. The 265/265-R2 relational helpers created the `Paid` seller order by **direct SQL** against `"order".seller_orders` / related rows, inserted a checkout **without** an owning payment confirmation, then constructed `FulfillmentUnit.CreateFromPaidOrder(...)` in-process and dispatched from that aggregate.
2. Consequently those helpers proved the **aggregate and the closure policy**, but they did **not** prove that an ordinary unpaid checkout becomes dispatchable, and they did not prove that the paid state came from the owning payment path.
3. The controlled/`fake` payment reader used in those runs answered "paid" to the aggregate while the canonical runtime's real payment state was unrelated. That is a fixture-construction property, not evidence of payment authorization.
4. The TB-266 live observation is the counter-check: an unpaid checkout on the canonical DB has **no** fulfillment, projects only `cancel`, and any physical command is refused before any effect.

## What was not wrong

- The durable Order closure, the single source claim, the serialized approve/reject decision, the partial-source conservation and the condition-aware disposition introduced by 265/265-R1/265-R2 are unchanged and remain valid at their own seams.
- No fake `Paid` fact was introduced into the canonical runtime by this task, and no earlier fixture was relabelled as paid or delivered.

## Consequence for future work

Any future claim about payment-gated supply must come from (a) an owning payment confirmation producing the paid state, or (b) a live negative observation plus a command-level refusal test — never from a factory that receives `Paid` by construction. The guard added in TB-266 (`fulfillment.order.payment_not_authorized` read from `IOrderFulfillmentReader.GetHandoffAsync(...).IsPaid`) is the single place where that distinction is now enforced.
