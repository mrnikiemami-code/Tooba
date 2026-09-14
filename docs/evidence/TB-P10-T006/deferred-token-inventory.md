# TB-P10-T006 — Deferred hard-coded token inventory

Do not migrate in this task. Tokenized surfaces already use `bg-primary` / `text-primary` (header, product card ATC/price, mini-cart, cart checkout CTA).

| File | Value | Surface | Future migrate? | Shopeiva lock |
| --- | --- | --- | --- | --- |
| `storefront-home.tsx` | `#2563EB`, `#1d4ed8` | Home section bars, slider, banner | yes, later token-completion | yes — Home |
| `storefront-home-repair-sections.tsx` | `#2563EB` | Home product hover/heart | yes later | yes — Home |
| `storefront-pdp.tsx` | `#2563EB`, `#1d4ed8` | PDP crumbs, price, ATC, tabs | yes later | yes — PDP |
| `storefront-pdp-bulk.tsx` / `pdp-qa` / `pdp-reviews` | `#2563EB` | PDP secondary | yes later | yes — PDP |
| `storefront-shipping.tsx` | `#E53935` | Shipping chrome/CTA | later; not brand palette | yes — checkout chrome |
| `storefront-cart.tsx` | leftover `#2563EB` | some cart chrome (checkout CTA already tokenized) | partial later | cart |
| `storefront-product-card.tsx` | `STOREFRONT_ACCENT = #2563EB` | constant leftover; card UI uses `bg-primary` | constant only | card |
| Admin settings shell | `#2563EB` | Admin chrome, not Storefront | out of Storefront scope | n/a |

Shipping `#E53935` is pre-existing Shopeiva-era chrome, not this task's wine-burgundy.
