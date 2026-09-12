# TB-P10-T004-R13 — Canonical committed proof

Creation: `persistCommittedCheckoutAndDetachActiveCart` on successful shipping commit / checkout submit, **before** active-cart detach.

Shape:

```json
{
  "checkoutId": "<committed checkout>",
  "cartId": "<source converted cart>",
  "guestSecret": "<source guest secret or empty for auth>",
  "storeKey": "storefront"
}
```

Storage key: `tooba.storefront.committedCheckoutProofs` — map keyed by checkoutId. Writing Order B does not overwrite Order A.

Resolution order for a payment journey:

1. R4 `paymentResultProof` when `paymentId` matches (and checkoutId matches when both present)
2. committed checkout proof for `checkoutId`
3. no fallback to active Cart

Used by: Payment page GET checkout, wallet quote, initiate, manual evidence/proof, sandbox, retry, result/status.

Cannot: mutate any Cart; authorize another checkout; enumerate orders; use payment/order id alone.

Expiry: session-scoped; lost session → Host 403 `checkout.access.denied` / `payment.access.denied` with payment-access copy (not order-registration-failed). Authenticated path uses Host session and does not require guest proof.

Multiple pending Orders: supported by checkout-keyed map.
