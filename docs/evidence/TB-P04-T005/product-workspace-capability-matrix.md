# TB-P04-T005 capability matrix

| Area | Status | Notes |
| --- | --- | --- |
| Admin list | Implemented | `/admin/products` DataGrid |
| Workspace route | Implemented | `/admin/products/[productId]` |
| Multi-domain composition | Implemented | Catalog+Offer+Pricing+Tax+Inventory |
| Variants | Implemented | Catalog-owned; no price/stock on variant |
| Commercial multi-seller | Implemented | Two seller offers in demo/composition |
| Pricing separate | Implemented | Tax-exclusive amounts |
| Tax classification | Implemented | Category code, not hard-coded rate |
| Multi-location inventory | Implemented | Offer+location rows |
| SEO & content | Seam | Slug/title seams; content studio unsupported |
| Publication readiness | Implemented | UI checks; Published != purchasable |
| History/Audit | Implemented | Separate inspector feeds |
| Permissions | Seam | Host scope header; no SpiceDB in UI |
| Concurrency | Implemented | Catalog title stale 409 |
| Mobile / RTL / LTR | Implemented | Workspace forceNarrow + dir toggle |
