# 17 — Final runtime (TB-P06-T016)

## Probes

| URL | Result |
|---|---|
| `http://127.0.0.1:5088/health/live` | 200 |
| `http://127.0.0.1:5088/health/ready` | 200 |
| `http://127.0.0.1:3000/fa` | 200 — lang=fa dir=rtl |
| `http://127.0.0.1:3000/en` | 200 — lang=en dir=ltr |
| `http://127.0.0.1:3000/fa/blogs` | 200 |
| `http://127.0.0.1:3000/en/blogs` | 200 |
| `http://127.0.0.1:3000/fa/products` | 200 |
| `http://127.0.0.1:3000/en/products` | 200 |
| `http://127.0.0.1:3000/` | 308 → `/fa` |
| `http://127.0.0.1:3000/products` | 308 → `/fa/products` |
| `http://127.0.0.1:3000/fr/products` | 404 |
| `http://127.0.0.1:3000/admin` | 200 unprefixed |
| `http://127.0.0.1:3000/customer-panel` | 200 unprefixed |
| `http://127.0.0.1:3000/vendor-panel` | 200 unprefixed |
| `http://127.0.0.1:3001/` Shopeiva | 200 |

## USER-PREVIEW

- Persian Home: http://127.0.0.1:3000/fa
- English Home: http://127.0.0.1:3000/en
- Persian Blog: http://127.0.0.1:3000/fa/blogs
- English Blog: http://127.0.0.1:3000/en/blogs
- Persian Listing: http://127.0.0.1:3000/fa/products
- English Listing: http://127.0.0.1:3000/en/products
- Original Shopeiva: http://127.0.0.1:3001/

Machine proof: `_locale-routing-api-proof.json`.

Keep Host :5088 + Frontend :3000 + Shopeiva :3001 running after Result where possible.
