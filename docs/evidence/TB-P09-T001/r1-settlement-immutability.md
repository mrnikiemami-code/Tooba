# R1 — Settlement immutability

## Runtime

Before Adjust (Credit only):

- `entry_id=63aca581-f470-417f-8042-1cd231062d33`
- gross `381500.0000`, commission `38150.0000`, net `343350.0000`
- idempotency `payment-accrual:88ddad779a7f4e518ed912e45ecbf791:01a0451cfac170009371f9d9ebb05855`

After Adjust + retry: **same credit row** (id/amounts/idempotency unchanged). Debit is additive (`EntryType=Debit`), not a rewrite of the credit.

## Automated

`SettlementFoundationTests.Settlement_lifecycle_applies_commission_refund_and_payout_safety` asserts statement list unchanged across Adjust retry; credit count stays 1.

`SellerOrderCancellationGuardTests.Evaluator_source_never_references_settlement` — eligibility evaluator has no Settlement coupling.
