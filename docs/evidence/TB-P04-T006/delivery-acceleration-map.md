# TB-P04-T006 — Delivery acceleration map

Goal: fastest presentable vertical slice using Shopeiva REUSE/ADAPT + Tooba Data Grid + live APIs by end of P06.

## Fastest presentation path

1. Storefront home ADAPT (`/` family) + Header/Footer REUSE
2. PLP ADAPT (`/category/...` `/search`)
3. PDP ADAPT (`/product/...`)
4. Cart + shipping + payment ADAPT
5. Order confirmation from Tooba Order module
6. Admin/Seller product list: Shopeiva vendor shell + **Tooba Data Grid** + Catalog APIs (builds on TB-P04-T005 functional foundation; visual language from Shopeiva vendor, not T005 custom Admin look)
7. Customer `/user-panel/orders` ADAPT

## Surface leverage

| Surface | Leverage | Notes |
| --- | --- | --- |
| Storefront chrome | HIGH_REUSE | Header/footer/trust |
| Home / PLP / PDP | MEDIUM_ADAPT | Mock JSON → Catalog/Offer/Pricing/Inventory |
| Cart/checkout | MEDIUM_ADAPT | Must replace empty-cart payment footer trap; real checkout |
| Vendor dashboard | MEDIUM_ADAPT | Shell REUSE; KPIs live |
| Vendor/Admin tables | MEDIUM_ADAPT | Grid INJECT |
| Product forms | MEDIUM_ADAPT | Align with T005 interaction, Shopeiva visual |
| Orders/customers/reviews | MEDIUM_ADAPT | Grid INJECT |
| Distinct Admin | LOW_REUSE_REBUILD | Does not exist — adapt vendor |
| Blog/gift/survey | DEFER | Not on P06 purchase path |
| Offer/tax/multi-location inventory UIs | LOW_REUSE_REBUILD | Shopeiva cannot satisfy Tooba semantics |

## P06 must-be-live

Storefront listing, PDP, cart, checkout, order; Admin product publish; Seller product/order; Customer orders; purchase path on real Tooba APIs.

## What not to do

Do not rebuild Storefront/Admin/Vendor/Customer visual language from scratch while Shopeiva has a usable pattern. Do not drop Tooba Data Grid.
