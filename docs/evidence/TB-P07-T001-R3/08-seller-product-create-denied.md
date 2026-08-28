# TB-P07-T001-R3 — Seller product create denied

## Ownership lock
Canonical Product master = **Admin only**. Seller is Offer / Price / Inventory only.

## Host
`SellerPanelEndpoints` under `/v1/seller`:

| Present | Absent |
| --- | --- |
| `GET/POST /offers`, price, inventory | `POST /products` (no create product) |
| `GET /catalog-variants` (read existing) | Admin Catalog create APIs |

Seller attribute endpoints (`PUT /v1/seller/products/{id}/attributes/...`, variant-axes) write values on an **existing** Catalog product id — they do not create a Product.

## FE
| Surface | Behavior |
| --- | --- |
| `/vendor-panel/products` | Lists Offers; CTA **پیشنهاد جدید** (not محصول جدید) |
| `/vendor-panel/products/new` | Create **Offer** on Admin-owned Catalog variant; copy: «محصول Catalog باید از قبل توسط ادمین ساخته شده باشد» |
| Admin `/admin/products` | Only place with **محصول جدید** → `POST /v1/admin/products` |

## Verdict
Direct canonical Product creation is **denied/hidden** on Seller. Offer-only path preserved; no Seller Product create UI.
