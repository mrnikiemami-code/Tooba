# 03 — Backend API surface (TB-P06-T011)

## Customer

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/v1/customer/returns` | List own returns |
| GET | `/v1/customer/returns/{id}` | Detail |
| POST | `/v1/customer/returns` | Create return |

## Seller

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/v1/seller/returns` | List seller returns |
| GET | `/v1/seller/returns/{id}` | Detail |
| POST | `/v1/seller/returns/{id}/approve` | Approve + initiate refund |
| POST | `/v1/seller/returns/{id}/reject` | Reject |

## Admin

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/v1/admin/returns` | Grid |
| GET | `/v1/admin/returns/{id}` | Detail |
| POST | `/v1/admin/returns/{id}/retry-refund` | Retry failed refund |

## Frontend bindings

| Surface | Client | Route |
| --- | --- | --- |
| Customer create | `return-api.ts` + `ReturnFormModal` | `/customer-panel/orders/[checkoutId]` |
| Seller list/detail | `vendor-panel/returns/*` | `/vendor-panel/returns` |
| Admin grid/detail | `admin-screens` | `/admin/returns` |

BFF: Customer uses `/api/customer/returns` proxy.
