# 10 — Navigation capability projection

Task: TB-P06-T024-R1

## Mechanism

Both shells fetch effective capabilities on mount:

| Shell | File | API |
|-------|------|-----|
| Admin | `admin-shell.tsx` | `createAdminAccessApi().getMyCapabilities()` → `GET /v1/admin/access-control/me/capabilities` |
| Seller | `vendor-shell.tsx` | `createSellerAccessApi().getMyCapabilities()` → `GET /v1/seller/access-control/me/capabilities` |

Capabilities mapped via `capabilityPermissionIds(effective)` → `Set<permissionId>`.

Nav filter: `itemAllowed(item, caps)` — requires `hasViewCapability(caps, item.viewPermission)` when `viewPermission` set.

## Admin modules (live nav)

| Nav item | viewPermission |
|----------|----------------|
| Dashboard | `admin.dashboard.view` |
| Catalog / Products | `product.view` |
| Orders | `order.view` |
| Fulfillment | `fulfillment.view` |
| Returns | `return.view` |
| Settlement / Payouts | `settlement.view` |
| Content | `content.view` |
| Stories | `story.view` |
| Page Composition | `pagecomposition.view` |
| Sellers | `seller.view` |
| Reviews | `review.view` |
| Promotions | `promotion.view` |
| Access Control | `accesscontrol.view` |

Items without `viewPermission` (e.g. Customers) remain visible when `live: true`.

## Seller modules (live nav)

| Nav item | viewPermission |
|----------|----------------|
| Products | `product.view` |
| Orders | `order.view` |
| Stories | `story.view` |
| Coupons | `promotion.view` |
| Reviews | `review.view` |
| Fulfillment | `fulfillment.view` |
| Returns | `return.view` |
| Wallet | `settlement.view` |
| Access Control | `accesscontrol.view` |

Dashboard, Notifications, Analytics, Settings — no view gate (live always).

## No hardcoded roles

Filtering uses permission IDs from effective access payload only — not role name strings.

## T024 partial → repair status

Navigation projection wired to `me/capabilities` for Admin + Seller shells. Remaining gap: some inner pages still use static capability objects (e.g. Story management) rather than dynamic `me/capabilities` — see `11-action-capability-projection.md`.
