# 13 — Public / prior panel regression proof (TB-P05-T023)

Seller panel shell restore must not regress:

| Surface | Check | Result |
|---|---|---|
| Home | `npm run test:home` / critical storefront | run in validation |
| PDP | `test:pdp-guard` | run in validation |
| Listing | `test:listing-guard` | run in validation |
| Cart / Checkout | untouched under `/cart`, `/checkout` | no seller-panel edits |
| Customer panel | `/account` shell untouched | no shared shell coupling |
| Seller Offer architecture | Product≠Offer; no Product.Price/Stock | preserved in products grid copy + API |
| SpiceDB isolation | Actor+Seller context select retained | preserved in `vendor-shell.tsx` |

No redesign of public storefront chrome.
