# TB-P07-T001 — Catalog / Variant Audit

## Scoreboard

| Area | Classification | Notes |
| --- | --- | --- |
| Catalog module / EF `catalog` schema | LIVE | Domain / Application / Infrastructure present |
| Product / Variant / Category / Brand | LIVE | `CatalogDomain.cs` aggregates |
| AttributeDefinition / Option / product+variant values | PARTIAL | Typed kinds + fingerprint LIVE; missing metadata, category schema, product-specific axes |
| Variant combination fingerprint uniqueness | LIVE | DB unique + directory reject |
| Category attribute binding + inheritance | MISSING | No `CategoryAttribute` / effective schema |
| Product-specific Variant Axes selection | MISSING | Axis flag is definition-global (`IsVariantAxis`) only |
| Category change orphan detection | MISSING | Assign is additive; no impact API |
| Admin Attribute / Category Schema HTTP | MISSING | Only product workspace list/create/title |
| Seller product attribute authoring HTTP | MISSING | Offer-centric seller APIs only |
| Admin / Seller attribute UI | MISSING | No `/admin/attributes` or schema editor |
| Storefront PDP default/single variant | LIVE | Must remain non-regressed |
| Full variant matrix generator | DEFERRED | Out of scope for this task |
| Faceted search engine | DEFERRED | Filterable projection seam only |
| Shopeiva product form attributes | PARTIAL | Free-text colors/tags — not typed definitions |

## Existing models (paths)

- Domain: `src/backend/Modules/Catalog/Tooba.Catalog.Domain/CatalogDomain.cs`
- Directory: `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogDirectory.cs`
- EF: `.../Persistence/CatalogDbContext.cs` schema `catalog`
- Architecture lock: `docs/architecture/42-catalog-product-variant-foundation.md`

## Attribute definition gaps vs task

Present today: `DefinitionId`, `Code`, `ValueKind`, `IsVariantAxis`, `CreatedAt`, localized name via `CatalogLocalizedText`.

Required / to add: `Unit?`, `IsRequired`, `IsFilterable`, `IsComparable`, `IsVariantAxisAllowed` (evolve from global axis flag), `IsMultivalue`, `DisplayOrder`, validation metadata, `Status`; option `DisplayOrder` / active flag; **Category ↔ Definition binding** with inheritance; **ProductVariantAxis** selection; effective schema API; category-change impact.

## DataType mapping (no invent unsupported)

| Task DataType | Existing `CatalogAttributeValueKind` |
| --- | --- |
| Text | Text |
| Integer / Decimal | Number (canonical decimal; integer validated where required) |
| Boolean | Boolean |
| SingleSelect / MultiSelect | Enumeration (+ `IsMultivalue` for multi) |

## Shopeiva source (reference)

- `reference/shopeiva/src/components/vendor/panel/products/productForm.jsx` — category/brand/SKU/SEO; colors as free-text hex tags
- Vendor new/edit pages under `src/app/(vendor)/vendor-panel/products/`
- No separate Attribute Definitions admin in reference — Tooba admin schema UI must be Shopeiva-*derived* panel patterns (cards/inputs/tabs), not a foreign schema-builder chrome

## Implementation plan (this task)

1. Extend Catalog domain + migration for definition metadata, options, category schema, product axes
2. Directory APIs: effective schema, bind/unbind, set values with schema validation, axes, category-change impact
3. Admin/Seller Host endpoints + Access Control permissions
4. Admin UI (definitions, category schema, product attrs/axes) + Seller product editor integration
5. Dev seed Mobile category + Color/Storage/RAM/Screen + multi-axis product
6. Tests 1–16 + E2E + browser proof; keep sale flow green; FULL_VARIANT_MATRIX remains DEFERRED
