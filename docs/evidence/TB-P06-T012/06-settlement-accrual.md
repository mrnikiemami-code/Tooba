# 03 — Post-payment accrual contract (TB-P06-T012)

## Trigger

Integration event: `PaymentSucceededIntegrationEvent` (`payment.succeeded.v1`)

Handler: `SettlementPaymentSucceededHandler` → `SettlementDirectory.AccrueFromPaymentAsync`

## Accrual flow

1. Inbox dedupe on `eventId` (`settlement.payment_inbox`)
2. Load payment snapshot via `ISettlementPaymentReader` — must be Succeeded
3. Resolve default commission policy (10% marketplace default)
4. For each distinct `sellerOrderId` in event payload:
   - Idempotency key: `payment-accrual:{paymentId}:{sellerOrderId}`
   - Skip if entry with key already exists
   - Load allocation amount from Payment allocations
   - Verify seller order is Paid via `ISettlementOrderReader`
   - Post `SettlementEntry.PostCreditFromPayment` with commission snapshot
5. Record inbox + save

## Entry math

```
commissionAmount = round(gross * policyRate, 4, AwayFromZero)
netAmount        = gross - commissionAmount
```

Default policy: `marketplace-default-10pct`, rate `0.10`.

## Marketplace gating

Handlers registered only when `Tooba:Edition == Marketplace` (`SettlementModule.IsMarketplaceEdition`).

## Idempotency surfaces

- Event inbox (`event_id` PK)
- Per-order entry idempotency key
- Safe to replay `payment.succeeded.v1`
