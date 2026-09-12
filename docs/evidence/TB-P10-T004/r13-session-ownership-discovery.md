# TB-P10-T004-R13 — Session / ownership discovery

Trace: Home/PDP AddToCart → Active Cart → Shipping → SubmitCheckout → Order commit → Cart Converted → Payment page → initiate → result → continue shopping → new Active Cart.

| Artifact | Lifetime | Owner | Storage | Authorization purpose | May rotate? | Must survive Cart replacement? |
| --- | --- | --- | --- | --- | --- | --- |
| `tooba.storefront.cartId` | Active shopping only | current Active Cart | sessionStorage | mutate/read Active Cart | yes, immediately after convert | no |
| `tooba.storefront.guestSecret` | Active shopping only | current Active Cart guest | sessionStorage | `X-Tooba-Guest-Secret` for Active Cart | yes with new Cart | no — never copied to new Cart |
| `tooba.storefront.committedCheckoutProofs[checkoutId]` | unpaid/paid committed Order | Store + that Checkout | sessionStorage map | Payment page/initiate/manual/sandbox/retry | no (keyed; other keys kept) | yes |
| `tooba.storefront.paymentResultProof` | payment result/status | that Payment + Checkout | sessionStorage | result/status after initiate | replaced only for same payment write | yes |
| Authenticated session | Host cookie/session | UserId | server | auth Order/Payment ownership | n/a | yes — independent of Converted Cart |
| Converted source Cart | immutable history | Order.CartId | DB | guest proof lineage only | no | yes (not as active pointer) |

Findings before fix:

- Payment GET used guest secret against converted Cart; missing secret → `راز مهمان` → `checkout.rejected` → «ثبت سفارش انجام نشد».
- `ensureStorefrontCart` accepted GET 200 on Converted and kept that id.
- `addOfferToCart` posted `expectedVersion` to Converted → domain EnsureActive → `cart.rejected`.
- R4 `paymentResultProof` required `paymentId`, so `/payment?checkoutId=` before initiate had no proof and fell back to active Cart.
