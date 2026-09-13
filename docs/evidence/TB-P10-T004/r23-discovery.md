# TB-P10-T004-R23 — Discovery

Mapped real `SellerOrderStatus` in `OrderDomain.cs`. Payment Attempt / payment status is a separate module and is never the open-unpaid counter.

| Order/Payment state | Counts as open unpaid? | Why | Can customer pay? | Can customer cancel? | Hidden-card effect |
| --- | --- | --- | --- | --- | --- |
| SellerOrder `PendingPayment` | Yes | Canonical online unpaid / retryable | Yes while hold/capability allows | Yes if no succeeded payment | None — still counted |
| SellerOrder `Submitted` | Yes | Still commercially open; included in `OpenUnpaidOrderPredicate` | Depends on mode; online path uses PendingPayment | Yes if unpaid policy allows | None |
| SellerOrder `ReservationRequested` | No | Request-to-reserve, not an unpaid online purchase | No online pay | Separate reserve lifecycle | None |
| SellerOrder `Cancelled` | No | Terminal cancel | No | Already cancelled | None |
| SellerOrder `Paid` | No | Verified payment projection | No (already paid) | Unpaid-only cancel denied | None |
| Payment Attempt pending/retry | No | Attempts are not Orders | N/A | N/A | None |
| Payment Failed but Order still `PendingPayment` | Yes (the Order) | Retryable money does not close the Order | Yes | Yes if payment not succeeded | None |
| Manual AwaitingEvidence / AwaitingAdmin | Yes (Order stays `PendingPayment`) | Those are Payment states; predicate is Order-only | Manual path | Denied after succeeded pay | None |
| Hidden pending card | Yes if Order still open/unpaid | Hide is presentation-only (LOCK-SF-104) | Unchanged | Unchanged | Does not change count |
| Guest / `StorefrontGuestActorId` | Skipped | Limits are Store + authenticated CustomerId | Auth-gated checkout | N/A | N/A |

Atomic path: `CheckoutDirectory.SubmitAsync` loads settings outside TX, then inside R21 `TransactionScope` calls `EnsureCanStartInitialReservationAsync` **before** `ReserveCartLinesForOrderAsync`, Cycle #1, Order write, and Cart Convert.

Auth CustomerId: session `PlacedByUserId` (OTP login). Concurrency: `CheckoutAbuseCustomerLocks` row UPDATE/INSERT inside the same TX.

Offer-level `MaxReservationCommitsPerCustomerPerOfferInWindow` is a future extension only — not implemented.
