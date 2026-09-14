# Runtime coverage — TB-P10-T017-R3

Crawler: `src/frontend/scripts/storefront-theme-coverage.mjs` (reusable; loads `STOREFRONT_ROUTE_INVENTORY`).

Report: `docs/evidence/TB-P10-T017/r3-runtime-report.json`

| Run | Palette | ThemeMode | BackgroundStyle | Result |
| --- | --- | --- | --- | --- |
| neutral-blue | tooba-blue | LightOnly | Neutral | 200s, heuristic 0 |
| tint-blue | tooba-blue | LightOnly | PaletteTint | 200s, heuristic 0 + proof shots |
| tint-wine | wine-burgundy | LightOnly | PaletteTint | representative subset 200 |
| dark-blue | tooba-blue | DarkOnly | PaletteTint | home + customer dashboard 200 |

- status500: none in passing run (retry-on-500 for Next compile races)
- hydration warnings: none
- customer login: Development OTP `09111111111` / `123456`
- missing dynamic sample: customer ticket `[id]` — layout-static via CustomerPanelShell + SupportTicketForm
- restored canonical: tooba-blue / LightOnly / classic / Neutral

Screenshots: `docs/evidence/TB-P10-T017/screenshots/r3/` (`home-r3.png`, `pdp-r3.png`, `plp-r3.png`, `login-r3.png`, `cart-r3.png`, `checkout-r3.png`, `customer-dashboard-r3.png`, `customer-orders-r3.png`, `customer-order-detail-r3.png`, `landing-r3.png`, `content-r3.png`, `dark-home-r3.png`, `dark-customer-r3.png`).
