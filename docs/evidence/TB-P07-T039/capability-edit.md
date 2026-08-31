# Capability edit (TB-P07-T039)

Attribute Definition edit surface (`catalog-attribute-ui.tsx`):

- Label: **قابل استفاده برای تنوع** (`VARIANT_AXIS_CAPABILITY_LABEL`)
- Helper explains no automatic category/product mutation
- Create form uses same label/helper; checkbox disabled for unsupported ValueKinds
- API: `PUT /v1/admin/catalog/attribute-definitions/{id}/variant-axis-capability`
