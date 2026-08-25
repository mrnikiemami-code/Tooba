# Mega Menu data-binding map

## Category path

```text
Catalog published categories
→ StorefrontComposer.GetCategoriesAsync
→ GET /v1/storefront/categories and Storefront home payload
→ StorefrontCategoryItem { categoryId, parentCategoryId, name }
→ StorefrontShell
→ StorefrontShopeivaHeader
→ roots / children / deeper descendants
→ /products?categoryId={Catalog category id}
```

Frontend tree projection:

- root: `parentCategoryId === null`;
- child: `parentCategoryId === selectedRoot.categoryId`;
- descendant: `parentCategoryId === child.categoryId`;
- no flattening and no synthetic leaf text;
- eight published top-level demo categories remain visible and reachable;
- 24 published children remain available through the live hierarchy.

## Brand promo path

```text
Catalog published brands
→ GET /v1/storefront/brands through same-origin Next rewrite
→ validated StorefrontBrandItem projection in client header
→ first six live names
→ /brand/{public-slug}
```

Failure is honest: when the API is unavailable the brands block is omitted; no
fixture names are substituted.

## Shopeiva chrome versus business data

| Element | Kind | Authority |
| --- | --- | --- |
| 3/6/3 panel, icons, borders, spacing, Gift/Star chrome | visual structure | purchased Shopeiva source |
| category names and hierarchy | business/navigation data | Catalog |
| category URLs | navigation adaptation | Tooba public products route |
| brand names/slugs | business/navigation data | Catalog |
| “پیشنهادهای فروشگاه” and `/offers` CTA | honest navigation label | existing public offers route |
| blue accent | approved Tooba theme adaptation | Tooba storefront contract |

## Explicit exclusions

The Mega Menu reads no Product card, Offer, Pricing, Inventory, seller, rating,
review, or discount-value contract. It performs no cross-module query and owns
no business decision.
