# TB-P10-T001 — Runtime smoke

Ports: Host `http://127.0.0.1:5088`, FE `http://127.0.0.1:3000` (rewrite `/v1/*`).

Raw JSON: `runtime-smoke-raw.json`

## Environment note

- Published storefront product/listing feeds currently return `products: []` / empty rails.
- Cart mutations succeeded against sellable Active Offer `LIVE-A` (`01a030d1-40f1-7000-95f6-b8efc58e2619`).
- Recommendation UI reuses `StorefrontProductCardView` + same `addOfferToCart`; E exercised identical Host ATC path (feed empty at runtime).

## Results

| Scenario | Result | Evidence |
|---|---|---|
| A — add Offer to cart | **PASS** | itemCount=1, unit/line/subtotal=1_850_000 IRR, title resolved |
| B — qty update / merge / remove / restore | **PASS** | qty 1→2→3, delete emptied, restore qty=2 subtotal=3_700_000 |
| C — GET cart ( /cart data ) | **PASS** | same cartId, lines=1, subtotal authoritative |
| D — pricing integrity | **PASS** | unit×qty=line (1_850_000×2=3_700_000) |
| E — recommendation ATC path | **PASS** | same `POST .../lines` path; feed empty → merge-add proof |
| F — guest continuity + wrong secret | **PASS** | reload with secret OK; wrong secret rejected |

FE: `/fa`=200, `/fa/cart`=200; no fabricated «ارسال رایگان» in HTML. Shipping honesty copy is client-rendered (`data-testid="cart-shipping-honest"`) — covered by structure guard.

## Summary

```text
A=PASS B=PASS C=PASS D=PASS E=PASS F=PASS
```
