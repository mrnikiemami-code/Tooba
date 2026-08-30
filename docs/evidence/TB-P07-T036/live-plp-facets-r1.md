# Live PLP Facets (TB-P07-T036-R1)

Source: `live-r1-proof.json` section D (**7/7 pass**)

## Checks
- `GET /v1/storefront/category-plp/{slug}` for viewed category.
- Visible facets = viewed-category effective facets + global `brand`.
- No union of foreign Primary-category-only facet codes from included products.
- Brand facet code present (`brand`); brandless semantics unchanged (no fake Brand entity).
- Missing attribute values do not invent buckets (validated via facet option construction rules already asserted in Host tests + live facet code set).

Demo Draft catalog may yield empty product cards on public PLP; facet **ownership** still verified from response facet list.
