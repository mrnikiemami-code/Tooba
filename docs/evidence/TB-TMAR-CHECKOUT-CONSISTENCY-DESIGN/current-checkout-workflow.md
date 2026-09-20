# Current checkout workflow — reconstructed from code

Primary source: `CheckoutDirectory.SubmitAsync` (Order.Infrastructure).

## Entry points
1. Host `POST /v1/storefront/checkout` → StorefrontCheckoutComposer → ICheckoutDirectory.SubmitAsync
2. StorefrontShippingComposer commit → same SubmitAsync
3. Preview: POST /checkout/preview → PreviewAsync (no TransactionScope / no persist)

## Pre-transaction (outside TransactionScope)
1. Use-case guard EnsureCanMutateAsync
2. Idempotency: existing by IdempotencyKey OR CartId → return snapshot + ReconcileCartConversionAsync
3. Cart read ICartQueryGateway — Active, ExpectedCartVersion, non-empty
4. QuoteSellerOrdersAsync(requireReservation=false): Catalog (category/qty/rounding), IOfferLookupGateway, Pricing quote revalidation (PRICE_CHANGED), IPromotionEvaluator, ITaxCalculator → in-memory SellerOrder graph

## Shared TransactionScope (ReadCommitted)
Durable participants (CONTRACTS-W2 CROSS_CONTEXT_SHARED_ACID):
1. ICheckoutAbuseGate.EnsureCanStartInitialReservationAsync
2. ReserveCartLinesForOrderAsync → availability batch + IInventoryDirectory.ReserveAsync
3. CheckoutGroup.Submit + bind reservations + OrderDbContext.Add
4. PrepareInitialCycleAsync (reservation cycle prep)
5. Abuse PrepareInitialCommit
6. OrderDbContext.SaveChangesAsync
7. ICartDirectory.ConvertAsync
8. scope.Complete()

## After Submit (not in shared TX)
- Payment/Wallet via Host StorefrontPaymentComposer (separate API)
- PaymentSucceeded → Fulfillment/Settlement via Outbox/Inbox integration events
- On Submit failure after reserves: ReleaseAcquiredAsync compensation attempt

## Atomicity today
Shared physical PostgreSQL ambient TransactionScope across Order + Inventory + Cart DbContexts.
