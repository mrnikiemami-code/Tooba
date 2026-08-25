# TB-P05-T007 — SEO Product proof

- PDP metadata title and description come from the live Host `StorefrontProductDetailPage`.
- Canonical ownership is the stable public slug route: `/products/{slug}`.
- The route remains `index,follow`; cart and checkout indexing rules are unchanged.
- Product JSON-LD is built by `buildProductStructuredData` from public presentation fields only: title, SEO description, optional brand, canonical path, backend-composed Offer amount/currency/availability, and seller display name.
- `ProductId`, `CatalogVariantId`, `OfferId`, and `SellerPartyId` are intentionally omitted from structured data and customer-visible PDP text.
- A focused frontend test serializes JSON-LD and proves internal identity values do not appear.
- Pricing and availability in JSON-LD remain backend-owned: Pricing supplies the active tax-exclusive amount and Inventory supplies available units for the selected Offer.
