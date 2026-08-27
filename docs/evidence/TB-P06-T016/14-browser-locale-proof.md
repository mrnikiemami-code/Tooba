# 14 — Browser locale proof (TB-P06-T016)

## Live HTTP / document proof

| URL | Status | lang | dir |
|---|---|---|---|
| `http://127.0.0.1:3000/fa` | 200 | fa | rtl |
| `http://127.0.0.1:3000/en` | 200 | en | ltr |
| `http://127.0.0.1:3000/fa/blogs` | 200 | fa | rtl |
| `http://127.0.0.1:3000/en/blogs` | 200 | en | ltr |
| `http://127.0.0.1:3000/fa/products` | 200 | fa | rtl |
| `http://127.0.0.1:3000/en/products` | 200 | en | ltr |
| Unprefixed `/` | 308 → `/fa` | — | — |
| Invalid `/fr/...` | 404 | — | — |
| Panels unprefixed | 200 | cookie/default | — |

Machine-readable: `_locale-routing-api-proof.json`.

## USER-PREVIEW

- Persian Home: http://127.0.0.1:3000/fa
- English Home: http://127.0.0.1:3000/en
- Persian Blog: http://127.0.0.1:3000/fa/blogs
- English Blog: http://127.0.0.1:3000/en/blogs
- Shopeiva: http://127.0.0.1:3001/
