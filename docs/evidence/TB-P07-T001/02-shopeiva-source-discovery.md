# Shopeiva attribute/source discovery

Reference root: `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva`

## Product authoring
- `src/app/(vendor)/vendor-panel/products/new/page.jsx`
- `src/app/(vendor)/vendor-panel/products/[id]/edit/page.jsx`
- `src/components/vendor/panel/products/productForm.jsx` — category/brand/SKU/SEO; colors as free-text hex tags (not typed AttributeDefinition catalog)

## Implication for Tooba UI
Admin Attribute Definitions + Category Schema editors must use accepted Admin shell + Shopeiva-derived cards/inputs/tabs geometry — not a foreign JSON schema-builder chrome. Seller product attribute fields integrate into existing vendor product edit patterns.
