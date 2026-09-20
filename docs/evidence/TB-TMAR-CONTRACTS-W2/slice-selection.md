# Slice selection — TB-TMAR-CONTRACTS-W2

## Selected slice
Offer lookup gateway extraction into `Tooba.Offer.Contracts`

## Evidence
| Field | Value |
|---|---|
| Source module | Offer |
| Consumers | Pricing.Infrastructure, Inventory.Infrastructure, Pricing.Application (+ Host/Cart/Order/Promotion via lookup) |
| Edges reduced | Pricing.Infra→Offer.App; Inventory.Infra→Offer.App; Pricing.App→Offer.App |
| Foreign types | `IOfferLookupGateway`, `OfferReference`, `OfferStatus` |
| Target | Tooba.Offer.Contracts |
| Why safest/highest value | Continues CONTRACTS-W1 Offer boundary; lookup is read-only stable surface; avoids Checkout/Saga redesign; shrinks both Infra→App and App→App |

## Rejected for this wave
- Order hub multi-edge migration (too broad)
- Full IWalletDirectory move (admin/gift-card surface not needed by Payment)
