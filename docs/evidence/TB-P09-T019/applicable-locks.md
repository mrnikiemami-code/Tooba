# Applicable locks — TB-P09-T019

Read `docs/architecture/TOOBA-LOCKS.md`.

Applied: LOCK-RET-001..007 (added this task), LOCK-QTY-001/004/005, LOCK-ROUND-001..003, LOCK-INVOICE-001..005, LOCK-OPS-001/004.

Return = مرجوعی; Refund = بازگشت وجه; independent lifecycles; immutable OrderLine snapshot; split-delivery quantity-aware clocks; post-payout compensating debit; cancellation refund without Return; one Return/Refund domain.
