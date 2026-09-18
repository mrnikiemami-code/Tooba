# SSR Exact Route — TB-P10-T022-R13-R2-R2

Exact public route:

`/landing/r13-r2-r2-exact-route-1789753976219-9b3u1`

Fetched with `Accept: text/html` (no hydration wait).

## Sunny (after Admin save/publish of سانی)

| Check | Result |
|---|---|
| HTTP | 200 |
| `data-product-layout="sunny"` | PASS |
| `data-testid="product-showcase-sunny"` | PASS |
| `data-product-showcase-rail="embla"` | PASS |
| section heading / product text | PASS |
| PDP link `/fa/products/...` | PASS |
| price markers | PASS |

## Cinematic (after Admin save of سینمایی on same Page)

| Check | Result |
|---|---|
| HTTP | 200 |
| `data-product-layout="cinematic"` | PASS |
| `data-testid="product-showcase-cinematic"` | PASS |
| embla rail | PASS |
| product content | PASS |

Evidence: `runtime-report.json` → `ssr`.
