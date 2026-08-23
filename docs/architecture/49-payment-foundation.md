# Tooba — Payment Foundation

Status:

```text
IN_PROGRESS — TB-P03-T008 awaiting Architect ACCEPT
```

Task:

```text
TB-P03-T008
```

## Purpose

Payment records verified money movement. Order records commercial obligation. A gateway provider is an adapter. Card data is never stored.

```text
Order != Payment
Payment != Gateway Provider
Payment != Card Storage
callback success text != verified payment success
client cannot choose payment amount
```

## Ownership

Module `Payment` owns `PaymentDbContext`, schema `payment`, migrations, and the write model.

Order is reached only through application contracts:

- `IPayableCheckoutReader` — amount/currency/mode from the Order snapshot
- `IOrderPaymentProjection` — mark eligible `OnlinePurchase` seller orders Paid after verified success

Payment Infrastructure does not open `OrderDbContext`. Order Infrastructure does not open `PaymentDbContext`. No cross-module FK.

## Aggregate

`CustomerPayment` is the customer-facing payment for a checkout group. Identity is internal `PaymentId`, not the provider transaction id.

Lifecycle: `Created` → `Pending` (after initiate) → `Succeeded` | `Failed` | `Cancelled` | `Expired`.

`PaymentAttempt` is durable history. Initiate does not overwrite prior attempts. Succeeded is set only after `IPaymentGateway.VerifyAsync` returns verified evidence.

## Amount integrity

`InitiatePaymentCommand` has no amount field. The directory sums seller-order payable snapshots. Currency must match the checkout snapshot. No FX in Payment.

## OnlinePurchase vs RequestToReserve

`OnlinePurchase` may create a Payment. `RequestToReserve` must not require Payment at initial submit. Verified payment does not mark reservation orders Paid.

## Gateway

`IPaymentGateway` / `IPaymentGatewayRegistry` are provider-neutral. This task ships `FakePaymentGateway` and `FakeFailingPaymentGateway` only. Real PSP SDKs are out of scope.

`VerifyAsync` ignores `callbackClaimsSuccess`. A `-FAIL-VERIFY` request reference fails verification even when the callback claims success.

## Idempotency and uniqueness

Initiate is keyed by `IdempotencyKey`. Duplicate verify of an already-Succeeded payment is a no-op (`NewlySucceeded = false`). `ProviderTransactionReference` is uniquely constrained when present.

## Order projection

Paid Order state is a recoverable projection. After Verify, Payment persists locally and writes Outbox `payment.succeeded.v1`. An Order-owned consumer applies Paid and records a durable inbox row in the same Order transaction. In-process projection after Payment `SaveChanges` is not a source of truth and is not used. Duplicate delivery is ignored by inbox EventId. Amount/currency mismatch must not mark Paid.

## Multi-seller allocation

One customer payment may cover several seller orders. `PaymentAllocation` snapshots `SellerOrderId`, `AllocatedAmount`, and `Currency`. Sum must equal the payment amount. This is not settlement or payout.

## Tenant isolation

Payment rows live in the tenant/edition database resolved by existing commerce connection seams. Tenant A cannot load or verify Tenant B payments.

## Out of scope

Real bank/PSP, PAN/CVV vault, refund/capture/settlement/payout, commercial UI, T009, P03 Gate.
