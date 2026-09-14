# Route inventory — TB-P10-T017-R3

Source of truth: `src/frontend/lib/storefront-appearance/storefront-route-inventory.ts`.
Crawler: `src/frontend/scripts/storefront-theme-coverage.mjs`.

Samples from Host (2026-09-14): PDP `demo-prod-fashion-men-men-pants-1`; category `demo-cat-mobile-tablet`; brand `demo-brand-adidas`; seller `962c7b3f972781ad8ef562c4`; landing `landing-campaign`; blog `t016-sched-20260904082033`; customer order `01a09b8b-3e28-7000-94ed-e37517c5cbf5`.

| Route Pattern | Example URL | Auth | Layout/Shell | Major Wrapper | Theme Coverage | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `/` | `/fa` | anonymous | StorefrontShell | PageBackground + Home bands | crawled | Home `fullBleed` |
| `/products` | `/fa/products` | anonymous | StorefrontShell | SectionSurface listing | crawled | PLP |
| `/category/[slug]` | `/fa/category/demo-cat-mobile-tablet` | anonymous | StorefrontShell | Category PLP section | crawled | |
| `/brands` | `/fa/brands` | anonymous | StorefrontShell | directory cards | crawled | |
| `/brand/[slug]` | `/fa/brand/demo-brand-adidas` | anonymous | StorefrontShell | merch section | crawled | |
| `/sellers` | `/fa/sellers` | anonymous | StorefrontShell | directory cards | crawled | |
| `/seller-profile/[publicId]` | `/fa/seller-profile/962c7b3f972781ad8ef562c4` | anonymous | StorefrontShell | listing | crawled | |
| `/offers` `/sale` `/new-products` `/most-viewed` `/best-seller` `/trending` | `/fa/{kind}` | anonymous | StorefrontShell | merch section | crawled | |
| `/products/[slug]` | `/fa/products/demo-prod-fashion-men-men-pants-1` | anonymous | StorefrontShell | section + card | crawled | |
| `/[slug]` | `/fa/landing-campaign` | anonymous | StorefrontShell | landing roles | crawled | `fullBleed` |
| `/cart` | `/fa/cart` | either | StorefrontShell | commerce section | crawled | |
| `/shipping` | `/fa/shipping` | either | StorefrontShell | page + section | crawled | actual checkout address step |
| `/checkout` | `/fa/checkout` | either | redirect | redirect to shipping | crawled | |
| `/payment` | `/fa/payment` | either | StorefrontShell | payment page | crawled | empty cart honest |
| `/payment/result` | `/fa/payment/result` | either | StorefrontShell | result card | crawled | |
| `/order/confirmation` | `/fa/order/confirmation` | either | StorefrontShell | confirmation card | crawled | |
| `/login` | `/fa/login` | anonymous | StorefrontShell | section + elevated card | crawled | OTP unchanged |
| `/customer-panel` | `/customer-panel` | customer | CustomerPanelShell | page/header/section | crawled | |
| `/customer-panel/orders` | `/customer-panel/orders` | customer | CustomerPanelShell | section + cards | crawled | |
| `/customer-panel/orders/[checkoutId]` | `/customer-panel/orders/01a09b8b-3e28-7000-94ed-e37517c5cbf5` | customer | CustomerPanelShell | section + cards | crawled | |
| `/customer-panel/wishlist` | `/customer-panel/wishlist` | customer | CustomerPanelShell | cards | crawled | |
| `/customer-panel/addresses` | `/customer-panel/addresses` | customer | CustomerPanelShell | cards | crawled | |
| `/customer-panel/notifications` | `/customer-panel/notifications` | customer | CustomerPanelShell | cards | crawled | |
| `/customer-panel/tickets` | `/customer-panel/tickets` | customer | CustomerPanelShell | cards | crawled | |
| `/customer-panel/tickets/new` | `/customer-panel/tickets/new` | customer | CustomerPanelShell | CardSurface form | crawled | |
| `/customer-panel/tickets/[id]` | n/a | customer | CustomerPanelShell | same shell + support form | layout-static | no seeded ticket id |
| `/customer-panel/wallet` | `/customer-panel/wallet` | customer | CustomerPanelShell | cards | crawled | |
| `/customer-panel/gift-cards` | `/customer-panel/gift-cards` | customer | CustomerPanelShell | cards | crawled | |
| `/customer-panel/profile` | `/customer-panel/profile` | customer | CustomerPanelShell | cards | crawled | |
| `/customer-panel/settings` | `/customer-panel/settings` | customer | CustomerPanelShell | cards | crawled | |
| `/customer-panel/dev/wallet-checkout` | `/customer-panel/dev/wallet-checkout` | customer | CustomerPanelShell | page | crawled | not in live nav |
| `/blogs` | `/fa/blogs` | anonymous | StorefrontShell via blogs layout | content section | crawled | |
| `/blogs/[slug]` | `/fa/blogs/t016-sched-20260904082033` | anonymous | StorefrontShell | article section + card | crawled | |
| `/blogs/category/[slug]` | `/fa/blogs/category/t016-cat-20260904082033` | anonymous | StorefrontShell | content section | crawled | |
| `/blogs/author/[slug]` | `/fa/blogs/author/t016-author-20260904082033` | anonymous | StorefrontShell | content section | crawled | |

Admin and vendor-panel are out of scope.
