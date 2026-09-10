# TB-P10-T002 — Discovery

## Frontend (before)

| Surface | Class |
|---|---|
| Dedicated `/shipping` | **missing** (shipping folded into `/checkout`) |
| Shopeiva shipping UI | reference only under FrontStarter |
| Method cards | template on Shopeiva; Host used `storefront-default` |
| Delivery slots | mock on Shopeiva; absent on Tooba |
| Cart CTA | `/checkout` |

## Backend (before)

| Capability | Class |
|---|---|
| AddressBook auth + guest inline snapshot | **real** |
| ShippingService catalog (admin) | **real** (WIP preserved) |
| Storefront method selection | **missing** / hardcoded default |
| Shipping price / lead / prep | **missing** → added via ShippingMethodsOptions.Rates + calculator |
| Delivery minimum | **missing** → StorefrontShippingCalculator |
| Customer note on checkout | **missing** → CheckoutGroup.CustomerNote |
| Cart shipping draft | **missing** → order.cart_shipping_drafts |

## Decision

Reuse ShippingService catalog + ShippingMethodRegistry EnabledCodes; do not invent parallel carrier domain. Payment remains template handoff (`/payment`).
