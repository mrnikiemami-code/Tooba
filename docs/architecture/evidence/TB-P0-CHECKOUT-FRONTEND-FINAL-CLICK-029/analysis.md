# TB-P0-CHECKOUT-FRONTEND-FINAL-CLICK-029 — source-grounded route map (evidence)

Base SHA: 47f4d8f0ed86cf28efa67111f5e5e50b1ca7f83a (== origin/main at claim)
Surface: `src/frontend/**` only. Backend/Host untouched.

## 1. Shipping → Payment → final click (after change)

| Step | Owner file:line | Wire |
| --- | --- | --- |
| Cart page CTA (unchanged) | `app/storefront/storefront-cart.tsx` (`checkoutHref` → `/shipping`) | local navigation |
| Shipping continue | `app/storefront/storefront-shipping.tsx:224` `persistAndContinue()` | `PUT /v1/storefront/shipping/selection` (`storefront-shipping-api.ts` `saveShippingSelection`) |
| Shipping navigation | `app/storefront/storefront-shipping.tsx:247` | `router.push(/payment?cartId={cartId})` — **no** `/v1/storefront/shipping/commit` |
| Payment load (cart mode) | `app/payment/storefront-payment-handoff.tsx:61-75`, client `app/storefront/storefront-final-click-api.ts:58` | `GET /v1/storefront/cart/{cartId}/payment-draft?couponCode=` (Order 011) + `GET /v1/storefront/payment-methods` (Payment) |
| Final click (cart mode) | `app/payment/storefront-payment-handoff.tsx:190`, client `storefront-final-click-api.ts:147` | `POST /v1/storefront/cart/{cartId}/start-payment` (Order 028) |
| Payment load (committed mode) | `storefront-payment-handoff.tsx:92-120` | `GET /v1/storefront/checkout/{checkoutId}?cartId=` + `wallet-quote` (unchanged legacy path) |
| Final click (committed mode) | `storefront-payment-handoff.tsx:195` | `POST /v1/storefront/checkout/{checkoutId}/payments` (unchanged legacy path) |

Both modes are chosen by one `useEffect` branch (`params.get("cartId")` first, else `checkoutId`/stored) and the
Pay handler uses a single exclusive ternary (`mode === "cart" ? startFinalClickPayment : startStorefrontPayment`),
so one click can reach exactly one initiation route.

## 2. Cart state / version storage (no new store)

- Cart id + guest secret: `sessionStorage` via `readCartSession()` in `app/storefront/storefront-cart-api.ts`
  (`tooba.storefront.cartId`, `tooba.storefront.guestSecret`); sent only as headers
  (`X-Tooba-Guest-Secret`, `X-Tooba-Cart-Version`) plus the conventional guest cookie server-side.
- Cart version used by the final click: `page.cartVersion` from the GET payment-draft response (`011`),
  not from any client-held total.
- Stable click idempotency key: `sessionStorage["tooba.storefront.finalClickIdempotency.{cartId}.{provider}"]`
  (`storefront-final-click-api.ts:103`) — reused across re-renders and retries of the same operation; only a
  deliberate change of method (or cart) scopes a new key. Never generated inline per request.
- Post-commit proofs (unchanged canonical store): `tooba.storefront.committedCheckoutProofs` +
  `tooba.storefront.paymentResultProof`; the active cart pointer is detached only after a **successful** start.

## 3. Eager-commit callers inventory (all found)

| Caller | File:line | Status after this task |
| --- | --- | --- |
| Shipping continue | `app/storefront/storefront-shipping.tsx` | **Removed** — draft-only + cart-addressed navigation. Asserted by `storefront-shipping-api.test.ts`. |
| Legacy commit wrapper | `app/storefront/storefront-shipping-api.ts:369` `commitShippingToPayment` | No caller remains (documented legacy/explicit-recovery seam, kept export only). Verified by grep + test (`assert.doesNotMatch(shipping, /commitShippingToPayment/)`). |
| Legacy `/checkout` page | `app/checkout/page.tsx` → `app/storefront/storefront-checkout.tsx:116` `submitStorefrontCheckout` (POST `/v1/storefront/checkout`) | **Out of bounded scope, reported residual**: still exists, still commits on its own explicit form submit. No link in the frontend points to `/checkout` (grep for `/checkout` href returns no matches), so it is not on the Shipping→Payment path. Untouched this task. |
| Cart totals preview | `app/storefront/storefront-cart.tsx:147` `previewStorefrontCheckout` | Read-only `POST /v1/storefront/checkout/preview` (011 quote seam): no order, no reservation, no cart conversion. Unchanged. |

## 4. Money / identity boundary

Client request body for the final click contains exactly five keys:
`expectedCartVersion`, `idempotencyKey`, `useWallet`, `providerCode`, `couponCode` (asserted key-list equality in
`storefront-final-click-api.test.ts`). No `amount`, `payable`, `shippingAmount`, `checkoutId`, `userId`,
`deadline`, `guestSecret` or `cartId` is serialized; the cart id travels only in the route and the guest proof
only in `cartHeaders()`/cookie. Displayed amounts come verbatim from the 011 draft response.

## 5. Failure / recovery handling

- Localized messages: `toCustomerFinalClickMessage` (`storefront-final-click-api.ts:224`) — hold/payability
  (`payment.reservation.not_payable`), availability (`checkout.inventory.unavailable`), stale cart, expired
  cart, missing shipping selection, access denied, plus delegation to the existing checkout/payment messages.
  No automatic retry, no invented renewal limit, no claim of automatic hold renewal.
- Post-commit failure: the ProblemDetails `checkoutId` is preserved (additive
  `StorefrontCartApiErrorMeta.checkoutId`) and a committed proof is written without detaching the active cart;
  the page then offers `ادامهٔ پرداخت همین سفارش` → `/payment?checkoutId=...`
  (`storefront-payment-handoff.tsx:360`), i.e. recovery on the same order with no second commit.
- No service worker, timeout loop or unconditional side effect was added.

## 6. Known limits (reported, not hidden)

- L1 Wallet eligibility for a **pre-order** cart is not available: `tooba.storefront.payment-methods` is
  store-scoped and `wallet-quote` is checkoutId-addressed (documented in 011 evidence L1). The cart-addressed
  payment page therefore shows only the store-enabled tenders and does **not** offer wallet before commit
  (no fabricated quote). Wallet stays available on the committed-order recovery path.
- L2 Runtime/HTTP verification against the live Host was NOT performed (Host CLOSED, no build/restart);
  verification is source-level + focused unit tests + `tsc --noEmit`.
- L3 `storefront-payment-handoff.tsx` was restructured in place (single file, same route) rather than split.
