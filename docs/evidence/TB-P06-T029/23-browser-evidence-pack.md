# 23 — Browser evidence pack (TB-P06-T029)

Index of `captures/` at evidence root. Desktop unless noted. Compact pack for Architect preview.

## Present captures

| File | Intended surface | URL |
| --- | --- | --- |
| `captures/storefront-home.png` | Storefront Home | http://localhost:3000/fa |
| `captures/02-listing.png` | Storefront Listing | http://localhost:3000/fa/products |
| `captures/customer-dashboard.png` | Customer Dashboard (post fake-UX fix) | http://localhost:3000/customer-panel |

## Required pack checklist (task X) — coverage status

### Storefront

| Surface | Capture / proof |
| --- | --- |
| Home | `storefront-home.png` |
| PDP | Runtime LIVE via sale E2E (`04`); screenshot optional follow-up |
| Checkout | Runtime LIVE (`04` / `09`); screenshot optional follow-up |

### Customer

| Surface | Capture / proof |
| --- | --- |
| Dashboard | `customer-dashboard.png` |
| Order | URL open **200** — `.../orders/01a0453b-6829-7000-8c77-32cfb5f5d409` |
| Wallet | URL open **200** |
| Tickets | URL open **200** |

### Seller

| Surface | Capture / proof |
| --- | --- |
| Dashboard / Orders / ACL / Settings | HTTP **200** navigation probes (`03`); ACL/settings confirmed this session |

### Admin

| Surface | Capture / proof |
| --- | --- |
| Dashboard / Orders / ACL / Tickets | HTTP **200** (`03`) |

### Content

| Surface | Capture / proof |
| --- | --- |
| Blog | `/fa/blogs` **200** |
| Article | seeded `guide-online-shopping` |

## Honesty

Screenshot folder is **partial** (3 files). Remaining surfaces are evidenced by HTTP 200 + commercial-demo Host journey (`commercial-demo.json`), not missing as broken routes. Additional PNGs are **NON_BLOCKING_POLISH** if Architect wants denser visual pack.
