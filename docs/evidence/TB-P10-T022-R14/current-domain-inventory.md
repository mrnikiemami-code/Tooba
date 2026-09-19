# Current Domain Inventory

## Catalog

| Entity | Table (schema `catalog`) | PK | Notes |
|---|---|---|---|
| `CatalogProduct` | `products` | `product_id` | `BrandId`, publication status, `SlugSeam`, `UpdatedAt` |
| `CatalogVariant` | `variants` | `variant_id` | Product-scoped sellable combination |
| `CatalogBrand` | `brands` | `brand_id` | |
| `CatalogCategory` | `categories` | `category_id` | `ParentCategoryId`, `SortOrder` |
| `CatalogTag` | `tags` | `tag_id` | Merchandising tags — not a campaign engine |
| `CatalogLocalizedField` | `localized_fields` | | Unique `(OwnerKind, OwnerId, FieldKey, Locale)` |
| `CatalogCategoryTranslation` | `category_translations` | | Locale + slug |
| `StoreLandingPage` | `store_landing_pages` | `page_id` | `Locale`, `Slug`, `PageType`, Status |
| `StoreLandingPageSection` | `store_landing_page_sections` | | `ConfigurationJson`, `SectionType` |

No Catalog `ListPrice` / `SalePrice` columns (`CatalogFoundationTests` asserts `ListPrice` absent).

## Offer (commercial listing)

| Entity | Table | PK | Scope |
|---|---|---|---|
| `SellerOffer` | `offer.offers` | `offer_id` | `CatalogVariantId` + `SellerPartyId` + `Channel` |

Status: `Draft` / `Active` / `Suspended` / `Archived`. Also: `SellerSku`, return-policy fields, `MinimumOrderQuantity`, `MaximumOrderQuantity`, timestamps.

**Domain invariant (code comment): Offer does not own price or inventory.**

Indexes: unique `(seller, sku)` filtered; unique `(seller, variant, channel)` where `status <> 'Archived'`.

## Pricing

| Entity | Table | PK |
|---|---|---|
| `AuthoredPrice` | `pricing.prices` | `price_id` |

Fields: `OfferId`, `Market`, `Channel`, `Currency`, `Amount`, `ValidFrom`, `ValidTo?`, `Status`, `QualifierKind` (`Base`…), `QualifierKey?`, timestamps.

**No compare-at / rrp / discount_percent columns.** Single authored `Amount`.

## Inventory

| Entity | Table | Notes |
|---|---|---|
| `InventoryLocation` | `inventory.locations` | |
| `StockPosition` | `inventory.stock_positions` | `OfferId`, `OnHand`, `Reserved`; `Available = OnHand - Reserved` |
| `StockReservation` | `inventory.reservations` | Hold / release / consume |

Unique `(OfferId, LocationId)` on stock positions.

## Promotion (checkout discount — not Digikala merchandising)

| Entity | Table | PK |
|---|---|---|
| `PromotionDefinition` | `promotion.promotions` | `promotion_id` |

Checkout evaluator: `Status`, `Priority`, `EffectiveFrom`/`EffectiveTo?`, stacking, `%` or fixed discount, optional `CouponCode`, optional single-axis filters (`OfferId?`, `CatalogVariantId?`, `CategoryId?`, `SellerPartyId?`, market/channel/currency/customer/org), `MinimumQuantity?`, `MinimumSubtotal?`.

**No campaign↔offer membership table. Domain states it does not rewrite authored Pricing.**

## Merchandising / Amazing equivalents searched

| Candidate | Finding |
|---|---|
| Amazing / Incredible / FlashSale aggregates | **Not present** |
| CampaignOffer membership | **Not present** |
| `CatalogTag` | Generic tags only |
| `UnsupportedProductSources` | Reserved: `BestSelling`, `Featured`, `Discounted` (unimplemented) |
| `LandingPageDevelopmentSeed.CampaignSlug` | Ordinary Landing seed page, not campaign entity |
| `FlashSaleStricter` | Reservation-policy UX copy only |
| `StorefrontModels.CampaignProducts` | Demo/home contract field, not DB membership |

## Order / purchase limits

- `SellerOffer.MinimumOrderQuantity` / `MaximumOrderQuantity`
- `PromotionDefinition.MinimumQuantity` / `MinimumSubtotal`
- No per-customer campaign quota / allocation tables found
