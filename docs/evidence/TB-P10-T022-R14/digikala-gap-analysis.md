# Digikala Incredible Offers — Gap Analysis (benchmark only)

No Digikala JSON file in repo; fields taken from task-observed API surface. **Do not copy Digikala response as write model.**

## Observed capabilities → classification

| Capability / field | Class | Tooba mapping note |
|---|---|---|
| `selling_price` | A | Campaign membership promo amount **or** active price under campaign rules; do not fork Pricing tables |
| `rrp_price` | A | Map to canonical `AuthoredPrice.Amount` (list) when promo applies |
| `discount_percent` | C | **Derive** from list vs selling |
| `order_limit` / `min_order_limit` | A | Reuse `SellerOffer` min/max; optional membership override later |
| `marketable_stock` | A | Reuse inventory `Available` aggregation |
| `is_incredible` | D as stored bool | **Reject** `Offer.IsAmazing`; express via campaign membership |
| end time / `timer` | A / C | Store `EndAt`; timer **derived** |
| `sold_percentage` | B | Needs allocation/sales later; derive when quota exists |
| `badge` | A (presentation) | Localized campaign/membership badge text |
| `incredible_products_list` | A | Active Amazing campaign membership query |
| `running_out_incredible_products` | B/C | Sort/filter by low stock — presentation/query later |
| `lightening_deal_products` | B | Separate `PromotionType` later, same campaign engine |
| `teasing_incredible_products` | B (architect now) | Future `StartAt` + teasing visibility |
| `early_access_products` | B | Later; keep schema extensible without implementing |
| brand filters / browse | C | Storefront browse read-model; reuse Catalog facets |
| see-more route | C | Landing/collection route later |

Classes: A = foundation now; B = architect for later; C = presentation/read-model; D = not needed / anti-pattern if stored as Digikala-shaped.

## Design rules confirmed

- Timer derived from window
- Discount % derived where safe
- Sold % derived from allocation/sales when introduced
- API read model ≠ write model
