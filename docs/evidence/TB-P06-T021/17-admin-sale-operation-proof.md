# 17 — Admin sale operation proof (TB-P06-T021)

Policy: do **not** create a new mega-screen if existing operational pages already cover the transaction.

## Existing operational surfaces

| Concern | Route | Host |
|---|---|---|
| Catalog Product (create/publish prerequisite) | `http://127.0.0.1:3000/admin/products`, `…/products/{productId}` | `POST/GET/PATCH /v1/admin/products*` |
| Orders | `http://127.0.0.1:3000/admin/orders`, `…/orders/{checkoutId}` | `GET /v1/admin/orders*` |
| Fulfillments | `http://127.0.0.1:3000/admin/fulfillments`, `…/fulfillments/{fulfillmentId}` | Admin fulfillment endpoints |
| Promotions (coupon on sale) | `http://127.0.0.1:3000/admin/promotions` | T020 `/v1/admin/promotions*` |
| Reviews | `http://127.0.0.1:3000/admin/reviews` | Existing admin reviews |

## Inspectable on one legitimate transaction

| Field | Covered by |
|---|---|
| Product / Offer reference | Admin order detail + product workspace |
| Seller | Order seller segments / admin sellers |
| Buyer / order | Admin order checkout detail |
| Payment | Payment state on admin order |
| Promotion | Coupon code / discount on checkout; admin promotions list |
| Fulfillment | Admin fulfillments list/detail |
| Refund / return foundation | Returns LIVE foundation (prior P06) where relevant |

## Catalog authoring for sale proof

`POST /v1/admin/products` creates Published Product + default Variant (no price/stock on Product) — prerequisite for Seller Offer path (`05`, `08`). Admin does **not** collapse Pricing/Inventory onto Catalog.

## Verdict

```text
ADMIN_SALE_OPERATION_VIEW = LIVE (existing pages)
```
