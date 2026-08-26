# 07 — Final runtime preview (TB-P06-T009-R1)

After backend rebuild + full test suite, backend restarted and re-probed:

```text
GET /health/live  -> 200
GET /health/ready -> 200
GET http://127.0.0.1:3000/                         -> 200
GET http://127.0.0.1:3000/products/demo-book-1     -> 200
GET /v1/seller/fulfillments (unauth)               -> 401
GET /v1/admin/fulfillments (unauth)                -> 401
GET /v1/customer/orders/{checkoutId}/fulfillments    -> 401
```

Backend and Frontend left running after repair validation.
