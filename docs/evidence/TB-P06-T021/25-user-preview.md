# 25 — User preview (TB-P06-T021)

Keep Backend + Tooba Frontend + Original Shopeiva running where technically possible.

Base ports: Host **5088**, FE **3000**, Shopeiva **3001**.

## Tooba — Seller

| Surface | URL |
|---|---|
| Seller Products (Offers) | http://127.0.0.1:3000/vendor-panel/products |
| Seller Product / Offer create | http://127.0.0.1:3000/vendor-panel/products/new |
| Seller Offer edit | http://127.0.0.1:3000/vendor-panel/products/{offerId} |
| Seller Orders | http://127.0.0.1:3000/vendor-panel/orders |
| Seller Order detail | http://127.0.0.1:3000/vendor-panel/orders/{sellerOrderId} |
| Seller Fulfillments | http://127.0.0.1:3000/vendor-panel/fulfillments |
| Seller Fulfillment detail | http://127.0.0.1:3000/vendor-panel/fulfillments/{fulfillmentId} |

## Tooba — Storefront

| Surface | URL |
|---|---|
| Home | http://127.0.0.1:3000/fa |
| Discovery / listing | http://127.0.0.1:3000/fa/products |
| PDP | http://127.0.0.1:3000/fa/products/{slug} |
| Cart | http://127.0.0.1:3000/fa/cart |
| Checkout | http://127.0.0.1:3000/fa/checkout |
| Payment sandbox | http://127.0.0.1:3000/fa/payment/sandbox |
| Payment result | http://127.0.0.1:3000/fa/payment/result |

## Tooba — Customer

| Surface | URL |
|---|---|
| Customer Orders | http://127.0.0.1:3000/customer-panel/orders |
| Customer Order detail | http://127.0.0.1:3000/customer-panel/orders/{checkoutId} |

## Tooba — Admin

| Surface | URL |
|---|---|
| Admin Products | http://127.0.0.1:3000/admin/products |
| Admin Product workspace | http://127.0.0.1:3000/admin/products/{productId} |
| Admin Orders | http://127.0.0.1:3000/admin/orders |
| Admin Order detail | http://127.0.0.1:3000/admin/orders/{checkoutId} |
| Admin Fulfillments | http://127.0.0.1:3000/admin/fulfillments |
| Admin Fulfillment detail | http://127.0.0.1:3000/admin/fulfillments/{fulfillmentId} |
| Admin Promotions | http://127.0.0.1:3000/admin/promotions |

## Host health

| Probe | URL |
|---|---|
| Health | http://127.0.0.1:5088/health |
| Live | http://127.0.0.1:5088/health/live |
| Ready | http://127.0.0.1:5088/health/ready |

## Original Shopeiva comparison

| Surface | URL |
|---|---|
| Home | http://127.0.0.1:3001/ |
| Products (vendor) | http://127.0.0.1:3001/vendor-panel/products |
| Product new | http://127.0.0.1:3001/vendor-panel/products/new |
| Product edit | http://127.0.0.1:3001/vendor-panel/products/{id}/edit |
| Storefront product | http://127.0.0.1:3001/product/{id}/{slug} |
| Cart | http://127.0.0.1:3001/cart |
| Checkout | http://127.0.0.1:3001/checkout |

## Demo steps (short)

1. Admin: create Published Catalog Product (+ default Variant).  
2. Seller: `/vendor-panel/products/new` → Offer + price + stock.  
3. Customer: discover on `/fa/products` → PDP → cart → checkout → sandbox pay.  
4. Seller: fulfill from `/vendor-panel/orders` / fulfillments.  
5. Customer: track on `/customer-panel/orders/{checkoutId}`.  
6. Admin: inspect order/fulfillment.  
7. Compare vendor/storefront against Shopeiva URLs above.
