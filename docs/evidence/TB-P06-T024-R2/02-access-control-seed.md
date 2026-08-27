# 02 — Access Control seed

Task: TB-P06-T024-R2

## Seeded actors / ownership

| Role | Identity | Notes |
|------|----------|-------|
| Platform Admin | Reuses Admin dev-context actor (`platformAdminActorId`) | Existing Development admin |
| Seller | فروشگاه آرمان | Real Party from Seller marketplace seed |
| Seller Owner | اپراتور آرمان | Existing seller-actor-a |
| Seller Employee | اپراتور سفارش موبایل | `seller-employee-mobile@tooba.local` |
| Role | Mobile Order Operator (`mobile-order-op`) | Seller-scoped |

## Permissions on Mobile Order Operator

| Permission | Scope |
|------------|-------|
| `order.view` | Category = موبایل |
| `order.handle` | Category = موبایل |
| `accesscontrol.view` | GlobalWithinOwner |

## Ceiling / assignment

- Seller owner assignment ensured for Owner Actor under Seller owner scope.
- Employee assigned to Mobile Order Operator.
- `SyncUserCapabilityTuplesAsync` after assignment.
- Party membership + **authorization `member` tuple** written for employee (required for `SellerPanelAccess` party#view).

## Runtime proof

`GET http://127.0.0.1:5088/v1/admin/access-control/demo-preview` returns live IDs (see `05-demo-identities.md`).
