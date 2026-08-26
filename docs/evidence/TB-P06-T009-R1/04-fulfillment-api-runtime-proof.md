# 04 — Fulfillment API runtime proof (TB-P06-T009-R1)

Unauthenticated smoke (route must exist; auth gate acceptable):

| URL | Method | Status | Interpretation |
|-----|--------|--------|----------------|
| `http://127.0.0.1:5088/v1/seller/fulfillments` | GET | 401 | Route exists; auth required |
| `http://127.0.0.1:5088/v1/admin/fulfillments` | GET | 401 | Route exists; auth required |
| `http://127.0.0.1:5088/v1/customer/orders/00000000-0000-0000-0000-000000000001/fulfillments` | GET | 401 | Route exists; auth required |

404 route absence not observed.
