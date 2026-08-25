# PDP backend capability inventory

Task: `TB-P05-T010`

Inventory basis:

- `CatalogProduct`, `CatalogVariant`, localized text, product/variant
  attributes, brand/category, and media references in Catalog;
- `SellerOffer` in Offer;
- authored prices in Pricing;
- positions and availability in Inventory;
- Promotion evaluator;
- Host `StorefrontComposer` and `StorefrontProductDetailPage`;
- existing Shopeiva `StorefrontShopeivaPdp`;
- architecture records for Reviews/Ratings and Media.

| Capability | Owning module | Current backend field / contract | Visible before T010? | Shopeiva target | Action |
| --- | --- | --- | --- | --- | --- |
| Product title | Catalog | localized `name` | yes | identity / H1 | preserve live binding |
| Slug | Catalog | `CatalogProduct.SlugSeam` | yes | canonical route | preserve |
| Brand | Catalog | `BrandId` + localized brand name | yes, name only | identity badge | preserve live name |
| Category | Catalog | `ProductCategories` + localized category name | yes, primary name | breadcrumb / meta cards | preserve |
| Media/gallery | Catalog | `CatalogProductMediaReference.MediaAssetId` | yes | Shopeiva gallery | bind every available media reference; opaque seam remains documented |
| Variants | Catalog | `CatalogVariant` | selected identity only | Shopeiva options area | expose real variants and select backend-authoritatively |
| Variant options | Catalog | `CatalogVariantAttributeValue`, variant-axis definitions/options | no | Shopeiva option chips | bind human-readable labels and values; no hard-coded color/size |
| Short description | Catalog localized text | generic `description` read seam; no distinct normal write path | duplicated/fallback | summary / intro | add safe `short_description` localized key and bind |
| Detailed description | Catalog localized text | generic `description` read seam; no distinct normal write path | duplicated/fallback | detailed tab | add safe `full_description` localized key and bind without duplicating summary |
| Specifications | Catalog | `CatalogProductAttributeValue` + non-axis definitions | no | specifications tab | project Persian labels/values; no IDs |
| Primary offer | Offer | active `SellerOffer` by selected Catalog variant | yes | buy box | make selected variant govern offer |
| Seller | Party via Offer seller identity | public display name | yes | buy box | preserve |
| Price | Pricing | active authored price keyed by OfferId | yes | buy box | preserve backend authority |
| Availability / quantity | Inventory | positions keyed by OfferId | yes | stock and quantity selector | preserve backend authority |
| Other sellers | Offer + Pricing + Inventory + Party | alternate candidates | yes, product-wide | other sellers | scope to selected variant |
| Promotion | Promotion | evaluator result | cards only | buy box | bind only applicable backend result |
| Reviews | NOT AVAILABLE IN BACKEND | no Review module/read model | placeholder only | reviews tab | honest unavailable state; no fake list |
| Ratings | NOT AVAILABLE IN BACKEND | no aggregate rating contract | static fake 4.5/stars | identity, cards, structured data | remove stars/numbers; no AggregateRating |
| Related products | Host composition over Catalog category truth | backend-selected cards | yes | related rail | preserve; current product excluded |
| SEO title | Catalog/Host composition | `SeoTitleSeam` fallback to title | yes | metadata | preserve |
| SEO description | Host composition | description-derived fallback | yes | metadata | prefer honest short/full content |
| Product JSON-LD | Host DTO + frontend serializer | Product and truthful selected Offer | yes | route structured data | preserve; no internal IDs |
| AggregateRating JSON-LD | NOT AVAILABLE IN BACKEND | no rating/review truth | absent | structured data | remain absent |

## Architecture conclusions

### Safe local additions

Distinct descriptions can use the existing localized-text table and unique
`OwnerKind/OwnerId/FieldKey/Locale` model. Keys such as
`short_description` and `full_description` do not require Product columns or a
schema migration and remain inside Catalog ownership.

Specifications and variant axes already exist in Catalog. Host may query each
Catalog table and compose in memory. Selection must be sent to Host so Host
resolves the active offer, authored price, inventory, seller, and promotion for
that exact variant.

### Honest absences

```text
Reviews: NOT AVAILABLE IN BACKEND
Ratings: NOT AVAILABLE IN BACKEND
AggregateRating: NOT AVAILABLE IN BACKEND
```

T010 must not create a Review domain. Fixed Shopeiva stars, `4.5`, review
counts, and rating structured data must not render.

### Preserved separations

```text
Product != Offer != Price != Inventory
```

Catalog carries descriptive product/variant data only. Offer carries seller
commercial identity. Pricing remains keyed by OfferId. Inventory remains keyed
by OfferId. Host composition queries module stores separately and never performs
a cross-module SQL join.
