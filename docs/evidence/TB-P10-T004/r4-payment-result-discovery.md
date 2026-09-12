# TB-P10-T004-R4 — Payment Result Discovery

## Defect confirmed

- `storefront-payment-result.tsx` polled `loadStorefrontPayment` every 1.5s for ~20s unconditionally.
- Manual AwaitingAdmin and Succeeded finalize Cart (`clearCartSession` + new empty cart).
- Later polls used the new active Cart as ownership → Host 401 loop while UI kept the first good paint.

## Ownership today (pre-fix)

| Actor | Mechanism |
| --- | --- |
| Auth | Session + OrderAccess on Payment/Checkout |
| Guest | `X-Tooba-Guest-Secret` + cartId |

Cart was used as authorization because guest secret was bound to the active Cart session. After conversion/clear, that binding broke.

## Stable proof after Order commit

- Guest: store-scoped `tooba.storefront.paymentResultProof` = `{ paymentId, checkoutId, committedCartId, guestSecret }` retained across Cart clear.
- Host: `GetOwnedForPaymentResultAsync` authorizes auth via session; guest via secret on **committed** `snapshot.CartId` (not active Cart). Payment GET ignores mutable `cartId` for ownership.

## Polling needs

| State | Poll? |
| --- | --- |
| Online Pending / Processing / Verifying | yes (bounded) |
| Manual awaiting customer form | no |
| Manual AwaitingAdmin (evidence submitted) | no |
| Succeeded / Failed / Cancelled / Rejected | no |

Stop on unmount, ownership/auth failure, and when `shouldPollStorefrontPayment` is false. No overlapping GETs (`inFlight`).
