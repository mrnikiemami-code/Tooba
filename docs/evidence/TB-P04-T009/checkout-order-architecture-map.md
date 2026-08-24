# TB-P04-T009 — Checkout/order architecture map

```text
Storefront Checkout UI (Shopeiva chrome)
  → Host /v1/storefront/checkout/preview | POST /checkout | GET /checkout/{id}
  → StorefrontCheckoutComposer
  → ICheckoutDirectory.PreviewAsync / SubmitAsync / GetCheckoutAsync
  → Cart query (access + version)
  → Offer lookup
  → Pricing re-quote (PRICE_CHANGED if cart quote stale)
  → Promotion evaluate
  → Tax calculate
  → Order snapshots (CheckoutGroup / SellerOrder / OrderLine)
  → Cart ConvertAsync (after persist; reconcile on retry)
```

Proofs:

- No cross-module SQL JOIN (Host composes Catalog/Party only for titles).
- No Product.Price / Product.Stock.
- Frontend does not author payable totals.
- One CartId → at most one CheckoutGroup (unique index + submit reuse).
