# CONTRACT GAPS — TB-296 (final, after R6 implementation)

Verified against live Host 5088 and source (2026-11-25).

## Implemented real contracts
- Offer create/get/patch/query/lists: `/v1/admin/offers*`, `/v1/seller/offers*`.
- Price write: `POST|PUT /offers/{offerId}/price` → `SetOfferPriceCommand`; the detail read returns one resolved `IR/IRR/Marketplace` base price.
- Inventory write: `POST|PUT /offers/{offerId}/inventory` → `SetOfferInventoryCommand` with `StockItemId`; multi-position confirmed and exercised.
- Fulfillment responsibility write (admin only): `PUT /v1/admin/offers/{offerId}/fulfillment` with `fulfillment.manage`.
- Shipping type catalog (admin read): `GET /v1/admin/shipping-types` (active operational types).
- Return policy, qty limits, lifecycle, seller SKU: `UpdateOfferCommand` via PATCH.

## Remaining contract/policy gaps (explicitly gated, no fake UI)
| Capability | Status | Reason |
|---|---|---|
| Per-offer discount definition (percent/fixed/coupon/priority/conditions) | GATED | Promotion is campaign-centric; no per-offer discount-definition command/DTO is decided |
| Offer history/event read | GATED | Only domain/outbox events exist; no read projection/API for the workspace |
| Per-offer shipping service assignment | GATED | Stage-3 decision; current contract stores responsibility + `ShippingTypeId`, not an offer-owned service assignment |
| Multi-currency/market/validity price rows | GATED | Detail read resolves a single price; no Pricing price-list read contract is exposed |
| Fulfillment responsibility read-back | GATED | Only the admin write route exists; no offer-workspace read field/route |
| Product decimal precision/step/rounding | PARTIAL | Read model exposes unit code/name only |

## Conclusion
No task-wide blocker remains for the implementable slices. The five gated capabilities require an Architect/owner contract decision and are visibly marked in the workspace; they are not implemented with invented behavior.
