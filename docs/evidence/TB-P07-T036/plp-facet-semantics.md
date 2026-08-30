# PLP Facet Semantics (TB-P07-T036)

## Ownership
For `/category/X`, visible facets =
- effective configured facets of Category **X**
- plus global Brand facet (`code=brand`, query `f_brand=<BrandId>`)

**Never** union facets from each included Product’s Primary Category.

## Participation
- Display/Primary members in X contribute to a facet bucket only when they have a value for the **same** AttributeDefinition.
- Missing value: product may appear with no facet selected; excluded when that facet value is selected; not counted in option buckets.

## Brand
- Product.BrandId (not AttributeDefinition).
- Options from brands among discoverable products; no fake «بدون برند» Brand entity.
- Brandless visible without Brand filter; excluded when Brand selected.

## Tests
`StorefrontCompositionTests` (viewed-category-only facets, missing-value exclusion, Brand semantics).
Doc: `docs/catalog/CATEGORY-PLP.md`
