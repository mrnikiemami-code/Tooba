# Slice selection — TB-TMAR-FE-ADMIN-W6

Selected: **admin-dashboard** (operational metrics + dashboard home screen).

| Criterion | Evidence |
| --- | --- |
| Risk | LOW — read-only Host dashboard metrics; composition/navigation only |
| Ownership | Clear API `/v1/admin/dashboard` + `AdminDashboardScreen` |
| Flat/god touch | Removed from `admin-api.ts` + `admin-screens.tsx` |
| Orders | Deferred — mutations, payment, fulfillment coupling |

Rejected: Orders (executeAdminOrderOperation, payment/fulfillment workflows, multi-file god surface).
