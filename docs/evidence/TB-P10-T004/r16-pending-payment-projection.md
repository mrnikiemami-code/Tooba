# R16 pending-payment projection

Endpoint: `POST /v1/storefront/pending-payments`

Composer: `StorefrontPendingPaymentComposer`

Mapper: `StorefrontPendingPaymentProjector` (no lifecycle mutation)

## Batching (no N+1)

1. One checkout query (`Include` seller orders + lines; `PlacedByUserId` or `CheckoutId IN proofs`)
2. One payment query `WHERE checkoutId IN (...)` then latest-per-checkout in memory
3. One `GetProjectionsAsync` for all cycle rows
4. One variants query, one localized name query, one media query

Does not call `GetLatestForCheckoutAsync` or per-id `GetProjectionAsync`.

## Per item

- human `orderReference`
- payable + currency
- compact items + mediaAssetId
- `paymentPresentation` (not raw enums)
- `canInitiatePayment` / `canRetryPayment` / `isManualAwaitingReview` / `primaryAction`
- reservation presentation `held|ended|none`
- cycleNumber, secondsRemaining, serverTime, holdEndsAt (not shown raw)
- maxCycles / retryCountRemaining / supplyStatus / paymentId / hasReachedRetryLimit

Succeeded / cancelled / refunded / all-seller-cancelled excluded.
Manual Pending + evidence = informational, `primaryAction=none`.
