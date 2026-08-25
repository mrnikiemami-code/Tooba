# TB-P05-T005 — Shopeiva commercial fidelity

Canonical source: accepted Shopeiva storefront structure and `docs/evidence/TB-P05-T002/`.

| Surface | Structure preserved | Live binding | Honest deviation |
| --- | --- | --- | --- |
| Header / mega menu | desktop mega-menu, mobile drawer, nested category navigation | published Catalog hierarchy | no product cards or prices in menu |
| Commercial home | banners, section spacing, rails, RTL order | backend-defined storefront sections | unsupported sections stay hidden |
| Special offers | Shopeiva offer rail and product cards | applicable automatic Promotion evaluation | no coupon/customer-specific claims without shopper context |
| Sale section | campaign card/rail language | only backend-confirmed discounted amounts | no fake countdown, badge, or discount |
| New arrivals | existing product rail | backend ordering from live published products | no invented arrival timestamp in UI |
| Product rail | existing horizontal cards and controls | Catalog + Offer + Pricing + Inventory projection | Product has no Price or Stock authority |
| Category landing | existing product-list route and shell | selected category plus descendants | canonical route remains `/products?category=...` |
| Mobile | existing header/drawer/cards/rails | same live contracts | overflow contained; no desktop rail squeezed into viewport |

Allowed adaptations are limited to the Tooba blue token and Persian localization.

Acceptance rule:

```text
Shopeiva structure is locked.
Replace bindings, not components.
```

