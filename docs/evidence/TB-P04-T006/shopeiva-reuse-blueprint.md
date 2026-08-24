# TB-P04-T006 — Shopeiva reuse blueprint

Locked rule: Shopeiva-first REUSE/ADAPT; inject Tooba Professional Data Grid; connect live Tooba APIs by **end of P06**; REBUILD only when Shopeiva cannot satisfy Tooba semantics.

## Classification counts (pattern families, not 73 mechanical rows)

| Class | Count | Meaning |
| --- | --- | --- |
| HIGH_REUSE | 8 | Chrome/footer/trust, campaign merchandising shells, auth card chrome, seller public profile composition |
| MEDIUM_ADAPT | 22 | Home, PLP, PDP, cart, checkout steps, vendor/customer shells, forms |
| LOW_REUSE_REBUILD | 6 | Offer/pricing/tax/inventory/authorization-aware command UIs; marketplace checkout truth |
| DEFER | 8 | Blog/CMS extras, gift cards, survey, referral, compare |

Tooba Data Grid **replaces** template tables on Products, Orders, Sellers, Customers, Inventory, Payments, Fulfillment, Returns, Reviews, Promotions, Content — not discarded.

## Pattern map

| Shopeiva route | Component/layout family | Tooba surface | Decision | Preserve | Must change | Future truth | Data Grid |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `/` `/index2` `/index3` | Header + home slider/stories/sections | Storefront home | ADAPT | RTL density, merchandising blocks | Tenant/catalog APIs, no mock JSON | Catalog + Offer + Pricing | no |
| `/categories` `/category/...` `/search` | listing + filters | Catalog browse | ADAPT | filter chrome, cards | facets from Catalog | Catalog search | optional |
| `/product/[id]/[name]` | PDP gallery/buy box | PDP | ADAPT | gallery composition | Price/Stock/Offer/Tax/Inventory | Product+Variant+Offer+Pricing+Tax+Inventory | no |
| `/cart` `/shipping` `/payment` | checkout steps | Cart/checkout | ADAPT | step chrome | marketplace checkout, payments, tax | Cart+Checkout+Order+Payment | no |
| `/sale` `/offers` `/coupons` | campaign pages | Promotions | ADAPT | campaign hero/grid | promo contracts | Promotion | no |
| `/sellers` `/seller-profile/...` | seller directory/profile | Seller public | ADAPT | storefront-of-seller | Party/seller APIs | Party + Listing | no |
| `/login` `/register` | auth cards | Auth | ADAPT | card layout | real Tooba auth | Identity | no |
| `/blogs` | content | CMS | DEFER | article layout | Tooba CMS later | Content | no |
| `/vendor-panel` | vendor shell + dashboard | Admin + Seller dashboards | ADAPT | sidebar/header/spacing | authz, live KPIs | Analytics contracts | no |
| `/vendor-panel/products` | toolbar + table | Admin/Seller products | ADAPT | shell/toolbar | replace table with Tooba Data Grid; Catalog commands | Catalog+Offer+Pricing+Inventory | **yes** |
| `/vendor-panel/products/new` `.../edit` | product form | Product workspace | ADAPT | form sections | Tooba product semantics vs T005 foundation | Catalog | no (form) |
| `/vendor-panel/orders` `[id]` | order list/detail | Orders | ADAPT | detail chronology | order state machine | Order+Fulfillment | **yes** on list |
| `/vendor-panel/customers` | customer table | Customers | ADAPT | shell | Party model | Party | **yes** |
| `/vendor-panel/analytics` | charts | Analytics | ADAPT | chart layout | live metrics; runtime client shell flaky | Analytics | no |
| `/vendor-panel/coupons` | promo table/form | Promotions | ADAPT | form chrome | Promotion module | Promotion | **yes** on list |
| `/vendor-panel/reviews` | review list | Reviews | ADAPT | moderation chrome | Review module | Review | **yes** |
| `/vendor-panel/wallet` | wallet | Payouts | ADAPT | balance UI | Payment/payout APIs | Payment | no |
| `/vendor-panel/tickets` | ticket list/detail | Support | ADAPT | thread UI | support backend | Support | **yes** on list |
| `/vendor-panel/settings` | settings | Settings | ADAPT | grouped fields | tenant settings | Tenant | no |
| `/user-panel/*` | customer shell | Customer account | ADAPT | account nav | identity/orders APIs | Identity+Order | list pages **yes** where tabular |
| (none) | Admin app | Tooba Admin | do not invent | — | No distinct Admin; adapt vendor | Admin modules | **yes** |

## REBUILD / INJECT only

- Tooba Professional Data Grid
- multi-seller Offer management
- advanced Pricing, Tax, multi-location Inventory
- authorization-aware commands, business audit
- marketplace checkout semantics

## P06 constraint

By end of P06, storefront core, admin core, seller core, customer core, and commerce purchase path must use live Tooba APIs. P07+ is polish/hardening, not first-time API wiring.
