# TB-P04-T008 — Cart architecture map

```text
Storefront UI (Next 15, Shopeiva cart chrome)
  → HTTP /v1/storefront/cart*
  → Host StorefrontCartComposer (presentation only)
  → ICartDirectory / ICartQueryGateway (existing Cart module)
  → IOfferLookupGateway + IPriceLookupGateway + IInventoryDirectory (application contracts)
Catalog titles/media and Party seller names are separate DbContext reads in Host.
No cross-schema SQL JOIN.
```

## Identity

- Cart line key = `OfferId` (`CartLineSnapshot.OfferId`).
- Product and Variant are presentation seams only.
- Amount = quoted authored price on the Offer (tax-exclusive estimate). Tax remains Checkout-authoritative.

## Forbidden

- `Product.Price` / `Product.Stock`
- Frontend durable totals
- Second cart model
- localStorage as business truth (sessionStorage holds guest cart id + secret transport only)
