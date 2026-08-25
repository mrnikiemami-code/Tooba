# PDP SEO and structured-data proof

Route verified:

```text
/products/demo-mobile-1
```

## Metadata

| Requirement | Source | Result |
| --- | --- | --- |
| Title | backend `SeoTitle`, falling back to live Catalog title | live |
| Description | backend `SeoDescription`, preferring distinct short description | live |
| Canonical | `/products/{public-slug}` | live |
| Indexability | published sellable PDP remains index/follow | live |
| Semantic heading | one product H1 in locked Shopeiva identity column | live |

## Product JSON-LD

`buildProductStructuredData` emits:

- `@type: Product`;
- live product name and honest description;
- canonical public slug URL;
- brand name when present;
- one truthful Offer using the backend-selected offer amount/currency,
  availability, and public seller display name.

It does not emit:

- internal `ProductId`;
- internal `CatalogVariantId`;
- internal `OfferId`;
- internal `SellerPartyId`;
- `AggregateRating`;
- `reviewCount`;
- static/fabricated stars or reviews.

Frontend tests deliberately pass fake `rating` and `reviewCount` fields into
the raw payload and prove the mapper discards them and JSON-LD contains neither
`AggregateRating` nor `reviewCount`.

## Variant authority

The public variant-selection request is:

```text
GET /v1/storefront/products/{slug}?variantId={opaque-variant-id}
```

The ID is request context, not authority. Host verifies that the variant belongs
to the product and then resolves its active Offer, authored Price, Inventory,
seller, alternate sellers, and Promotion. Public structured URLs remain based
on the product slug; internal IDs are not put into canonical or JSON-LD URLs.

## Rating truth

```text
Reviews/Ratings: NOT AVAILABLE IN BACKEND
AggregateRating: NOT EMITTED
```
