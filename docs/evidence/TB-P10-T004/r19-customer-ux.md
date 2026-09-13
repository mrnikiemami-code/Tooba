# R19 customer UX

Storefront pending section (`storefront-pending-payments.tsx` + projector + `storefront-pending-payment-api.test.ts`):

- Visible only for unpaid owned checkouts; Succeeded/Cancelled/Refunded excluded
- Active Cart remains independent (`persistCommittedCheckoutAndDetachActiveCart`; list does not send `readCartSession()`)
- Multiple pending Orders independently scoped
- Empty Active Cart copy coexists with pending cards (no false empty-cart-only)

Countdown:

- `remainingSecondsFromServer` from server `ExpiresAt`
- Failed payment keeps same HoldEndsAt / cycle
- Local `setInterval` only; not `setInterval(() => fetch`
- One refresh at zero (`shouldRefreshOnceAtZero`)

Retry:

- Same Order / checkoutId
- Active cycle → initiate; no timer reset
- After expiry → `EnsureRetryAfterExpiryAsync` then new cycle with Retry TTL
- Unavailable / max cycles → localized copy, no pay

Success:

- Projector drops Succeeded items
- Paid Order remains on customer/order surfaces
- New Active Cart unaffected

USER_VISUAL_ACCEPTED=NO. No live screenshot claim.
