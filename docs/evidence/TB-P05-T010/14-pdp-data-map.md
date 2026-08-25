# PDP data map

## Read composition

```text
GET /v1/storefront/products/{slug}?variantId={optional}
  → Catalog product / localized content / brand / category / media
  → Catalog variants / product attributes / variant axes
  → Offer active offers for product variants
  → Pricing active authored prices keyed by OfferId
  → Inventory positions keyed by OfferId
  → Party public seller display names
  → Tax offer classification
  → Promotion evaluation for selected Offer
  → Host in-memory composition
  → StorefrontProductDetailPage
  → locked Shopeiva PDP
```

Each module store is queried separately. There is no cross-module SQL join or
foreign-module navigation.

## Contract map

| Public PDP field | Authority | UI location |
| --- | --- | --- |
| `title`, `slug` | Catalog | H1 / route |
| `shortDescription` | Catalog localized `short_description` | identity + intro |
| `fullDescription` | Catalog localized `full_description` | detailed tab |
| `categoryName`, `brandName` | Catalog | breadcrumb / identity / meta |
| `mediaAssetIds` | Catalog opaque media references | gallery |
| `specifications[]` | Catalog product attributes + selected variant axes | specifications tab |
| `variants[].axes` | Catalog variant-axis values | option chips |
| `selectedVariantId` | Host-validated Catalog membership | selected option state |
| `primaryOffer` | Offer candidate selected by Host | buy box / cart identity |
| `primaryOffer.amountExclusiveOfTax` | Pricing | buy-box base price |
| `promotionalAmountExclusiveOfTax`, `promotionLabel` | Promotion evaluator | buy-box promotion |
| `primaryOffer.availableUnits` | Inventory | availability / quantity |
| `primaryOffer.sellerDisplayName` | Party through Offer seller identity | buy box |
| `otherSellers[]` | Offer + Pricing + Inventory + Party for selected variant | other-sellers subsection |
| `relatedProducts[]` | Host category-based selection over live product cards | related rail |
| `seoTitle`, `seoDescription` | Catalog/Host composition | metadata |
| `cartMutationEnabled` | Host | CTA state |

## Selection lifecycle

```text
user selects Catalog option chip
→ frontend sends slug + variantId to Host
→ Host rejects a variant outside the product
→ Host scopes candidate offers to selected variant
→ Host resolves primary Offer
→ Pricing, Inventory, seller, alternates, and Promotion come from that Offer
→ frontend replaces whole PDP projection
→ add-to-cart posts returned OfferId
```

The frontend never computes price, stock, promotion, or valid variant
combinations.

## Catalog content seam

`ICatalogDirectory.UpsertProductLocalizedFieldAsync` is the owning-module write
seam for distinct localized PDP content. It uses existing
`CatalogLocalizedText`; no Product columns or schema migration were added.
Development seed data is deterministic and safely repeatable. Existing seeded
databases are enriched by idempotent localized-field upserts.

## Honest capability boundary

```text
Review list: NOT AVAILABLE IN BACKEND
Rating summary: NOT AVAILABLE IN BACKEND
AggregateRating: NOT AVAILABLE IN BACKEND
```

No Review aggregate, fake stars, numeric score, count, or structured rating is
created by T010.
