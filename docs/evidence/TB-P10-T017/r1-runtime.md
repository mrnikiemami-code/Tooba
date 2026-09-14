# Runtime — TB-P10-T017-R1

Host `:5088` + FE `:3000`. Capture: `r1-capture.mjs`. Report: `r1-runtime-report.json` `ok=true`.

| Step | Result |
| --- | --- |
| Cold `/fa` `/fa/products` `/fa/products/demo-prod-fashion-men-men-pants-1` `/fa/landing-campaign` `/fa/cart` | 200 first request |
| PaletteTint Home/PLP/PDP/Landing/Cart | 200, `data-storefront-background-style=PaletteTint` |
| DarkOnly PDP/Landing | 200, `html.dark` |
| Restore | tooba-blue / LightOnly / classic / Neutral / canonical Home |
| `useTheme` throw in capture | none |
| `/api/auth/me` | 7×401, not a storm |

Screenshots: `docs/evidence/TB-P10-T017/screenshots/r1/`.
