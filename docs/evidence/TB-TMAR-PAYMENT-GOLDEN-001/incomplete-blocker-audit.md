# Incomplete blocker audit — TB-TMAR-PAYMENT-GOLDEN-001

## Why INCOMPLETE

Financial-critical Host surfaces remain:

1. `StorefrontPaymentComposer` (~27KB) depends on Host checkout/session, `Wallet.Application`, `Inventory.Application`, `Media.Application`, Payment Infrastructure — cannot move wholesale into Application without Contracts gates.
2. Storefront payment HTTP + `MapPaymentException` prose heuristics still in Host.
3. Admin payment HTTP + `AdminPaymentsGridQueryEngine` (OrderDbContext) still in Host.
4. Webhook endpoint still Host with Infrastructure validator/options.
5. Reconciliation worker still Guid.NewGuid / UtcNow / raw StartActivity.

## Scaffold present (not PASS)

- `Tooba.Payment.Application/Errors/PaymentErrorCodes.cs`
- `Tooba.Payment.Application/Errors/PaymentExceptionMapper.cs`
- `Tooba.Payment.Endpoints/` empty project folders (not in slnx)

## R1 required

Finish Endpoints + MediatR CQRS for webhook/storefront/admin/grid/reconcile with Contracts-only foreign boundaries; preserve payment state transitions exactly.
