# Schema migration

Hand-written `[Migration]` Up() scripts. Existing integer rows convert `USING quantity::numeric(18,6)` (5 → 5.000000). Money columns untouched.

| Migration | Schema |
|---|---|
| `20260909130000_AddQuantityFoundation` | `catalog.units_of_measure`, `unit_of_measure_translations` (LanguageId), `store_quantity_settings` singleton Nearest, product Unit/DecimalPlaces/Step |
| `20260909130100_AddOfferQuantityLimits` | offer min/max `numeric(18,6)` nullable |
| `20260909130200_DecimalCartQuantity` | `cart.cart_lines.quantity` |
| `20260909130300_DecimalOrderQuantity` | `order.order_lines.quantity` + unit/places/step/display snapshots |
| `20260909130400_DecimalInventoryQuantity` | on_hand, reserved, reservation qty, restock inbox |
| `20260909130500_DecimalFulfillmentQuantity` | ordered/processing/packed/shipped + shipment_items |
| `20260909130600_DecimalReturnQuantity` | `returns.return_items.quantity` |
| `20260909130700_DecimalBulkInquiryQuantity` | inquiry quantity |

Record-count integers (pagination, VariantCount, OfferCount, LocationCount, returnWindowDays) not migrated.
