# TB-P09-T013 Discovery

Product quantity is integer across commerce paths. No UnitOfMeasure, DecimalPlaces, Step, Offer min/max, or global quantity rounding existed.

## Reuse (do not duplicate)

| Concept | Existing | Reuse |
|---|---|---|
| Language | `Language.LanguageId` + `ILanguageDirectory` | Unit translations use `LanguageId` |
| Category translation | locale-string, not LanguageId | Pattern only; UoM uses LanguageId as required |
| Money decimal | `numeric(19,4)` / `numeric(18,4)` | Quantity uses `numeric(18,6)` separately |
| Settings | operator/seller/customer prefs only; no store KV | Single `catalog.store_quantity_settings` row — not a second settings platform |
| Inventory | OnHand/Reserved/Available + reserve/release | Types only |
| Fulfillment T010–T012 | exact selection, pack-after-process, cancel precedence | Preserve rules with decimal |
| Offer | listing + return policy | Add min/max only |
| Product | `CatalogProduct` descriptive | Add Unit/DecimalPlaces/Step; Variant does not duplicate |

## Product quantities (must become decimal)

| Module | Field | CLR | DB | Change |
|---|---|---|---|---|
| Cart | `CartLine.Quantity` | int | integer | yes |
| Order | `OrderLine.Quantity` | int | integer | yes |
| Inventory | `StockPosition.OnHand/Reserved` | int | integer | yes |
| Inventory | `StockReservation.Quantity` | int | integer | yes |
| Fulfillment | `QuantityOrdered/Processing/Packed/Shipped` | int | integer | yes |
| Fulfillment | `ShipmentItem.Quantity` | int | integer | yes |
| Returns | return line Quantity | int | integer | yes |
| BulkInquiry | inquiry Quantity | int | integer | yes (product amount) |
| FE cart/order/fulfillment/return | `quantity: number` | TS number | JSON number | accept 1.25; no parseInt |

## Remain integer (record counts)

`VariantCount`, `OfferCount`, `SellableUnits` (count of units/locations), pagination `page`, `SortOrder`, employee/row counts, `returnWindowDays`.

## Missing (add)

- `UnitOfMeasure` + `UnitOfMeasureTranslation` (LanguageId)
- Product: `UnitOfMeasureId`, `QuantityDecimalPlaces`, `QuantityStep`
- `EffectiveQuantityPolicy` + `IQuantityNormalizer` (BuildingBlocks)
- Offer: `MinimumOrderQuantity`, `MaximumOrderQuantity`
- Store `GlobalRoundingMode` (Floor/Ceiling/Nearest)
- OrderLine quantity-policy snapshots (unit/places/step) so later policy changes do not rewrite history

## Rounding / frontend

No module-level quantity Floor/Ceiling helpers. FE `parseInt` is used for pages/sort, not cart quantity (`number()` mapper). Storefront quantity inputs are integer-oriented today.
