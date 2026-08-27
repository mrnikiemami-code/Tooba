# 17 — Final runtime preview (TB-P06-T014)

| URL | Expectation |
|---|---|
| http://127.0.0.1:5088/health/live | 200 |
| http://127.0.0.1:5088/health/ready | 200 |
| http://127.0.0.1:3000/ | Home + LocaleSwitcher |
| http://127.0.0.1:3000/blogs | Blog live |
| http://127.0.0.1:3000/customer-panel | Customer dashboard |
| http://127.0.0.1:3000/customer-panel/settings | Profile bridge |
| http://127.0.0.1:3000/vendor-panel | Seller dashboard |
| http://127.0.0.1:3000/vendor-panel/analytics | Live metrics |
| http://127.0.0.1:3000/admin | Admin dashboard |
| Cookie `tooba_locale=en` | html lang=en dir=ltr |
| http://127.0.0.1:3001/ | Shopeiva reference |

Keep Backend + Tooba FE + Shopeiva running after Result where possible.
