# Tooba — Catalog Product & Variant Foundation

Status:

```text
IN_PROGRESS — TB-P03-T001 awaiting Architect ACCEPT
```

Task:

```text
TB-P03-T001
```

## Purpose

Catalog is the module-owned descriptive truth of sellable *things*: Product, Variant, Category, Brand, and typed attributes. It is not the commercial offer, not the price list, and not the stock ledger.

## Locked separations

```text
Catalog Product != Seller Offer / Listing
Catalog Product != Price
Catalog Product != Inventory
Variant belongs to Product
Locale != Market != Currency
```

`CatalogProduct` has no `Price`, stock, `SellerId`, or `OfferId`. A published Catalog product is still not purchasable; purchasability belongs to a future Offer after Pricing and Inventory exist.

## Aggregates and entities

| Concept | Owner | Notes |
| --- | --- | --- |
| Product | Catalog | Durable `ProductId`, publication status, optional brand, slug/SEO *seams* only |
| Variant | Catalog | Child of Product; unique attribute-combination fingerprint per product |
| Category | Catalog | Optional parent in the same schema; not storefront navigation UI |
| Brand | Catalog | Editorial identity; not seller ownership |
| AttributeDefinition / Option / values | Catalog | Typed (text/number/boolean/enum/instant); variant axes vs product specs |
| Localized text | Catalog | BCP-47 locale rows; not market or currency |
| Media reference | Catalog | Opaque `MediaAssetId`; no binary, no cross-module FK |

## Typed attributes

Definitions are not unlimited free-form EAV. `CatalogAttributeCanonicalizer` normalizes values by `CatalogAttributeValueKind`. Enumeration values persist the option id. Variant axes cannot be stored as product-level specs.

Duplicate variants for the same product + fingerprint are rejected by a unique database index, not only memory checks.

## Events and Search

Outbox integration names:

- `catalog.product_created.v1`
- `catalog.product_published.v1`
- `catalog.product_updated.v1`
- `catalog.variant_created.v1`

These are projection *seams* for future Search. Search must never become Catalog source of truth. No indexer runs in this task.

## Persistence and tenancy

`CatalogDbContext` owns schema `catalog` on the resolved Marketplace or Single-Store database. Catalog does not parse Host. Tenant A and Tenant B use separate databases; ids are not visible across them.

No FK to Identity, Party, Media, Pricing, or Inventory tables.

## Contracts

`ProductReference`, `VariantReference`, `ICatalogLookupGateway`, and `ICatalogDirectory` live in Application. EF entities stay in Infrastructure/Domain. `ICatalogUseCaseGuard` is an authorization seam without SpiceDB types or role columns on Product.

## Out of scope here

Seller Offer, Pricing, Tax, Inventory, Cart, Order, Payment, Search indexing, Media processing, full SEO engine, Seller portal, Admin Product Workspace, Shopeiva, Data Grid, Design System, commercial UI.

## Future workspace

Admin product authoring will sit on these contracts later. Weak UI remains a product failure, not a reason to put price or stock on Product.
