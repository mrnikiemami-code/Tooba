# focused-financial-tests

Task: TB-TMAR-NEXT-MODULE-BATCH-002-R1

## Required coverage map

| Requirement | Coverage |
|-------------|----------|
| Wallet order-payment idempotent replay | **NEW** `WalletFinancialCharacterizationTests.Order_payment_debit_is_idempotent_on_replay` |
| Wallet refund-credit idempotent replay | **NEW** `WalletFinancialCharacterizationTests.Refund_credit_is_idempotent_on_replay` |
| Wallet insufficient balance rejection | **NEW** `WalletFinancialCharacterizationTests.Insufficient_balance_rejects_order_payment_debit` |
| Wallet notification — payment flow | **NEW** `...Payment_success_emits_wallet_payment_notification_semantics` |
| Wallet notification — refund flow | **NEW** `...Refund_credit_emits_wallet_refund_notification_semantics` |
| Payment → Wallet gateway amount/currency/customer/payment/idempotency | **NEW** `PaymentFinancialCharacterizationTests.Wallet_gateway_*` |
| Payment webhook idempotency / duplicate handling | **NEW** `...Webhook_inbox_record_dedup_key_is_provider_plus_event_id` (+ handler uses same key; Host SkippableFact retains full path when Docker available) |
| Payment lifecycle success/failure | **NEW** `...Payment_lifecycle_success_and_failure_transitions` (+ existing `CustomerPaymentFactoryBehaviorTests`) |

## Cite / reuse

- Host `WalletCheckoutRefundTests.Wallet_spend_verify_and_refund_are_ledger_safe` (SkippableFact, Docker) — broader ledger+gateway+notification integration; not duplicated as Host suite here.
- Existing architecture guards retained and strengthened.

## Results (this repair)

- `dotnet test Tooba.Wallet.Tests` — **13 passed** (8 prior + 5 financial + Notification Contracts guards)
- `dotnet test Tooba.Payment.Tests` — **9 passed** (5 prior + 4 financial characterization)
