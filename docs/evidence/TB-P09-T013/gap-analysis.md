# TB-P09-T013 Gap Analysis

| Gap | Decision |
|---|---|
| Integer product qty everywhere | Migrate true product amounts to `decimal` / `numeric(18,6)`; preserve existing integers as `n.000000` |
| No UoM | Add catalog-owned multilingual UoM; seed `pcs`, `kg`, `g` |
| No product policy | Product owns Unit + DecimalPlaces + optional Step; Variant does not |
| No Offer min/max | Optional decimal min/max on SellerOffer; validated against product policy |
| No global rounding | One `catalog.store_quantity_settings.rounding_mode`; no per-product/offer rounding |
| No central normalizer | `QuantityNormalizer` in BuildingBlocks; modules must not Floor/Ceiling independently |
| Order history | Snapshot unit/places/step on OrderLine |
| FE integer UX | Decimal-aware input; step=null does not force HTML step; display strips `.000000` |
| Inventory/fulfillment/returns | Type + validation only; no redesign |
| Multiple selling units | Out of scope |

Classification complete. Implementation proceeds only after this file.
